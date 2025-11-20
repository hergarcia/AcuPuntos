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
}
