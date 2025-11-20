using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AcuPuntos.Models;
using AcuPuntos.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace AcuPuntos.ViewModels
{
    [QueryProperty(nameof(User), "User")]
    public partial class UserDetailViewModel : BaseViewModel
    {
        private readonly IFirestoreService _firestoreService;

        [ObservableProperty]
        private User? user;

        [ObservableProperty]
        private ObservableCollection<Transaction> recentTransactions;

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

        public UserDetailViewModel(IFirestoreService firestoreService)
        {
            _firestoreService = firestoreService;
            Title = "Detalle de Usuario";
            RecentTransactions = new ObservableCollection<Transaction>();
        }

        protected override async Task OnAppearingAsync()
        {
            await base.OnAppearingAsync();
            await LoadUserDetails();
        }

        partial void OnUserChanged(User? value)
        {
            if (value != null)
            {
                Title = value.DisplayName ?? "Detalle de Usuario";
            }
        }

        private async Task LoadUserDetails()
        {
            await ExecuteAsync(async () =>
            {
                if (User == null || string.IsNullOrEmpty(User.Uid))
                    return;

                // Cargar usuario actualizado
                User = await _firestoreService.GetUserAsync(User.Uid);

                // Cargar estadísticas
                UserStats = await _firestoreService.GetUserStatsAsync(User.Uid);

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

                // Cargar transacciones recientes (últimas 10)
                var transactions = await _firestoreService.GetUserTransactionsAsync(User.Uid);
                RecentTransactions.Clear();
                foreach (var transaction in transactions.Take(10))
                {
                    RecentTransactions.Add(transaction);
                }

            }, "Cargando detalles...");
        }

        [RelayCommand]
        private async Task AssignPoints()
        {
            if (User == null)
                return;

            var pointsStr = await Shell.Current.DisplayPromptAsync(
                "Asignar Puntos",
                $"¿Cuántos puntos deseas asignar a {User.DisplayName}?",
                "Asignar",
                "Cancelar",
                keyboard: Keyboard.Numeric);

            if (string.IsNullOrWhiteSpace(pointsStr))
                return;

            if (!int.TryParse(pointsStr, out int points) || points <= 0)
            {
                await Shell.Current.DisplayAlert("Error", "Ingresa una cantidad válida de puntos", "OK");
                return;
            }

            var description = await Shell.Current.DisplayPromptAsync(
                "Descripción",
                "Motivo de la asignación:",
                "Asignar",
                "Cancelar",
                placeholder: "Ej: Participación en evento");

            if (string.IsNullOrWhiteSpace(description))
                description = "Asignación por administrador";

            await ExecuteAsync(async () =>
            {
                var success = await _firestoreService.AssignPointsToUserAsync(User.Uid!, points, description);

                if (success)
                {
                    await Shell.Current.DisplayAlert("¡Éxito!", $"Se han asignado {points} puntos a {User.DisplayName}", "OK");
                    await LoadUserDetails();
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error", "No se pudieron asignar los puntos", "OK");
                }
            }, "Asignando puntos...");
        }

        [RelayCommand]
        private async Task RefreshUserDetail()
        {
            await LoadUserDetails();
        }
    }
}
