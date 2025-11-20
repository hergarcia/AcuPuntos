using System;
using System.Threading.Tasks;
using AcuPuntos.Models;
using Plugin.Firebase.Auth;
using User = AcuPuntos.Models.User;

namespace AcuPuntos.Services
{
    public class AuthService : IAuthService
    {
        private readonly IFirestoreService _firestoreService;
        private User? _currentUser;

        public User? CurrentUser => _currentUser;
        public bool IsAuthenticated => _currentUser != null;
        public bool IsAdmin => _currentUser?.IsAdmin ?? false;

        public event EventHandler<User?>? AuthStateChanged;

        public AuthService(IFirestoreService firestoreService)
        {
            _firestoreService = firestoreService;
        }

        public async Task<User?> SignInWithGoogleAsync()
        {
            try
            {
#if ANDROID || IOS
                var auth = CrossFirebaseAuth.Current;
                var result = await auth.SignInWithGoogleAsync();

                if (result?.User != null)
                {
                    // Verificar si el usuario existe en Firestore
                    var user = await _firestoreService.GetUserAsync(result.User.Uid);

                    if (user == null)
                    {
                        // Crear nuevo usuario con puntos de bienvenida
                        user = new User
                        {
                            Uid = result.User.Uid,
                            Email = result.User.Email ?? "",
                            DisplayName = result.User.DisplayName ?? "Usuario",
                            PhotoUrl = result.User.PhotoUrl?.ToString(),
                            Role = "user",
                            Points = 100, // Puntos de bienvenida
                            CreatedAt = DateTime.UtcNow,
                            LastLogin = DateTime.UtcNow
                        };

                        await _firestoreService.CreateUserAsync(user);

                        // Crear transacción de bienvenida
                        var welcomeTransaction = new Transaction
                        {
                            Type = TransactionType.Earned,
                            Amount = 100,
                            FromUserId = "system",
                            ToUserId = user.Uid,
                            Description = "¡Bienvenido a AcuPuntos! 🎉",
                            CreatedAt = DateTime.UtcNow
                        };

                        await _firestoreService.CreateTransactionAsync(welcomeTransaction);
                    }
                    else
                    {
                        // Actualizar último login
                        user.LastLogin = DateTime.UtcNow;
                        await _firestoreService.UpdateUserAsync(user);
                    }

                    _currentUser = user;
                    AuthStateChanged?.Invoke(this, _currentUser);
                    return user;
                }

                return null;
#else
                throw new PlatformNotSupportedException("Google Sign-In solo está soportado en Android e iOS");
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en SignInWithGoogle: {ex.Message}");
                return null;
            }
        }

        public async Task SignOutAsync()
        {
            try
            {
#if ANDROID || IOS
                var auth = CrossFirebaseAuth.Current;
                await auth.SignOutAsync();
#endif
                _currentUser = null;
                AuthStateChanged?.Invoke(this, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en SignOut: {ex.Message}");
            }
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            try
            {
#if ANDROID || IOS
                var auth = CrossFirebaseAuth.Current;
                if (auth.CurrentUser != null && _currentUser == null)
                {
                    _currentUser = await _firestoreService.GetUserAsync(auth.CurrentUser.Uid);
                }
#endif
                return _currentUser;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting current user: {ex.Message}");
                return null;
            }
        }

        public async Task UpdateUserTokenAsync(string fcmToken)
        {
            if (_currentUser != null)
            {
                _currentUser.FcmToken = fcmToken;
                await _firestoreService.UpdateUserAsync(_currentUser);
            }
        }
    }
}
