using AcuPuntos.ViewModels;

namespace AcuPuntos.Views;

public partial class TransferPage : ContentPage
{
    public TransferPage(TransferViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
