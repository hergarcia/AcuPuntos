using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AcuPuntos.Services;
using System.Threading.Tasks;

namespace AcuPuntos.ViewModels
{
    public partial class LoginViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;

        [ObservableProperty]
        private string welcomeMessage = "¡Bienvenido a AcuPuntos!";

        [ObservableProperty]
        private string subtitleMessage = "Tu sistema de puntos de acupuntura";

        [ObservableProperty]
        private bool isLoading;

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService;
            Title = "AcuPuntos";
        }

        [RelayCommand]
        private async Task SignInWithGoogleAsync()
        {
            await ExecuteAsync(async () =>
            {
                var user = await _authService.SignInWithGoogleAsync();
                
                if (user != null)
                {
                    // Navegar a la página principal
                    await Shell.Current.GoToAsync("//main");
                }
                else
                {
                    await Shell.Current.DisplayAlert(
                        "Error de inicio de sesión",
                        "No se pudo iniciar sesión con Google. Por favor, intenta de nuevo.",
                        "OK");
                }
            }, "Iniciando sesión...");
        }

        protected override async Task OnAppearingAsync()
        {
            await base.OnAppearingAsync();
            
            // Si ya está autenticado, ir directo a la página principal
            if (_authService.IsAuthenticated)
            {
                await Shell.Current.GoToAsync("//main");
            }
        }
    }
}
