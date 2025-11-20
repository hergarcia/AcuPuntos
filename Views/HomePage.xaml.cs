using AcuPuntos.ViewModels;

namespace AcuPuntos.Views;

public partial class HomePage : BasePage
{
    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
