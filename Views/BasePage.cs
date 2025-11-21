using AcuPuntos.ViewModels;
using System;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace AcuPuntos.Views;

public class BasePage : ContentPage
{
    private static DateTime _lastBackPress = DateTime.MinValue;
    private static readonly TimeSpan _exitTimeThreshold = TimeSpan.FromSeconds(2);

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is BaseViewModel viewModel)
        {
            // Ejecutar el comando AppearingCommand del ViewModel
            if (viewModel.AppearingCommand?.CanExecute(null) == true)
            {
                await viewModel.AppearingCommand.ExecuteAsync(null);
            }
        }
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();

        if (BindingContext is BaseViewModel viewModel)
        {
            // Ejecutar el comando DisappearingCommand del ViewModel
            if (viewModel.DisappearingCommand?.CanExecute(null) == true)
            {
                await viewModel.DisappearingCommand.ExecuteAsync(null);
            }
        }
    }

    protected override bool OnBackButtonPressed()
    {
        // Verificar si hay stack de navegación
        var navigationStack = Shell.Current.Navigation.NavigationStack;

        // Si hay más de una página en el stack, navegar hacia atrás
        if (navigationStack.Count > 1)
        {
            Shell.Current.Navigation.PopAsync();
            return true;
        }

        // Verificar si estamos en una página que no está en el TabBar principal
        var currentRoute = Shell.Current.CurrentState.Location.ToString();

        if (!currentRoute.Contains("///main"))
        {
            // Si no estamos en el main, navegar al home
            Shell.Current.GoToAsync("///main");
            return true;
        }

        // Si estamos en el TabBar principal, implementar patrón "presiona dos veces para salir"
        var currentTime = DateTime.Now;
        var timeSinceLastBack = currentTime - _lastBackPress;

        if (timeSinceLastBack > _exitTimeThreshold)
        {
            // Primera presión: mostrar mensaje
            _lastBackPress = currentTime;
            ShowExitMessage();
            return true; // Prevenir el cierre
        }
        else
        {
            // Segunda presión dentro del tiempo límite: permitir cerrar la app
            return false;
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
