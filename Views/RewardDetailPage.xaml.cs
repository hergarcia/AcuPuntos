using AcuPuntos.ViewModels;

namespace AcuPuntos.Views;

public partial class RewardDetailPage : BasePage
{
    public RewardDetailPage(RewardDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
