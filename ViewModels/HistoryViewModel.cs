using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AcuPuntos.Models;
using AcuPuntos.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace AcuPuntos.ViewModels
{
    public partial class HistoryViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly IFirestoreService _firestoreService;
        private IDisposable? _transactionsListener;

        [ObservableProperty]
        private User? currentUser;

        [ObservableProperty]
        private ObservableCollection<Transaction> transactions;

        [ObservableProperty]
        private ObservableCollection<Transaction> filteredTransactions;

        [ObservableProperty]
        private string searchText = "";

        [ObservableProperty]
        private string selectedFilter = "Todas";

        private List<string> _filters = new List<string> { "Todas", "Enviadas", "Recibidas", "Canjes" };
        public List<string> Filters => _filters;

        [ObservableProperty]
        private int totalTransactions;

        [ObservableProperty]
        private int totalPointsEarned;

        [ObservableProperty]
        private int totalPointsSpent;

        public HistoryViewModel(IAuthService authService, IFirestoreService firestoreService)
        {
            _authService = authService;
            _firestoreService = firestoreService;
            Title = "Historial";
            Transactions = new ObservableCollection<Transaction>();
            FilteredTransactions = new ObservableCollection<Transaction>();
        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            CurrentUser = _authService.CurrentUser;
            await LoadTransactions();
            SubscribeToUpdates();
        }

        private async Task LoadTransactions(bool isRefresh = false)
        {
            if (isRefresh)
            {
                if (IsBusy) return;
                
                try
                {
                    // No seteamos IsBusy para mantener la lista visible y evitar el Skeleton
                    await ProcessTransactions();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
                    await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
                }
                finally
                {
                    IsRefreshing = false;
                }
            }
            else
            {
                await ExecuteAsync(async () =>
                {
                    await ProcessTransactions();
                }, "Cargando historial...");
            }
        }

        private async Task ProcessTransactions()
        {
            if (CurrentUser == null)
                return;

            // 1. Obtener datos (IO)
            var allTransactions = await _firestoreService.GetUserTransactionsAsync(CurrentUser.Uid!);

            // 2. Procesar datos en segundo plano (CPU)
            await Task.Run(() =>
            {
                // Calcular estadísticas en background
                var totalCount = allTransactions.Count;
                
                var earned = allTransactions
                    .Where(t => t.Type == TransactionType.Received || t.Type == TransactionType.Reward)
                    .Sum(t => t.Amount);

                var spent = allTransactions
                    .Where(t => t.Type == TransactionType.Sent || t.Type == TransactionType.Redemption)
                    .Sum(t => t.Amount);

                // Actualizar propiedades en el hilo principal luego
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TotalTransactions = totalCount;
                    TotalPointsEarned = earned;
                    TotalPointsSpent = spent;
                });
            });

            // 3. Actualizar colección en el hilo principal
            Transactions.Clear();
            foreach (var transaction in allTransactions)
            {
                Transactions.Add(transaction);
            }

            FilterTransactions();
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterTransactions();
        }

        partial void OnSelectedFilterChanged(string value)
        {
            FilterTransactions();
        }

        private void FilterTransactions()
        {
            FilteredTransactions.Clear();

            var filtered = Transactions.AsEnumerable();

            // Filtrar por texto
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = filtered.Where(t =>
                    t.Description?.ToLower().Contains(searchLower) == true);
            }

            // Filtrar por tipo
            if (SelectedFilter != "Todas")
            {
                filtered = SelectedFilter switch
                {
                    "Enviadas" => filtered.Where(t => t.Type == TransactionType.Sent),
                    "Recibidas" => filtered.Where(t => t.Type == TransactionType.Received || t.Type == TransactionType.Reward),
                    "Canjes" => filtered.Where(t => t.Type == TransactionType.Redemption),
                    _ => filtered
                };
            }

            foreach (var transaction in filtered)
            {
                FilteredTransactions.Add(transaction);
            }
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

            _transactionsListener = _firestoreService.ListenToTransactions(CurrentUser.Uid, transactions =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await UpdateTransactionsList(transactions);
                });
            });
        }

        private void UnsubscribeUpdates()
        {
            _transactionsListener?.Dispose();
            _transactionsListener = null;
        }

        private async Task UpdateTransactionsList(List<Transaction> allTransactions)
        {
            // Procesar datos en segundo plano (CPU)
            await Task.Run(() =>
            {
                // Calcular estadísticas en background
                var totalCount = allTransactions.Count;
                
                var earned = allTransactions
                    .Where(t => t.Type == TransactionType.Received || t.Type == TransactionType.Reward)
                    .Sum(t => t.Amount);

                var spent = allTransactions
                    .Where(t => t.Type == TransactionType.Sent || t.Type == TransactionType.Redemption)
                    .Sum(t => t.Amount);

                // Actualizar propiedades en el hilo principal luego
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TotalTransactions = totalCount;
                    TotalPointsEarned = earned;
                    TotalPointsSpent = spent;
                });
            });

            // Actualizar colección en el hilo principal
            Transactions.Clear();
            foreach (var transaction in allTransactions)
            {
                Transactions.Add(transaction);
            }

            FilterTransactions();
        }

        [RelayCommand]
        private async Task RefreshHistory()
        {
            await LoadTransactions(true);
        }
    }
}
