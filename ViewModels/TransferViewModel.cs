using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AcuPuntos.Models;
using AcuPuntos.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace AcuPuntos.ViewModels
{
    public partial class TransferViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly IFirestoreService _firestoreService;
        private readonly IGamificationService _gamificationService;

        [ObservableProperty]
        private User? currentUser;

        [ObservableProperty]
        private ObservableCollection<User> users;

        [ObservableProperty]
        private ObservableCollection<User> filteredUsers;

        [ObservableProperty]
        private User? selectedUser;

        [ObservableProperty]
        private string searchText = "";

        [ObservableProperty]
        private string pointsToTransfer = "";

        [ObservableProperty]
        private string description = "";

        [ObservableProperty]
        private bool canTransfer;

        [ObservableProperty]
        private string errorMessage = "";

        public TransferViewModel(IAuthService authService, IFirestoreService firestoreService, IGamificationService gamificationService)
        {
            _authService = authService;
            _firestoreService = firestoreService;
            _gamificationService = gamificationService;
            Title = "Transferir Puntos";
            Users = new ObservableCollection<User>();
            FilteredUsers = new ObservableCollection<User>();
        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            CurrentUser = _authService.CurrentUser;
            await LoadUsers();
        }

        private async Task LoadUsers()
        {
            await ExecuteAsync(async () =>
            {
                var allUsers = await _firestoreService.GetAllUsersAsync();
                
                Users.Clear();
                FilteredUsers.Clear();
                
                foreach (var user in allUsers)
                {
                    // No mostrar el usuario actual en la lista
                    if (user.Uid != CurrentUser?.Uid)
                    {
                        Users.Add(user);
                        FilteredUsers.Add(user);
                    }
                }
            }, "Cargando usuarios...");
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterUsers();
        }

        partial void OnPointsToTransferChanged(string value)
        {
            ValidateTransfer();
        }

        partial void OnSelectedUserChanged(User? value)
        {
            ValidateTransfer();
        }

        private void FilterUsers()
        {
            FilteredUsers.Clear();
            
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                foreach (var user in Users)
                {
                    FilteredUsers.Add(user);
                }
            }
            else
            {
                var searchLower = SearchText.ToLower();
                foreach (var user in Users)
                {
                    if (user.DisplayName?.ToLower().Contains(searchLower) == true ||
                        user.Email?.ToLower().Contains(searchLower) == true)
                    {
                        FilteredUsers.Add(user);
                    }
                }
            }
        }

        private void ValidateTransfer()
        {
            ErrorMessage = "";
            CanTransfer = false;
            
            if (SelectedUser == null)
            {
                ErrorMessage = "Selecciona un usuario";
                return;
            }
            
            if (!int.TryParse(PointsToTransfer, out int points))
            {
                if (!string.IsNullOrEmpty(PointsToTransfer))
                    ErrorMessage = "Ingresa una cantidad válida";
                return;
            }
            
            if (points <= 0)
            {
                ErrorMessage = "La cantidad debe ser mayor a 0";
                return;
            }
            
            if (CurrentUser != null && points > CurrentUser.Points)
            {
                ErrorMessage = $"No tienes suficientes puntos (máximo: {CurrentUser.Points})";
                return;
            }
            
            CanTransfer = true;
            ErrorMessage = "";
        }

        [RelayCommand(CanExecute = nameof(CanTransfer))]
        private async Task TransferPoints()
        {
            if (CurrentUser == null || SelectedUser == null)
                return;
            
            if (!int.TryParse(PointsToTransfer, out int points))
                return;
            
            var confirm = await Shell.Current.DisplayAlert(
                "Confirmar Transferencia",
                $"¿Enviar {points} puntos a {SelectedUser.DisplayName}?",
                "Enviar",
                "Cancelar");
            
            if (!confirm)
                return;
            
            await ExecuteAsync(async () =>
            {
                var desc = string.IsNullOrWhiteSpace(Description) 
                    ? $"Transferencia a {SelectedUser.DisplayName}"
                    : Description;
                
                var success = await _firestoreService.TransferPointsAsync(
                    CurrentUser.Uid!,
                    SelectedUser.Uid!,
                    points,
                    desc);
                
                if (success)
                {
                    // Dar experiencia por la transferencia (5% de los puntos transferidos)
                    int xpGained = Math.Max(1, points / 20);
                    await _gamificationService.AddExperienceAsync(CurrentUser.Uid!, xpGained, $"Transferencia de {points} puntos");

                    await Shell.Current.DisplayAlert(
                        "¡Éxito!",
                        $"Se han transferido {points} puntos a {SelectedUser.DisplayName}\n+{xpGained} XP ganada",
                        "OK");

                    // Limpiar formulario
                    SelectedUser = null;
                    PointsToTransfer = "";
                    Description = "";
                    SearchText = "";

                    // Actualizar usuario actual
                    CurrentUser = await _firestoreService.GetUserAsync(CurrentUser.Uid!);
                }
                else
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "No se pudo completar la transferencia. Verifica que tienes suficientes puntos.",
                        "OK");
                }
            }, "Transfiriendo puntos...");
        }

        [RelayCommand]
        private void SelectUser(User user)
        {
            SelectedUser = user;
        }
    }
}
