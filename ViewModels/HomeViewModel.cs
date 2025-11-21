using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AcuPuntos.Models;
using AcuPuntos.Services;
using AcuPuntos.Views;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace AcuPuntos.ViewModels
{
    public partial class HomeViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly IFirestoreService _firestoreService;
        private readonly IGamificationService _gamificationService;
        private readonly INavigationService _navigationService;
        private IDisposable? _userListener;
        private IDisposable? _transactionsListener;

        [ObservableProperty]
        private User? currentUser;

        [ObservableProperty]
        private ObservableCollection<Transaction> recentTransactions;

        [ObservableProperty]
        private string greetingMessage = "";

        [ObservableProperty]
        private string pointsDisplay = "0";

        [ObservableProperty]
        private bool hasTransactions;

        [ObservableProperty]
        private bool canCheckIn = false;

        [ObservableProperty]
        private int consecutiveDays = 0;

        public HomeViewModel(IAuthService authService, IFirestoreService firestoreService, IGamificationService gamificationService, INavigationService navigationService)
        {
            _authService = authService;
            _firestoreService = firestoreService;
            _gamificationService = gamificationService;
            _navigationService = navigationService;
            Title = "Inicio";
            RecentTransactions = new ObservableCollection<Transaction>();

            UpdateGreeting();
        }

        protected override async Task OnAppearingAsync()
        {
            await base.OnAppearingAsync();
            
            CurrentUser = _authService.CurrentUser;
            
            if (CurrentUser != null)
            {
                UpdatePointsDisplay();
                
                // Suscribirse a cambios en tiempo real
                _userListener = _firestoreService.ListenToUserChanges(CurrentUser.Uid, user =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        CurrentUser = user;
                        UpdatePointsDisplay();
                    });
                });
                
                _transactionsListener = _firestoreService.ListenToTransactions(CurrentUser.Uid, transactions =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        RecentTransactions.Clear();
                        foreach (var transaction in transactions.Take(5))
                        {
                            RecentTransactions.Add(transaction);
                        }
                        HasTransactions = RecentTransactions.Any();
                    });
                });
                
                await LoadTransactions();
                await CheckDailyCheckInStatus();
            }
        }

        private async Task CheckDailyCheckInStatus()
        {
            if (CurrentUser == null) return;

            // Verificar si puede hacer check-in hoy
            if (CurrentUser.LastCheckIn.HasValue)
            {
                var timeSinceLastCheckIn = DateTimeOffset.UtcNow - CurrentUser.LastCheckIn.Value;
                CanCheckIn = timeSinceLastCheckIn.TotalHours >= 20; // Puede hacer check-in después de 20 horas
            }
            else
            {
                CanCheckIn = true; // Primera vez
            }

            ConsecutiveDays = CurrentUser.ConsecutiveDays;
        }

        protected override async Task OnDisappearingAsync()
        {
            await base.OnDisappearingAsync();
            
            // Limpiar listeners
            _userListener?.Dispose();
            _transactionsListener?.Dispose();
        }

        private async Task LoadTransactions()
        {
            if (CurrentUser == null) return;
            
            await ExecuteAsync(async () =>
            {
                var transactions = await _firestoreService.GetUserTransactionsAsync(CurrentUser.Uid, 5);
                
                RecentTransactions.Clear();
                foreach (var transaction in transactions)
                {
                    RecentTransactions.Add(transaction);
                }
                
                HasTransactions = RecentTransactions.Any();
            });
        }

        private void UpdateGreeting()
        {
            var hour = DateTime.Now.Hour;
            
            if (hour < 12)
                GreetingMessage = "Buenos días";
            else if (hour < 18)
                GreetingMessage = "Buenas tardes";
            else
                GreetingMessage = "Buenas noches";
        }

        private void UpdatePointsDisplay()
        {
            if (CurrentUser != null)
            {
                PointsDisplay = CurrentUser.Points.ToString("N0");
            }
        }

        [RelayCommand]
        private async Task GoToTransfer()
        {
            await _navigationService.NavigateToAsync(nameof(TransferPage));
        }

        [RelayCommand]
        private async Task GoToRewards()
        {
            await _navigationService.NavigateToAsync(nameof(RewardsPage));
        }

        [RelayCommand]
        private async Task GoToHistory()
        {
            await _navigationService.NavigateToAsync(nameof(HistoryPage));
        }

        [RelayCommand]
        private async Task GoToAdmin()
        {
            await _navigationService.NavigateToAsync(nameof(AdminPage));
        }

        [RelayCommand]
        private async Task RefreshData()
        {
            await LoadTransactions();
        }

        [RelayCommand(CanExecute = nameof(CanCheckIn))]
        private async Task DailyCheckIn()
        {
            if (CurrentUser == null) return;

            await ExecuteAsync(async () =>
            {
                var (success, bonus, streak) = await _gamificationService.DailyCheckInAsync(CurrentUser.Uid!);

                if (success)
                {
                    await Shell.Current.DisplayAlert(
                        "¡Check-in Completado! 🎉",
                        $"Has ganado {bonus} puntos\nRacha actual: {streak} días consecutivos",
                        "¡Genial!");

                    // Actualizar usuario
                    CurrentUser = await _firestoreService.GetUserAsync(CurrentUser.Uid!);
                    UpdatePointsDisplay();
                    await CheckDailyCheckInStatus();
                }
                else
                {
                    await Shell.Current.DisplayAlert(
                        "Ya hiciste check-in hoy",
                        "Vuelve mañana para continuar tu racha",
                        "OK");
                }
            }, "Procesando check-in...");
        }
    }
}
