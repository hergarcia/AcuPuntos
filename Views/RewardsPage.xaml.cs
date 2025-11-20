using AcuPuntos.ViewModels;

namespace AcuPuntos.Views;

public partial class RewardsPage : ContentPage
{
    public RewardsPage(RewardsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
