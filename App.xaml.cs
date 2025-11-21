using System.Globalization;
using AcuPuntos.Services;

namespace AcuPuntos;

public partial class App : Application
{
    private readonly IAuthService _authService;

    public App(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService;
        MainPage = new AppShell();
    }

    protected override async void OnStart()
    {
        base.OnStart();
        // Navegar a splash que verificará la autenticación
        await Shell.Current.GoToAsync("//splash");
    }

    protected override void OnSleep()
    {
        base.OnSleep();
    }

    protected override async void OnResume()
    {
        base.OnResume();
        // Al reanudar, verificar autenticación nuevamente
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
            await Shell.Current.GoToAsync("//login");
        }
    }
}

// Converter para invertir bool
public class InvertedBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return !(bool)value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return !(bool)value;
    }
}