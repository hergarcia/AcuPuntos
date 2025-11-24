using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AcuPuntos.Models;
using AcuPuntos.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AcuPuntos.ViewModels
{
    public partial class AgendaViewModel : BaseViewModel
    {
        private readonly IFirestoreService _firestoreService;
        private readonly IAuthService _authService;
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        private DateTime _selectedDate = DateTime.Today;

        [ObservableProperty]
        private ObservableCollection<AppointmentSlot> _availableSlots = new();

        [ObservableProperty]
        private ObservableCollection<AppointmentSlot> _myAppointments = new();

        [ObservableProperty]
        private bool _isAdmin;

        private IDisposable? _availableSlotsListener;
        private IDisposable? _myAppointmentsListener;

        public AgendaViewModel(IFirestoreService firestoreService, IAuthService authService, INavigationService navigationService)
            : base()
        {
            _firestoreService = firestoreService;
            _authService = authService;
            _navigationService = navigationService;
            Title = "Agenda";
            IsAdmin = _authService.IsAdmin;
        }

        protected override async Task InitializeAsync()
        {
            await LoadDataAsync();
            SetupListeners();
        }

        [RelayCommand]
        private void Disappearing()
        {
            _availableSlotsListener?.Dispose();
            _myAppointmentsListener?.Dispose();
        }

        private void SetupListeners()
        {
            // 1. Available Slots Listener for Selected Date
            _availableSlotsListener?.Dispose();
            
            var start = new DateTimeOffset(SelectedDate.Date);
            var end = new DateTimeOffset(SelectedDate.Date.AddDays(1).AddTicks(-1));
            
            _availableSlotsListener = _firestoreService.ListenToAppointments(start, end, (slots) =>
            {
                var available = slots.Where(x => x.Status == AppointmentStatus.Available).OrderBy(x => x.StartTime).ToList();
                AvailableSlots = new ObservableCollection<AppointmentSlot>(available);
            });

            // 2. My Appointments Listener (Only setup once if not exists, or if user changes)
            if (_myAppointmentsListener == null && _authService.CurrentUser?.Uid != null)
            {
                _myAppointmentsListener = _firestoreService.ListenToUserAppointments(_authService.CurrentUser.Uid, (slots) =>
                {
                    MyAppointments = new ObservableCollection<AppointmentSlot>(slots);
                });
            }
        }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                // Initial load while listeners connect
                await LoadAvailableSlotsAsync();
                await LoadMyAppointmentsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading agenda data: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        partial void OnSelectedDateChanged(DateTime value)
        {
            // Update available slots listener for the new date
            _availableSlotsListener?.Dispose();
            var start = new DateTimeOffset(value.Date);
            var end = new DateTimeOffset(value.Date.AddDays(1).AddTicks(-1));
            
            _availableSlotsListener = _firestoreService.ListenToAppointments(start, end, (slots) =>
            {
                var available = slots.Where(x => x.Status == AppointmentStatus.Available).OrderBy(x => x.StartTime).ToList();
                AvailableSlots = new ObservableCollection<AppointmentSlot>(available);
            });
        }

        private async Task LoadAvailableSlotsAsync()
        {
            var start = new DateTimeOffset(SelectedDate.Date);
            var end = new DateTimeOffset(SelectedDate.Date.AddDays(1).AddTicks(-1));
            var slots = await _firestoreService.GetAvailableSlotsAsync(start, end);
            AvailableSlots = new ObservableCollection<AppointmentSlot>(slots);
        }

        private async Task LoadMyAppointmentsAsync()
        {
            if (_authService.CurrentUser?.Uid == null) return;
            var slots = await _firestoreService.GetUserAppointmentsAsync(_authService.CurrentUser.Uid);
            MyAppointments = new ObservableCollection<AppointmentSlot>(slots);
        }

        [RelayCommand]
        private async Task BookSlot(AppointmentSlot slot)
        {
            if (slot == null || _authService.CurrentUser == null) return;

            // Prompt for user notes
            var userNotes = await App.Current.MainPage.DisplayPromptAsync(
                "Reservar Turno",
                $"Notas o comentarios (opcional):\nTurno: {slot.StartTime.ToLocalTime():HH:mm}",
                "Reservar",
                "Cancelar",
                placeholder: "Ej: Prefiero turno por la mañana...",
                maxLength: 200);

            if (userNotes == null) // User cancelled
                return;

            try
            {
                IsBusy = true;
                
                // 1. Optimistic UI update: Remove from available list immediately
                AvailableSlots.Remove(slot);

                slot.UserId = _authService.CurrentUser.Uid;
                slot.Status = AppointmentStatus.PendingApproval;
                
                if (!string.IsNullOrWhiteSpace(userNotes))
                {
                    slot.UserNotes = userNotes;
                }

                // 2. Try to book safely
                await _firestoreService.BookSlotAsync(slot);
                
                await LoadDataAsync();
                await App.Current.MainPage.DisplayAlert(
                    "Éxito", 
                    "Solicitud enviada. Espera la confirmación del administrador.", 
                    "OK");
            }
            catch (Exception ex)
            {
                // 3. Rollback UI if failed
                if (!AvailableSlots.Contains(slot))
                {
                    AvailableSlots.Add(slot);
                    // Re-sort might be needed, but LoadDataAsync will fix it anyway
                }

                System.Diagnostics.Debug.WriteLine($"Error booking slot: {ex.Message}");
                
                string message = "No se pudo reservar el turno.";
                if (ex.Message.Contains("ya no está disponible"))
                {
                    message = "Lo sentimos, este turno ya no está disponible.";
                    await LoadDataAsync(); // Refresh to show real state
                }
                
                await App.Current.MainPage.DisplayAlert("Error", message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task CancelAppointment(AppointmentSlot slot)
        {
            if (slot == null) return;

            var confirm = await App.Current.MainPage.DisplayAlert(
                "Cancelar", 
                "¿Estás seguro de que deseas cancelar esta reserva?", 
                "Sí", 
                "No");
            
            if (!confirm) return;

            try
            {
                IsBusy = true;
                
                if (slot.Status == AppointmentStatus.PendingApproval)
                {
                    // Can cancel directly if still pending
                    slot.UserId = null;
                    slot.Status = AppointmentStatus.Available;
                    slot.UserNotes = null;
                    await _firestoreService.UpdateSlotAsync(slot);
                    await App.Current.MainPage.DisplayAlert("Cancelada", "La solicitud de reserva ha sido cancelada.", "OK");
                }
                else
                {
                    // Request cancellation for confirmed appointments
                    slot.RequestedModification = "Solicitud de cancelación";
                    slot.Status = AppointmentStatus.ModificationRequested;
                    await _firestoreService.UpdateSlotAsync(slot);
                    await App.Current.MainPage.DisplayAlert(
                        "Solicitud Enviada", 
                        "La solicitud de cancelación ha sido enviada al administrador.", 
                        "OK");
                }

                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cancelling appointment: {ex.Message}");
                await App.Current.MainPage.DisplayAlert("Error", "No se pudo procesar la cancelación.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task RequestModification(AppointmentSlot slot)
        {
            if (slot == null) return;

            var modification = await App.Current.MainPage.DisplayPromptAsync(
                "Solicitar Modificación",
                "Describe el cambio que necesitas:",
                "Enviar",
                "Cancelar",
                placeholder: "Ej: ¿Puede cambiar la hora a las 15:00?",
                maxLength: 200);

            if (string.IsNullOrWhiteSpace(modification))
                return;

            try
            {
                IsBusy = true;
                slot.RequestedModification = modification;
                slot.Status = AppointmentStatus.ModificationRequested;
                await _firestoreService.UpdateSlotAsync(slot);
                
                await LoadDataAsync();
                await App.Current.MainPage.DisplayAlert(
                    "Solicitud Enviada", 
                    "Tu solicitud de modificación ha sido enviada al administrador.", 
                    "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error requesting modification: {ex.Message}");
                await App.Current.MainPage.DisplayAlert("Error", "No se pudo enviar la solicitud.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task NavigateToAdminAgenda()
        {
            await _navigationService.NavigateToAsync(nameof(Views.AdminAgendaPage));
        }
    }
}
