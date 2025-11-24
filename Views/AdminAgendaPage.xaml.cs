using AcuPuntos.ViewModels;

namespace AcuPuntos.Views;

public partial class AdminAgendaPage : BasePage
{
    public AdminAgendaPage(AdminAgendaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
