using AcuPuntos.Services;

namespace AcuPuntos.Views;

public partial class SplashPage : ContentPage
{
    private readonly IAuthService _authService;

    public SplashPage(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Verificar autenticación
        await CheckAuthenticationAsync();
    }

    private async Task CheckAuthenticationAsync()
    {
        try
        {
            var currentUser = await _authService.GetCurrentUserAsync();

            if (currentUser != null)
            {
                // Usuario autenticado - ir a la pantalla principal
                await Shell.Current.GoToAsync("//main");
            }
            else
            {
                // Sin autenticación - ir a login
                await Shell.Current.GoToAsync("//login");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking authentication: {ex.Message}");
            // En caso de error, ir a login
            await Shell.Current.GoToAsync("//login");
        }
    }
}
