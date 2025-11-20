using System;
using System.Threading.Tasks;
using AcuPuntos.Models;
using Firebase.Auth;
using Firebase.Auth.Providers;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Firestore;
using User = AcuPuntos.Models.User;

namespace AcuPuntos.Services
{
    public class AuthService : IAuthService
    {
        private readonly IFirebaseAuth _firebaseAuth;
        private readonly IFirestoreService _firestoreService;
        private User? _currentUser;
        
        public User? CurrentUser => _currentUser;
        public bool IsAuthenticated => _currentUser != null;
        public bool IsAdmin => _currentUser?.IsAdmin ?? false;
        
        public event EventHandler<User?>? AuthStateChanged;

        public AuthService(IFirestoreService firestoreService)
        {
            _firestoreService = firestoreService;
            _firebaseAuth = CrossFirebaseAuth.Current;
            
            // Escuchar cambios en el estado de autenticación
            _firebaseAuth.AuthStateChanged += OnAuthStateChanged;
        }

        private async void OnAuthStateChanged(object? sender, FirebaseAuth.AuthStateEventArgs e)
        {
            if (e.Auth.CurrentUser != null)
            {
                _currentUser = await _firestoreService.GetUserAsync(e.Auth.CurrentUser.Uid);
                if (_currentUser == null)
                {
                    // Si el usuario no existe en Firestore, crearlo
                    _currentUser = new User
                    {
                        Uid = e.Auth.CurrentUser.Uid,
                        Email = e.Auth.CurrentUser.Email,
                        DisplayName = e.Auth.CurrentUser.DisplayName ?? "Usuario",
                        PhotoUrl = e.Auth.CurrentUser.PhotoUrl?.ToString(),
                        Role = "user",
                        Points = 100 // Puntos de bienvenida
                    };
                    await _firestoreService.CreateUserAsync(_currentUser);
                }
            }
            else
            {
                _currentUser = null;
            }
            
            AuthStateChanged?.Invoke(this, _currentUser);
        }

        public async Task<User?> SignInWithGoogleAsync()
        {
            try
            {
#if ANDROID
                var result = await _firebaseAuth.SignInWithGoogleAsync();
#elif IOS
                // Para iOS, necesitamos configuración adicional
                var result = await _firebaseAuth.SignInWithGoogleAsync();
#else
                throw new PlatformNotSupportedException("Google Sign-In no está soportado en esta plataforma");
#endif

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
                            Email = result.User.Email,
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
                    return user;
                }
                
                return null;
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
                await _firebaseAuth.SignOutAsync();
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
            if (_firebaseAuth.CurrentUser != null && _currentUser == null)
            {
                _currentUser = await _firestoreService.GetUserAsync(_firebaseAuth.CurrentUser.Uid);
            }
            return _currentUser;
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
