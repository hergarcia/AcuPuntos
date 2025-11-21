using AcuPuntos.ViewModels;

namespace AcuPuntos.Views;

public partial class AdminPage : BasePage
{
    public AdminPage(AdminViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
