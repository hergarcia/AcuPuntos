using AcuPuntos.ViewModels;

namespace AcuPuntos.Views;

// RewardsPage
public partial class RewardsPage : ContentPage
{
    public RewardsPage(RewardsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

// ProfilePage
public partial class ProfilePage : ContentPage
{
    public ProfilePage(ProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

// AdminPage
public partial class AdminPage : ContentPage
{
    public AdminPage(AdminViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

// HistoryPage
public partial class HistoryPage : ContentPage
{
    public HistoryPage(HistoryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

// RewardDetailPage
public partial class RewardDetailPage : ContentPage
{
    public RewardDetailPage(RewardDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

// UserDetailPage
public partial class UserDetailPage : ContentPage
{
    public UserDetailPage(UserDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
