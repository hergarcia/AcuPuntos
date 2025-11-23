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
        }

        private async Task LoadTransactions()
        {
            await ExecuteAsync(async () =>
            {
                if (CurrentUser == null)
                    return;

                var allTransactions = await _firestoreService.GetUserTransactionsAsync(CurrentUser.Uid!);

                Transactions.Clear();

                foreach (var transaction in allTransactions)
                {
                    Transactions.Add(transaction);
                }

                CalculateStatistics();
                FilterTransactions();
            }, "Cargando historial...");
        }

        private void CalculateStatistics()
        {
            if (CurrentUser == null)
                return;

            TotalTransactions = Transactions.Count;

            TotalPointsEarned = Transactions
                .Where(t => t.Type == TransactionType.Received || t.Type == TransactionType.Reward)
                .Sum(t => t.Amount);

            TotalPointsSpent = Transactions
                .Where(t => t.Type == TransactionType.Sent || t.Type == TransactionType.Redemption)
                .Sum(t => t.Amount);
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

        [RelayCommand]
        private async Task RefreshHistory()
        {
            await LoadTransactions();
        }
    }
}
