using AcuPuntos.ViewModels;

namespace AcuPuntos.Views;

public partial class TransferPage : BasePage
{
    public TransferPage(TransferViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
