using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Plugin.Firebase.Auth.Google;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace AcuPuntos;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    private static DateTime _lastBackPress = DateTime.MinValue;
    private static readonly TimeSpan _exitTimeThreshold = TimeSpan.FromSeconds(2);

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        FirebaseAuthGoogleImplementation.HandleActivityResultAsync(requestCode, resultCode, data);
        base.OnActivityResult(requestCode, resultCode, data);
    }

    public override void OnBackPressed()
    {
        // Obtener el Shell actual
        if (Shell.Current == null)
        {
            base.OnBackPressed();
            return;
        }

        // Verificar si hay stack de navegación
        var navigationStack = Shell.Current.Navigation.NavigationStack;

        // Si hay más de una página en el stack, navegar hacia atrás
        if (navigationStack.Count > 1)
        {
            Shell.Current.Navigation.PopAsync();
            return;
        }

        // Verificar si estamos en una página que no está en el TabBar principal
        var currentRoute = Shell.Current.CurrentState.Location.ToString();

        if (!currentRoute.Contains("///main"))
        {
            // Si no estamos en el main, navegar al home
            Shell.Current.GoToAsync("///main");
            return;
        }

        // Si estamos en el TabBar principal, implementar patrón "presiona dos veces para salir"
        var currentTime = DateTime.Now;
        var timeSinceLastBack = currentTime - _lastBackPress;

        if (timeSinceLastBack > _exitTimeThreshold)
        {
            // Primera presión: mostrar mensaje
            _lastBackPress = currentTime;
            ShowExitMessage();
        }
        else
        {
            // Segunda presión dentro del tiempo límite: cerrar la app
            base.OnBackPressed();
        }
    }

    private async void ShowExitMessage()
    {
        // Mostrar snackbar usando CommunityToolkit.Maui
        var snackbarOptions = new SnackbarOptions
        {
            BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#1F2937"),
            TextColor = Microsoft.Maui.Graphics.Colors.White,
            CornerRadius = new CornerRadius(10),
            Font = Microsoft.Maui.Font.SystemFontOfSize(14),
        };

        var snackbar = Snackbar.Make(
            "Presiona nuevamente para salir de la app",
            null,
            null,
            TimeSpan.FromSeconds(2),
            snackbarOptions);

        await snackbar.Show();
    }
}
