using AcuPuntos.ViewModels;

namespace AcuPuntos.Views;

public partial class LoginPage : BasePage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
