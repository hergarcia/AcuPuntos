using CommunityToolkit.Mvvm.ComponentModel;
using AcuPuntos.Services;

namespace AcuPuntos.ViewModels
{
    // RewardsViewModel
    public partial class RewardsViewModel : BaseViewModel
    {
        public RewardsViewModel(IAuthService authService, IFirestoreService firestoreService)
        {
            Title = "Recompensas";
        }
    }

    // ProfileViewModel
    public partial class ProfileViewModel : BaseViewModel
    {
        public ProfileViewModel(IAuthService authService, IFirestoreService firestoreService)
        {
            Title = "Perfil";
        }
    }

    // AdminViewModel
    public partial class AdminViewModel : BaseViewModel
    {
        public AdminViewModel(IAuthService authService, IFirestoreService firestoreService)
        {
            Title = "Administración";
        }
    }

    // HistoryViewModel
    public partial class HistoryViewModel : BaseViewModel
    {
        public HistoryViewModel(IAuthService authService, IFirestoreService firestoreService)
        {
            Title = "Historial";
        }
    }

    // RewardDetailViewModel
    public partial class RewardDetailViewModel : BaseViewModel
    {
        public RewardDetailViewModel(IAuthService authService, IFirestoreService firestoreService)
        {
            Title = "Detalle de Recompensa";
        }
    }

    // UserDetailViewModel  
    public partial class UserDetailViewModel : BaseViewModel
    {
        public UserDetailViewModel(IAuthService authService, IFirestoreService firestoreService)
        {
            Title = "Detalle de Usuario";
        }
    }
}
