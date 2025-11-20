using AcuPuntos.ViewModels;

namespace AcuPuntos.Views;

public partial class ProfilePage : BasePage
{
    public ProfilePage(ProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
