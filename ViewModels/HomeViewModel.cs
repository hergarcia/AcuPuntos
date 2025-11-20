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

        public HomeViewModel(IAuthService authService, IFirestoreService firestoreService)
        {
            _authService = authService;
            _firestoreService = firestoreService;
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
            }
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
            await Shell.Current.GoToAsync(nameof(TransferPage));
        }

        [RelayCommand]
        private async Task GoToRewards()
        {
            await Shell.Current.GoToAsync(nameof(RewardsPage));
        }

        [RelayCommand]
        private async Task GoToHistory()
        {
            await Shell.Current.GoToAsync(nameof(HistoryPage));
        }

        [RelayCommand]
        private async Task GoToProfile()
        {
            await Shell.Current.GoToAsync(nameof(ProfilePage));
        }

        [RelayCommand]
        private async Task GoToAdmin()
        {
            await Shell.Current.GoToAsync(nameof(AdminPage));
        }

        [RelayCommand]
        private async Task RefreshData()
        {
            await LoadTransactions();
        }

        [RelayCommand]
        private async Task SignOut()
        {
            var confirm = await Shell.Current.DisplayAlert(
                "Cerrar sesión",
                "¿Estás seguro de que deseas cerrar sesión?",
                "Sí",
                "Cancelar");
            
            if (confirm)
            {
                await _authService.SignOutAsync();
                await Shell.Current.GoToAsync("//login");
            }
        }
    }
}
