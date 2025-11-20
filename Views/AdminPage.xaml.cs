using AcuPuntos.ViewModels;

namespace AcuPuntos.Views;

public partial class AdminPage : ContentPage
{
    public AdminPage(AdminViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
