using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AcuPuntos.Models;
using AcuPuntos.Services;
using AcuPuntos.Views;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace AcuPuntos.ViewModels
{
    public partial class RewardsViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly IFirestoreService _firestoreService;

        [ObservableProperty]
        private User? currentUser;

        [ObservableProperty]
        private ObservableCollection<Reward> rewards;

        [ObservableProperty]
        private ObservableCollection<Reward> filteredRewards;

        [ObservableProperty]
        private string searchText = "";

        [ObservableProperty]
        private string selectedCategory = "Todas";

        private List<string> _categories = new List<string> { "Todas", "Servicios", "Productos", "Descuentos", "Especial" };
        public List<string> Categories => _categories;

        public RewardsViewModel(IAuthService authService, IFirestoreService firestoreService)
        {
            _authService = authService;
            _firestoreService = firestoreService;
            Title = "Recompensas";
            Rewards = new ObservableCollection<Reward>();
            FilteredRewards = new ObservableCollection<Reward>();
        }

        protected override async Task OnAppearingAsync()
        {
            await base.OnAppearingAsync();

            CurrentUser = _authService.CurrentUser;
            await LoadRewards();
        }

        private async Task LoadRewards()
        {
            await ExecuteAsync(async () =>
            {
                var allRewards = await _firestoreService.GetActiveRewardsAsync();

                Rewards.Clear();

                foreach (var reward in allRewards)
                {
                    // Verificar si el usuario puede canjear
                    reward.CanRedeem = CurrentUser != null && CurrentUser.Points >= reward.PointsCost;

                    if (!reward.CanRedeem && CurrentUser != null)
                    {
                        var pointsNeeded = reward.PointsCost - CurrentUser.Points;
                        reward.DisabledReason = $"Te faltan {pointsNeeded} puntos";
                    }

                    // Verificar si ha expirado
                    if (reward.ExpiryDate.HasValue && reward.ExpiryDate.Value < DateTime.UtcNow)
                    {
                        reward.CanRedeem = false;
                        reward.DisabledReason = "Esta recompensa ha expirado";
                    }

                    Rewards.Add(reward);
                }

                FilterRewards();
            }, "Cargando recompensas...");
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterRewards();
        }

        partial void OnSelectedCategoryChanged(string value)
        {
            FilterRewards();
        }

        private void FilterRewards()
        {
            FilteredRewards.Clear();

            var filtered = Rewards.AsEnumerable();

            // Filtrar por búsqueda
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = filtered.Where(r =>
                    r.Name?.ToLower().Contains(searchLower) == true ||
                    r.Description?.ToLower().Contains(searchLower) == true);
            }

            // Filtrar por categoría
            if (SelectedCategory != "Todas")
            {
                filtered = filtered.Where(r =>
                    r.Category?.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase) == true);
            }

            foreach (var reward in filtered)
            {
                FilteredRewards.Add(reward);
            }
        }

        [RelayCommand]
        private async Task RedeemReward(Reward reward)
        {
            if (CurrentUser == null || reward == null)
                return;

            if (!reward.CanRedeem)
            {
                await Shell.Current.DisplayAlert(
                    "No disponible",
                    reward.DisabledReason ?? "No puedes canjear esta recompensa en este momento.",
                    "OK");
                return;
            }

            var confirm = await Shell.Current.DisplayAlert(
                "Confirmar Canje",
                $"¿Canjear '{reward.Name}' por {reward.PointsCost} puntos?",
                "Canjear",
                "Cancelar");

            if (!confirm)
                return;

            await ExecuteAsync(async () =>
            {
                var success = await _firestoreService.RedeemRewardAsync(
                    CurrentUser.Uid!,
                    reward.Id!,
                    reward.PointsCost);

                if (success)
                {
                    await Shell.Current.DisplayAlert(
                        "¡Éxito!",
                        $"Has canjeado '{reward.Name}'. Un administrador procesará tu solicitud pronto.",
                        "OK");

                    // Actualizar usuario y recargar recompensas
                    CurrentUser = await _firestoreService.GetUserAsync(CurrentUser.Uid!);
                    await LoadRewards();
                }
                else
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "No se pudo completar el canje. Verifica que tienes suficientes puntos.",
                        "OK");
                }
            }, "Canjeando recompensa...");
        }

        [RelayCommand]
        private async Task ViewRewardDetail(Reward reward)
        {
            if (reward == null)
                return;

            var parameters = new Dictionary<string, object>
            {
                { "Reward", reward }
            };

            await Shell.Current.GoToAsync(nameof(RewardDetailPage), parameters);
        }

        [RelayCommand]
        private async Task RefreshRewards()
        {
            await LoadRewards();
        }
    }
}
