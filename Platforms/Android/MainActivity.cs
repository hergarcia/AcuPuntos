using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Plugin.Firebase.Auth.Google;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using AndroidX.Activity;

namespace AcuPuntos;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    private static DateTime _lastBackPress = DateTime.MinValue;
    private static readonly TimeSpan _exitTimeThreshold = TimeSpan.FromSeconds(2);

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Configurar el callback para el botón de atrás (Android 13+)
        OnBackPressedDispatcher.AddCallback(this, new BackPressedCallback(async () =>
        {
            await HandleBackPressed();
        }));
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        FirebaseAuthGoogleImplementation.HandleActivityResultAsync(requestCode, resultCode, data);
        base.OnActivityResult(requestCode, resultCode, data);
    }

    private async Task HandleBackPressed()
    {
        try
        {

            // Obtener el Shell actual
            if (Shell.Current == null)
            {
                Finish();
                return;
            }

            // Verificar si hay stack de navegación
            var navigationStack = Shell.Current.Navigation.NavigationStack;

            // Si hay más de una página en el stack, navegar hacia atrás
            if (navigationStack.Count > 1)
            {
                await Shell.Current.Navigation.PopAsync();
                return;
            }

            // Verificar si estamos en una página que no está en el TabBar principal
            var currentRoute = Shell.Current.CurrentState.Location.ToString();

            if (!currentRoute.Contains("//main"))
            {
                // Si no estamos en el main, navegar al home
                await Shell.Current.GoToAsync("//main");
                return;
            }

            // Si estamos en el TabBar principal, implementar patrón "presiona dos veces para salir"
            var currentTime = DateTime.Now;
            var timeSinceLastBack = currentTime - _lastBackPress;

            if (timeSinceLastBack > _exitTimeThreshold)
            {
                // Primera presión: mostrar mensaje
                _lastBackPress = currentTime;
                await ShowExitMessage();
            }
            else
            {
                // Segunda presión dentro del tiempo límite: cerrar la app
                Finish();
            }
    
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }}

    private static async Task ShowExitMessage()
    {
        // Mostrar toast usando CommunityToolkit.Maui
        var toast = Toast.Make(
            "Presiona nuevamente para salir",
            ToastDuration.Short,
            14);

        await MainThread.InvokeOnMainThreadAsync(async () => await toast.Show());
    }
}

// Callback personalizado para manejar el botón de atrás
public class BackPressedCallback : OnBackPressedCallback
{
    private readonly Func<Task> _handleBackPressed;

    public BackPressedCallback(Func<Task> handleBackPressed) : base(true)
    {
        _handleBackPressed = handleBackPressed;
    }

    public override void HandleOnBackPressed()
    {
        // Ejecutar la lógica de forma asíncrona
        Task.Run(async () => await _handleBackPressed());
    }
}