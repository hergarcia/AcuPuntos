using AcuPuntos.ViewModels;

namespace AcuPuntos.Views;

public partial class RewardsPage : BasePage
{
    public RewardsPage(RewardsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
