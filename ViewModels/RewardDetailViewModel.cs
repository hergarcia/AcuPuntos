using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AcuPuntos.Models;
using AcuPuntos.Services;
using System.Threading.Tasks;

namespace AcuPuntos.ViewModels
{
    [QueryProperty(nameof(Reward), "Reward")]
    public partial class RewardDetailViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly IFirestoreService _firestoreService;

        [ObservableProperty]
        private Reward? reward;

        [ObservableProperty]
        private User? currentUser;

        [ObservableProperty]
        private bool canRedeem;

        [ObservableProperty]
        private string statusMessage = "";

        public RewardDetailViewModel(IAuthService authService, IFirestoreService firestoreService)
        {
            _authService = authService;
            _firestoreService = firestoreService;
            Title = "Detalle de Recompensa";
        }

        protected override async Task OnAppearingAsync()
        {
            await base.OnAppearingAsync();
            CurrentUser = _authService.CurrentUser;
            CheckRedeemability();
        }

        partial void OnRewardChanged(Reward? value)
        {
            if (value != null)
            {
                Title = value.Name ?? "Detalle de Recompensa";
                CheckRedeemability();
            }
        }

        private void CheckRedeemability()
        {
            if (Reward == null || CurrentUser == null)
            {
                CanRedeem = false;
                StatusMessage = "Cargando...";
                return;
            }

            if (!Reward.IsActive)
            {
                CanRedeem = false;
                StatusMessage = "Esta recompensa ya no está disponible";
                return;
            }

            if (Reward.ExpiryDate.HasValue && Reward.ExpiryDate.Value < DateTime.UtcNow)
            {
                CanRedeem = false;
                StatusMessage = "Esta recompensa ha expirado";
                return;
            }

            if (CurrentUser.Points < Reward.PointsCost)
            {
                CanRedeem = false;
                var pointsNeeded = Reward.PointsCost - CurrentUser.Points;
                StatusMessage = $"Te faltan {pointsNeeded:N0} puntos";
                return;
            }

            CanRedeem = true;
            StatusMessage = "¡Puedes canjear esta recompensa!";
        }

        [RelayCommand]
        private async Task RedeemReward()
        {
            if (CurrentUser == null || Reward == null || !CanRedeem)
                return;

            var confirm = await Shell.Current.DisplayAlert(
                "Confirmar Canje",
                $"¿Estás seguro de canjear '{Reward.Name}' por {Reward.PointsCost:N0} puntos?\n\nUn administrador procesará tu solicitud pronto.",
                "Canjear",
                "Cancelar");

            if (!confirm)
                return;

            await ExecuteAsync(async () =>
            {
                var success = await _firestoreService.RedeemRewardAsync(
                    CurrentUser.Uid!,
                    Reward.Id!,
                    Reward.PointsCost);

                if (success)
                {
                    await Shell.Current.DisplayAlert(
                        "¡Éxito!",
                        $"Has canjeado '{Reward.Name}' exitosamente.",
                        "OK");

                    // Actualizar usuario
                    CurrentUser = await _firestoreService.GetUserAsync(CurrentUser.Uid!);
                    CheckRedeemability();

                    // Volver atrás
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await Shell.Current.DisplayAlert(
                        "Error",
                        "No se pudo completar el canje. Por favor intenta de nuevo.",
                        "OK");
                }
            }, "Canjeando recompensa...");
        }
    }
}
