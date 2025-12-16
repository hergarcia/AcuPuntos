using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AcuPuntos.Models;
using AcuPuntos.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AcuPuntos.ViewModels
{
    public partial class AdminAgendaViewModel : BaseViewModel
    {
        private readonly IFirestoreService _firestoreService;

        [ObservableProperty]
        private DateTime _selectedDate = DateTime.Today;

        [ObservableProperty]
        private ObservableCollection<AppointmentSlot> _slots = new();

        [ObservableProperty]
        private ObservableCollection<AppointmentSlot> _pendingRequests = new();

        // Statistics Properties
        [ObservableProperty]
        private int _totalSlots;

        [ObservableProperty]
        private int _pendingCount;

        [ObservableProperty]
        private int _confirmedCount;

        [ObservableProperty]
        private int _availableCount;

        // Time picker properties for creating slots
        [ObservableProperty]
        private TimeSpan _newSlotTime = new TimeSpan(10, 0, 0);

        [ObservableProperty]
        private int _slotDurationMinutes = 60;

        // Batch creation properties
        [ObservableProperty]
        private TimeSpan _batchStartTime = new TimeSpan(9, 0, 0);

        [ObservableProperty]
        private TimeSpan _batchEndTime = new TimeSpan(17, 0, 0);

        [ObservableProperty]
        private int _batchIntervalMinutes = 60;

        private IDisposable? _slotsListener;
        private IDisposable? _pendingRequestsListener;

        public AdminAgendaViewModel(IFirestoreService firestoreService)
            : base()
        {
            _firestoreService = firestoreService;
            Title = "Gestión de Agenda";
        }

        protected override async Task InitializeAsync()
        {
            await LoadDataAsync();
            SetupListeners();
        }

        [RelayCommand]
        private void Disappearing()
        {
            _slotsListener?.Dispose();
            _pendingRequestsListener?.Dispose();
        }

        private void SetupListeners()
        {
            // 1. Slots Listener for Selected Date
            _slotsListener?.Dispose();
            
            // Create DateTimeOffset from local date (automatically converts to UTC internally)
            var localStart = new DateTime(SelectedDate.Year, SelectedDate.Month, SelectedDate.Day, 0, 0, 0, DateTimeKind.Local);
            var localEnd = localStart.AddDays(1).AddTicks(-1);
            
            var startUtc = new DateTimeOffset(localStart);
            var endUtc = new DateTimeOffset(localEnd);
            
            _slotsListener = _firestoreService.ListenToAppointments(startUtc, endUtc, async (slots) =>
            {
                await EnrichSlotsWithUserData(slots);
                Slots = new ObservableCollection<AppointmentSlot>(slots);
                UpdateStatistics();
            });

            // 2. Pending Requests Listener (Only setup once)
            if (_pendingRequestsListener == null)
            {
                _pendingRequestsListener = _firestoreService.ListenToAppointments(
                    DateTimeOffset.MinValue, 
                    DateTimeOffset.MaxValue, 
                    async (allSlots) =>
                    {
                        var pending = allSlots
                            .Where(x => x.Status == AppointmentStatus.PendingApproval || 
                                       x.Status == AppointmentStatus.ModificationRequested)
                            .OrderBy(x => x.StartTime)
                            .ToList();
                        
                        await EnrichSlotsWithUserData(pending);
                        PendingRequests = new ObservableCollection<AppointmentSlot>(pending);
                        UpdateStatistics();
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
                await LoadSlotsAsync();
                await LoadPendingRequestsAsync();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading admin agenda data: {ex.Message}");
            }
            finally { IsBusy = false; }
        }

        partial void OnSelectedDateChanged(DateTime value)
        {
            // Update slots listener for the new date
            _slotsListener?.Dispose();
            // Create DateTimeOffset from local date (automatically converts to UTC internally)
            var localStart = new DateTime(value.Year, value.Month, value.Day, 0, 0, 0, DateTimeKind.Local);
            var localEnd = localStart.AddDays(1).AddTicks(-1);
            
            var startUtc = new DateTimeOffset(localStart);
            var endUtc = new DateTimeOffset(localEnd);
            
            _slotsListener = _firestoreService.ListenToAppointments(startUtc, endUtc, async (slots) =>
            {
                await EnrichSlotsWithUserData(slots);
                Slots = new ObservableCollection<AppointmentSlot>(slots);
                UpdateStatistics();
            });
        }

        private async Task LoadSlotsAsync()
        {
            // Create DateTimeOffset from local date (automatically converts to UTC internally)
            var localStart = new DateTime(SelectedDate.Year, SelectedDate.Month, SelectedDate.Day, 0, 0, 0, DateTimeKind.Local);
            var localEnd = localStart.AddDays(1).AddTicks(-1);
            
            var startUtc = new DateTimeOffset(localStart);
            var endUtc = new DateTimeOffset(localEnd);

            var slots = await _firestoreService.GetAllSlotsAsync(startUtc, endUtc);
            await EnrichSlotsWithUserData(slots);
            Slots = new ObservableCollection<AppointmentSlot>(slots);
            UpdateStatistics();
        }

        private async Task LoadPendingRequestsAsync()
        {
            var pending = await _firestoreService.GetPendingAppointmentsAsync();
            await EnrichSlotsWithUserData(pending);
            PendingRequests = new ObservableCollection<AppointmentSlot>(pending);
            UpdateStatistics();
        }

        private async Task EnrichSlotsWithUserData(List<AppointmentSlot> slots)
        {
            foreach (var slot in slots)
            {
                if (!string.IsNullOrEmpty(slot.UserId))
                {
                    slot.User = await _firestoreService.GetUserAsync(slot.UserId);
                }
            }
        }

        private void UpdateStatistics()
        {
            TotalSlots = Slots.Count;
            PendingCount = PendingRequests.Count;
            ConfirmedCount = Slots.Count(s => s.Status == AppointmentStatus.Confirmed);
            AvailableCount = Slots.Count(s => s.Status == AppointmentStatus.Available);
        }

        [RelayCommand]
        private async Task CreateSlot()
        {
            try
            {
                IsBusy = true;
                // Create DateTimeOffset from Local time (automatically converts to UTC internally)
                var localStartTime = new DateTime(
                    SelectedDate.Year, SelectedDate.Month, SelectedDate.Day,
                    NewSlotTime.Hours, NewSlotTime.Minutes, NewSlotTime.Seconds,
                    DateTimeKind.Local);
                
                var startTime = new DateTimeOffset(localStartTime);
                var endTime = startTime.AddMinutes(SlotDurationMinutes);

                var slot = new AppointmentSlot
                {
                    StartTime = startTime,
                    EndTime = endTime,
                    DurationMinutes = SlotDurationMinutes,
                    Status = AppointmentStatus.Available
                };

                await _firestoreService.CreateSlotAsync(slot);
                await LoadSlotsAsync();
                await App.Current.MainPage.DisplayAlert("Éxito", "Turno creado correctamente", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating slot: {ex.Message}");
                await App.Current.MainPage.DisplayAlert("Error", "No se pudo crear el turno", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task BatchCreateSlots()
        {
            try
            {
                IsBusy = true;
                var currentTime = BatchStartTime;
                var slotsCreated = 0;

                while (currentTime < BatchEndTime)
                {
                    // Create DateTimeOffset from Local time (automatically converts to UTC internally)
                    var localSlotStart = new DateTime(
                        SelectedDate.Year, SelectedDate.Month, SelectedDate.Day,
                        currentTime.Hours, currentTime.Minutes, currentTime.Seconds,
                        DateTimeKind.Local);
                    
                    var slotStart = new DateTimeOffset(localSlotStart);
                    var slotEnd = slotStart.AddMinutes(BatchIntervalMinutes);

                    var slot = new AppointmentSlot
                    {
                        StartTime = slotStart,
                        EndTime = slotEnd,
                        DurationMinutes = BatchIntervalMinutes,
                        Status = AppointmentStatus.Available
                    };

                    await _firestoreService.CreateSlotAsync(slot);
                    slotsCreated++;
                    currentTime = currentTime.Add(TimeSpan.FromMinutes(BatchIntervalMinutes));
                }

                await LoadSlotsAsync();
                await App.Current.MainPage.DisplayAlert("Éxito", $"Se crearon {slotsCreated} turnos", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error batch creating slots: {ex.Message}");
                await App.Current.MainPage.DisplayAlert("Error", "No se pudieron crear los turnos", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task EditSlot(AppointmentSlot slot)
        {
            if (slot == null) return;

            // Create and show time picker dialog
            var dialog = new Views.Dialogs.TimePickerDialog(slot.StartTime.ToLocalTime().TimeOfDay);
            await App.Current.MainPage.Navigation.PushModalAsync(dialog);
            var newTime = await dialog.GetResultAsync();

            // User cancelled
            if (newTime == null)
                return;

            try
            {
                IsBusy = true;
                // Create DateTimeOffset from Local time (automatically converts to UTC internally)
                var localStartTime = new DateTime(
                    SelectedDate.Year, SelectedDate.Month, SelectedDate.Day,
                    newTime.Value.Hours, newTime.Value.Minutes, newTime.Value.Seconds,
                    DateTimeKind.Local);

                slot.StartTime = new DateTimeOffset(localStartTime);
                slot.EndTime = slot.StartTime.AddMinutes(60);

                await _firestoreService.UpdateSlotAsync(slot);
                await LoadSlotsAsync();
                await App.Current.MainPage.DisplayAlert("Éxito", "Turno actualizado", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error editing slot: {ex.Message}");
                await App.Current.MainPage.DisplayAlert("Error", "No se pudo editar el turno", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ApproveRequest(AppointmentSlot slot)
        {
            if (slot == null) return;

            // Prompt for admin notes
            var adminNotes = await App.Current.MainPage.DisplayPromptAsync(
                "Aprobar Solicitud",
                "Notas (opcional):",
                "Aprobar",
                "Cancelar",
                placeholder: "Agregar notas para el usuario...",
                maxLength: 200);

            if (adminNotes == null) // User cancelled
                return;

            try
            {
                IsBusy = true;
                
                if (slot.Status == AppointmentStatus.ModificationRequested)
                {
                    if (slot.RequestedModification?.Contains("cancelación", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        // Cancellation approved: Free the slot
                        var userId = slot.UserId;
                        slot.Status = AppointmentStatus.Available;
                        slot.UserId = null;
                        slot.UserNotes = null;
                        slot.RequestedModification = null;
                        slot.AdminNotes = null; // Clear admin notes as it's now available

                        await _firestoreService.UpdateSlotAsync(slot);
                        PendingRequests.Remove(slot);
                        UpdateStatistics();

                        if (!string.IsNullOrEmpty(userId))
                        {
                            var cancelNotification = new Notification
                            {
                                UserId = userId,
                                Title = "Cancelación Aprobada",
                                Message = $"Tu solicitud de cancelación para el {slot.StartTime.ToLocalTime():dd/MM HH:mm} ha sido aprobada.",
                                Type = NotificationType.Info,
                                RelatedEntityId = slot.Id
                            };
                            await _firestoreService.CreateNotificationAsync(cancelNotification);
                        }
                        
                        _ = LoadDataAsync();
                        await App.Current.MainPage.DisplayAlert("Éxito", "Cancelación aprobada. El turno está disponible nuevamente.", "OK");
                        return; // Exit early as we handled it differently
                    }
                    else
                    {
                        // Other modification approved: Confirm
                        slot.Status = AppointmentStatus.Confirmed;
                        slot.RequestedModification = null;
                    }
                }
                else
                {
                    slot.Status = AppointmentStatus.Confirmed;
                }

                if (!string.IsNullOrWhiteSpace(adminNotes))
                {
                    slot.AdminNotes = adminNotes;
                }

                await _firestoreService.UpdateSlotAsync(slot);
                
                // Remove from pending list immediately
                PendingRequests.Remove(slot);
                UpdateStatistics();

                // Notify user
                var notification = new Notification
                {
                    UserId = slot.UserId,
                    Title = "Reserva Aprobada",
                    Message = $"Tu reserva para el {slot.StartTime.ToLocalTime():dd/MM HH:mm} ha sido confirmada." + 
                              (!string.IsNullOrWhiteSpace(adminNotes) ? $"\nNota: {adminNotes}" : ""),
                    Type = NotificationType.Success,
                    RelatedEntityId = slot.Id
                };
                await _firestoreService.CreateNotificationAsync(notification);

                // Refresh full data in background to be sure
                _ = LoadDataAsync();
                
                await App.Current.MainPage.DisplayAlert("Éxito", "Solicitud aprobada", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error approving request: {ex.Message}");
                await App.Current.MainPage.DisplayAlert("Error", "No se pudo aprobar la solicitud", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task RejectRequest(AppointmentSlot slot)
        {
            if (slot == null) return;

            // Prompt for rejection reason
            var reason = await App.Current.MainPage.DisplayPromptAsync(
                "Rechazar Solicitud",
                "Motivo del rechazo:",
                "Rechazar",
                "Cancelar",
                placeholder: "Explica el motivo...",
                maxLength: 200);

            if (reason == null) // User cancelled
                return;

            try
            {
                IsBusy = true;
                
                string notificationTitle = "Solicitud Rechazada";
                string notificationMessage = "";
                var userId = slot.UserId;

                if (slot.Status == AppointmentStatus.ModificationRequested)
                {
                    // Rejecting a modification/cancellation request means keeping the slot CONFIRMED
                    // The user stays assigned.
                    slot.Status = AppointmentStatus.Confirmed;
                    slot.RequestedModification = null;
                    
                    notificationTitle = "Solicitud de Modificación Rechazada";
                    notificationMessage = $"Tu solicitud de modificación/cancelación para el {slot.StartTime.ToLocalTime():dd/MM HH:mm} ha sido rechazada. El turno sigue confirmado.";
                }
                else
                {
                    // Rejecting a new booking request (PendingApproval) means freeing the slot
                    slot.Status = AppointmentStatus.Available;
                    slot.UserId = null;
                    slot.UserNotes = null;
                    slot.RequestedModification = null;
                    slot.AdminNotes = null;

                    notificationTitle = "Reserva Rechazada";
                    notificationMessage = $"Tu solicitud de reserva para el {slot.StartTime.ToLocalTime():dd/MM HH:mm} no ha sido aprobada.";
                }

                await _firestoreService.UpdateSlotAsync(slot);

                // Remove from pending list immediately
                PendingRequests.Remove(slot);
                UpdateStatistics();

                if (!string.IsNullOrEmpty(userId))
                {
                    var notification = new Notification
                    {
                        UserId = userId,
                        Title = notificationTitle,
                        Message = notificationMessage + (!string.IsNullOrWhiteSpace(reason) ? $"\nMotivo: {reason}" : ""),
                        Type = NotificationType.Warning,
                        RelatedEntityId = slot.Id
                    };
                    await _firestoreService.CreateNotificationAsync(notification);
                }

                // Refresh full data in background
                _ = LoadDataAsync();

                await App.Current.MainPage.DisplayAlert("Confirmado", "Solicitud rechazada", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error rejecting request: {ex.Message}");
                await App.Current.MainPage.DisplayAlert("Error", "No se pudo rechazar la solicitud", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task DeleteSlot(AppointmentSlot slot)
        {
            if (slot == null || slot.Id == null) return;

            var confirm = await App.Current.MainPage.DisplayAlert(
                "Eliminar Turno", 
                "¿Estás seguro de que deseas eliminar este horario?", 
                "Sí", 
                "No");
            
            if (!confirm) return;

            try
            {
                IsBusy = true;
                await _firestoreService.DeleteSlotAsync(slot.Id);
                await LoadSlotsAsync();
                await App.Current.MainPage.DisplayAlert("Éxito", "Turno eliminado", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting slot: {ex.Message}");
                await App.Current.MainPage.DisplayAlert("Error", "No se pudo eliminar el turno", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
