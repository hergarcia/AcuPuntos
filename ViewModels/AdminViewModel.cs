using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AcuPuntos.Models;
using AcuPuntos.Services;
using AcuPuntos.Views;
using AcuPuntos.Helpers;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace AcuPuntos.ViewModels
{
    public partial class AdminViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly IFirestoreService _firestoreService;
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        private ObservableCollection<User> users;

        [ObservableProperty]
        private ObservableCollection<Redemption> pendingRedemptions;

        [ObservableProperty]
        private string searchText = "";

        [ObservableProperty]
        private int totalUsers;

        [ObservableProperty]
        private int pendingRedemptionsCount;

        public AdminViewModel(IAuthService authService, IFirestoreService firestoreService, INavigationService navigationService)
        {
            _authService = authService;
            _firestoreService = firestoreService;
            _navigationService = navigationService;
            Title = "Administración";
            Users = new ObservableCollection<User>();
            PendingRedemptions = new ObservableCollection<Redemption>();
        }

        protected override async Task OnAppearingAsync()
        {
            await base.OnAppearingAsync();
            await LoadData();
        }

        private async Task LoadData()
        {
            await ExecuteAsync(async () =>
            {
                // Cargar usuarios
                var allUsers = await _firestoreService.GetAllUsersAsync();
                Users.Clear();
                foreach (var user in allUsers)
                {
                    Users.Add(user);
                }
                TotalUsers = Users.Count;

                // Cargar canjes pendientes
                var redemptions = await _firestoreService.GetPendingRedemptionsAsync();
                PendingRedemptions.Clear();
                foreach (var redemption in redemptions)
                {
                    PendingRedemptions.Add(redemption);
                }
                PendingRedemptionsCount = PendingRedemptions.Count;

            }, "Cargando datos...");
        }

        [RelayCommand]
        private async Task ViewUserDetail(User user)
        {
            if (user == null)
                return;

            var parameters = new Dictionary<string, object>
            {
                { "User", user }
            };

            await _navigationService.NavigateToAsync(nameof(UserDetailPage), parameters);
        }

        [RelayCommand]
        private async Task AssignPoints(User user)
        {
            if (user == null)
                return;

            var pointsStr = await Shell.Current.DisplayPromptAsync(
                "Asignar Puntos",
                $"¿Cuántos puntos deseas asignar a {user.DisplayName}?",
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
                var success = await _firestoreService.AssignPointsToUserAsync(user.Uid!, points, description);

                if (success)
                {
                    await Shell.Current.DisplayAlert("¡Éxito!", $"Se han asignado {points} puntos a {user.DisplayName}", "OK");
                    await LoadData();
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error", "No se pudieron asignar los puntos", "OK");
                }
            }, "Asignando puntos...");
        }

        [RelayCommand]
        private async Task ApproveRedemption(Redemption redemption)
        {
            if (redemption == null)
                return;

            var confirm = await Shell.Current.DisplayAlert(
                "Aprobar Canje",
                $"¿Aprobar el canje de {redemption.User?.DisplayName}?",
                "Aprobar",
                "Cancelar");

            if (!confirm)
                return;

            await ExecuteAsync(async () =>
            {
                await _firestoreService.UpdateRedemptionStatusAsync(
                    redemption.Id!,
                    RedemptionStatus.Completed);

                await Shell.Current.DisplayAlert("¡Aprobado!", "El canje ha sido aprobado", "OK");
                await LoadData();
            }, "Aprobando canje...");
        }

        [RelayCommand]
        private async Task RejectRedemption(Redemption redemption)
        {
            if (redemption == null)
                return;

            var confirm = await Shell.Current.DisplayAlert(
                "Rechazar Canje",
                $"¿Rechazar el canje de {redemption.User?.DisplayName}? Los puntos serán devueltos.",
                "Rechazar",
                "Cancelar");

            if (!confirm)
                return;

            await ExecuteAsync(async () =>
            {
                await _firestoreService.UpdateRedemptionStatusAsync(
                    redemption.Id!,
                    RedemptionStatus.Cancelled);

                await Shell.Current.DisplayAlert("Rechazado", "El canje ha sido rechazado y los puntos devueltos", "OK");
                await LoadData();
            }, "Rechazando canje...");
        }

        [RelayCommand]
        private async Task InitializeBadges()
        {
            var confirm = await Shell.Current.DisplayAlert(
                "Inicializar Badges",
                "¿Deseas crear los 16 badges predefinidos del sistema?\n\nEsto solo debe hacerse UNA VEZ. Si los badges ya existen, se ignorarán.",
                "Sí, crear",
                "Cancelar");

            if (!confirm)
                return;

            await ExecuteAsync(async () =>
            {
                try
                {
                    await BadgeSeeder.SeedBadgesAsync(_firestoreService);

                    await Shell.Current.DisplayAlert(
                        "¡Éxito!",
                        "Los badges han sido creados correctamente en Firestore.\n\nLos usuarios recibirán badges automáticamente según sus logros.",
                        "OK");
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        $"No se pudieron crear los badges:\n{ex.Message}\n\nAsegúrate de que las reglas de Firestore permitan escribir en la colección 'badges'.",
                        "OK");
                }
            }, "Creando badges...");
        }

        [RelayCommand]
        private async Task InitializeRewards()
        {
            await ExecuteAsync(async () =>
            {
                try
                {
                    await RewardSeeder.SeedRewardsAsync(_firestoreService);

                    await Shell.Current.DisplayAlert(
                        "¡Éxito!",
                        "Las recompensas han sido creadas correctamente en Firestore.\n\nLos usuarios podrán canjear estas recompensas con sus puntos.",
                        "OK");
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        $"No se pudieron crear las recompensas:\n{ex.Message}\n\nAsegúrate de que las reglas de Firestore permitan escribir en la colección 'rewards'.",
                        "OK");
                }
            }, "Creando recompensas...");
        }

        [RelayCommand]
        private async Task RefreshData()
        {
            await LoadData();
        }
    }
}
