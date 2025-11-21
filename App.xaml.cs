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

    protected override void OnStart()
    {
        base.OnStart();
        CheckAuthentication();
    }

    protected override void OnSleep()
    {
        base.OnSleep();
    }

    protected override void OnResume()
    {
        base.OnResume();
        CheckAuthentication();
    }

    private async void CheckAuthentication()
    {
        try
        {
            // Pequeño delay para asegurar que el Shell esté completamente inicializado
            await Task.Delay(100);

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