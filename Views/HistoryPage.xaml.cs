using AcuPuntos.ViewModels;

namespace AcuPuntos.Views;

public partial class HistoryPage : BasePage
{
    public HistoryPage(HistoryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
