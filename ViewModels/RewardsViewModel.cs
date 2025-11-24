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
        private readonly IGamificationService _gamificationService;
        private readonly INavigationService _navigationService;
        private IDisposable? _userListener;
        private IDisposable? _rewardsListener;

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

        public RewardsViewModel(IAuthService authService, IFirestoreService firestoreService, IGamificationService gamificationService, INavigationService navigationService)
        {
            _authService = authService;
            _firestoreService = firestoreService;
            _gamificationService = gamificationService;
            _navigationService = navigationService;
            Title = "Recompensas";
            Rewards = new ObservableCollection<Reward>();
            FilteredRewards = new ObservableCollection<Reward>();
        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            CurrentUser = _authService.CurrentUser;
            await LoadRewards();
            SubscribeToUpdates();
        }

        private async Task LoadRewards(bool silent = false)
        {
            Func<Task> operation = async () =>
            {
                // 1. Obtener datos (IO)
                var allRewards = await _firestoreService.GetActiveRewardsAsync();

                // Capturar valores necesarios para el procesamiento
                var userPoints = CurrentUser?.Points ?? 0;
                var isLoggedIn = CurrentUser != null;

                // 2. Procesar datos en segundo plano (CPU)
                await Task.Run(() =>
                {
                    foreach (var reward in allRewards)
                    {
                        // Verificar si el usuario puede canjear
                        reward.CanRedeem = isLoggedIn && userPoints >= reward.PointsCost;

                        if (!reward.CanRedeem && isLoggedIn)
                        {
                            var pointsNeeded = reward.PointsCost - userPoints;
                            reward.DisabledReason = $"Te faltan {pointsNeeded} puntos";
                        }

                        // Verificar si ha expirado
                        if (reward.ExpiryDate.HasValue && reward.ExpiryDate.Value < DateTime.UtcNow)
                        {
                            reward.CanRedeem = false;
                            reward.DisabledReason = "Esta recompensa ha expirado";
                        }
                    }
                });

                // 3. Actualizar UI en el hilo principal
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Rewards.Clear();
                    foreach (var reward in allRewards)
                    {
                        Rewards.Add(reward);
                    }

                    FilterRewards();
                });
            };

            if (silent)
            {
                try
                {
                    await operation();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading rewards silently: {ex.Message}");
                }
            }
            else
            {
                await ExecuteAsync(operation, "Cargando recompensas...");
            }
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
                var redemption = await _firestoreService.RedeemRewardAsync(
                    CurrentUser.Uid!,
                    reward.Id!);

                if (redemption != null)
                {
                    // Dar experiencia por el canje (10% del costo de la recompensa)
                    int xpGained = Math.Max(1, reward.PointsCost / 10);
                    await _gamificationService.AddExperienceAsync(CurrentUser.Uid!, xpGained, $"Canje de '{reward.Name}'");

                    await Shell.Current.DisplayAlert(
                        "¡Éxito!",
                        $"Has canjeado '{reward.Name}'. Un administrador procesará tu solicitud pronto.\n+{xpGained} XP ganada",
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

            await _navigationService.NavigateToAsync(nameof(RewardDetailPage), parameters);
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

            // Escuchar cambios en el usuario (puntos)
            _userListener = _firestoreService.ListenToUserChanges(CurrentUser.Uid, user =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    CurrentUser = user;
                    // Recargar recompensas para actualizar estado "CanRedeem"
                    await LoadRewards(silent: true);
                });
            });

            // Escuchar cambios en las recompensas (nuevas, expiradas, etc)
            _rewardsListener = _firestoreService.ListenToRewards(rewards =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await ProcessRewardsList(rewards);
                });
            });
        }

        private void UnsubscribeUpdates()
        {
            _userListener?.Dispose();
            _userListener = null;
            _rewardsListener?.Dispose();
            _rewardsListener = null;
        }

        private async Task ProcessRewardsList(List<Reward> allRewards)
        {
             // Capturar valores necesarios para el procesamiento
            var userPoints = CurrentUser?.Points ?? 0;
            var isLoggedIn = CurrentUser != null;

            // Procesar datos en segundo plano (CPU)
            await Task.Run(() =>
            {
                foreach (var reward in allRewards)
                {
                    // Verificar si el usuario puede canjear
                    reward.CanRedeem = isLoggedIn && userPoints >= reward.PointsCost;

                    if (!reward.CanRedeem && isLoggedIn)
                    {
                        var pointsNeeded = reward.PointsCost - userPoints;
                        reward.DisabledReason = $"Te faltan {pointsNeeded} puntos";
                    }

                    // Verificar si ha expirado
                    if (reward.ExpiryDate.HasValue && reward.ExpiryDate.Value < DateTime.UtcNow)
                    {
                        reward.CanRedeem = false;
                        reward.DisabledReason = "Esta recompensa ha expirado";
                    }
                }
            });

            // Actualizar UI en el hilo principal
            Rewards.Clear();
            foreach (var reward in allRewards)
            {
                Rewards.Add(reward);
            }

            FilterRewards();
        }

        [RelayCommand]
        private async Task RefreshRewards()
        {
            await LoadRewards();
        }
    }
}
