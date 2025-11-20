using AcuPuntos.ViewModels;

namespace AcuPuntos.Views;

public partial class UserDetailPage : ContentPage
{
    public UserDetailPage(UserDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
