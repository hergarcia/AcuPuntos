using AcuPuntos.ViewModels;

namespace AcuPuntos.Views;

public partial class RewardDetailPage : ContentPage
{
    public RewardDetailPage(RewardDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
