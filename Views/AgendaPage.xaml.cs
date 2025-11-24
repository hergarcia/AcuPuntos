using AcuPuntos.ViewModels;

namespace AcuPuntos.Views;

public partial class AgendaPage : BasePage
{
    public AgendaPage(AgendaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
