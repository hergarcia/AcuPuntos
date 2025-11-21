using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AcuPuntos.Models;
using AcuPuntos.Services;
using AcuPuntos.Views;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AcuPuntos.ViewModels
{
    public partial class ProfileViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly IFirestoreService _firestoreService;
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        private User? currentUser;

        [ObservableProperty]
        private Dictionary<string, object>? userStats;

        [ObservableProperty]
        private int totalTransactions;

        [ObservableProperty]
        private int totalRedemptions;

        [ObservableProperty]
        private int totalPointsEarned;

        [ObservableProperty]
        private int totalPointsSpent;

        public ProfileViewModel(IAuthService authService, IFirestoreService firestoreService, INavigationService navigationService)
        {
            _authService = authService;
            _firestoreService = firestoreService;
            _navigationService = navigationService;
            Title = "Mi Perfil";
        }

        protected override async Task OnAppearingAsync()
        {
            await base.OnAppearingAsync();

            CurrentUser = _authService.CurrentUser;
            await LoadUserStats();
        }

        private async Task LoadUserStats()
        {
            await ExecuteAsync(async () =>
            {
                if (CurrentUser == null)
                    return;

                // Cargar estadísticas
                UserStats = await _firestoreService.GetUserStatsAsync(CurrentUser.Uid!);

                // Extraer valores
                if (UserStats != null)
                {
                    TotalTransactions = UserStats.ContainsKey("totalTransactions")
                        ? Convert.ToInt32(UserStats["totalTransactions"])
                        : 0;

                    TotalRedemptions = UserStats.ContainsKey("totalRedemptions")
                        ? Convert.ToInt32(UserStats["totalRedemptions"])
                        : 0;

                    TotalPointsEarned = UserStats.ContainsKey("totalPointsEarned")
                        ? Convert.ToInt32(UserStats["totalPointsEarned"])
                        : 0;

                    TotalPointsSpent = UserStats.ContainsKey("totalPointsSpent")
                        ? Convert.ToInt32(UserStats["totalPointsSpent"])
                        : 0;
                }
            }, "Cargando estadísticas...");
        }

        [RelayCommand]
        private async Task RefreshProfile()
        {
            if (CurrentUser == null)
                return;

            CurrentUser = await _firestoreService.GetUserAsync(CurrentUser.Uid!);
            await LoadUserStats();
        }

        [RelayCommand]
        private async Task Logout()
        {
            var confirm = await Shell.Current.DisplayAlert(
                "Cerrar Sesión",
                "¿Estás seguro de que quieres cerrar sesión?",
                "Sí",
                "No");

            if (!confirm)
                return;

            await _authService.SignOutAsync();

            // Navegar a la pantalla de login
            await _navigationService.NavigateToRootAsync("login");
        }

        [RelayCommand]
        private async Task EditProfile()
        {
            await Shell.Current.DisplayAlert(
                "Próximamente",
                "La edición de perfil estará disponible pronto.",
                "OK");
        }
    }
}
