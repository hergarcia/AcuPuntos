using AcuPuntos.ViewModels;

namespace AcuPuntos.Views;

public class BasePage : ContentPage
{
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
        // Verificar si estamos en una página que no está en el TabBar
        // Si es así, navegar de vuelta al home en lugar de cerrar la app
        var currentRoute = Shell.Current.CurrentState.Location.ToString();

        // Si estamos en una ruta que no es parte del TabBar principal (main/*)
        // navegar de vuelta al home
        if (!currentRoute.Contains("///main"))
        {
            Shell.Current.GoToAsync("///main");
            return true; // Indica que manejamos el evento
        }

        // Para las páginas del TabBar, permitir el comportamiento por defecto
        return base.OnBackButtonPressed();
    }
}
