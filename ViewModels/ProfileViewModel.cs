using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AcuPuntos.Models;
using AcuPuntos.Services;
using AcuPuntos.Views;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace AcuPuntos.ViewModels
{
    public partial class ProfileViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly IFirestoreService _firestoreService;
        private readonly IGamificationService _gamificationService;
        private readonly INavigationService _navigationService;
        private readonly IThemeService _themeService;
        private IDisposable? _userListener;

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

        [ObservableProperty]
        private ObservableCollection<UserBadge> userBadges;

        [ObservableProperty]
        private ObservableCollection<Notification> notifications;

        [ObservableProperty]
        private int badgesCount;

        [ObservableProperty]
        private bool hasBadges;

        [ObservableProperty]
        private bool isDarkMode;

        public ProfileViewModel(IAuthService authService, IFirestoreService firestoreService, IGamificationService gamificationService, INavigationService navigationService, IThemeService themeService)
        {
            _authService = authService;
            _firestoreService = firestoreService;
            _gamificationService = gamificationService;
            _navigationService = navigationService;
            _themeService = themeService;
            Title = "Mi Perfil";
            UserBadges = new ObservableCollection<UserBadge>();
            Notifications = new ObservableCollection<Notification>();
            
            // Initialize dark mode state
            IsDarkMode = _themeService.CurrentTheme == AppTheme.Dark;
        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            CurrentUser = _authService.CurrentUser;

            if (CurrentUser != null)
            {
                // Recargar datos actualizados del usuario desde Firestore
                CurrentUser = await _firestoreService.GetUserAsync(CurrentUser.Uid!);
            }

            await LoadUserStats();
            await LoadUserStats();
            await LoadUserBadges();
            await LoadNotifications();
            SubscribeToUpdates();
        }

        private async Task LoadUserStats(bool silent = false)
        {
            Func<Task> operation = async () =>
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
            };

            if (silent)
            {
                try
                {
                    await operation();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading user stats silently: {ex.Message}");
                }
            }
            else
            {
                await ExecuteAsync(operation, "Cargando estadísticas...");
            }
        }

        private async Task LoadUserBadges(bool silent = false)
        {
            Func<Task> operation = async () =>
            {
                if (CurrentUser == null)
                    return;

                // Verificar y otorgar badges automáticamente según nivel/puntos actuales
                await _gamificationService.CheckAndAwardBadgesAsync(CurrentUser.Uid!);

                // Cargar badges del usuario (incluyendo los recién otorgados)
                var badges = await _gamificationService.GetUserBadgesAsync(CurrentUser.Uid!);

                UserBadges.Clear();
                foreach (var badge in badges)
                {
                    UserBadges.Add(badge);
                }

                BadgesCount = badges.Count;
                HasBadges = badges.Count > 0;
            };

            if (silent)
            {
                try
                {
                    await operation();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading user badges silently: {ex.Message}");
                }
            }
            else
            {
                await ExecuteAsync(operation);
            }
        }

        private async Task LoadNotifications()
        {
            if (CurrentUser == null) return;
            var notifs = await _firestoreService.GetUserNotificationsAsync(CurrentUser.Uid!);
            Notifications = new ObservableCollection<Notification>(notifs);
        }

        [RelayCommand]
        private async Task MarkNotificationAsRead(Notification notification)
        {
            if (notification == null) return;
            await _firestoreService.MarkNotificationAsReadAsync(notification.Id!);
            notification.IsRead = true;
            // Optionally remove from list or just update UI
            // Notifications.Remove(notification); 
        }

        protected override async Task OnAppearingAsync()
        {
            await base.OnAppearingAsync();
        }

        protected override async Task OnDisappearingAsync()
        {
            await base.OnDisappearingAsync();
        }

        private void SubscribeToUpdates()
        {
            if (CurrentUser == null || string.IsNullOrEmpty(CurrentUser.Uid)) return;

            UnsubscribeUpdates();

            _userListener = _firestoreService.ListenToUserChanges(CurrentUser.Uid, user =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    CurrentUser = user;
                    // Recargar estadísticas y badges cuando cambia el usuario (ej. puntos)
                    await LoadUserStats(silent: true);
                    await LoadUserBadges(silent: true);
                });
            });
        }

        private void UnsubscribeUpdates()
        {
            _userListener?.Dispose();
            _userListener = null;
        }

        [RelayCommand]
        private async Task RefreshProfile()
        {
            if (CurrentUser == null)
                return;

            CurrentUser = await _firestoreService.GetUserAsync(CurrentUser.Uid!);
            await LoadUserStats();
            await LoadUserBadges();
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

        partial void OnIsDarkModeChanged(bool value)
        {
            // When IsDarkMode changes, update the theme
            var newTheme = value ? AppTheme.Dark : AppTheme.Light;
            _themeService.SetTheme(newTheme);
        }
    }
}
