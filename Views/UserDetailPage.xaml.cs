using AcuPuntos.ViewModels;

namespace AcuPuntos.Views;

public partial class UserDetailPage : BasePage
{
    public UserDetailPage(UserDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
