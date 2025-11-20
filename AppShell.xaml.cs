using AcuPuntos.Views;

namespace AcuPuntos;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        
        // Registrar rutas para navegación
        Routing.RegisterRoute(nameof(HomePage), typeof(HomePage));
        Routing.RegisterRoute(nameof(TransferPage), typeof(TransferPage));
        Routing.RegisterRoute(nameof(RewardsPage), typeof(RewardsPage));
        Routing.RegisterRoute(nameof(ProfilePage), typeof(ProfilePage));
        Routing.RegisterRoute(nameof(AdminPage), typeof(AdminPage));
        Routing.RegisterRoute(nameof(HistoryPage), typeof(HistoryPage));
        Routing.RegisterRoute(nameof(RewardDetailPage), typeof(RewardDetailPage));
        Routing.RegisterRoute(nameof(UserDetailPage), typeof(UserDetailPage));
    }
}