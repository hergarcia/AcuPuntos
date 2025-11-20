using System.Threading.Tasks;
using AcuPuntos.Models;

namespace AcuPuntos.Services
{
    public interface IAuthService
    {
        User? CurrentUser { get; }
        bool IsAuthenticated { get; }
        bool IsAdmin { get; }
        
        Task<User?> SignInWithGoogleAsync();
        Task SignOutAsync();
        Task<User?> GetCurrentUserAsync();
        Task UpdateUserTokenAsync(string fcmToken);
        event EventHandler<User?>? AuthStateChanged;
    }
}
