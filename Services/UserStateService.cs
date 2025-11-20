using AcuPuntos.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading.Tasks;

namespace AcuPuntos.Services
{
    /// <summary>
    /// Servicio centralizado para gestionar el estado del usuario actual.
    /// Proporciona una única fuente de verdad para los datos del usuario,
    /// reduciendo duplicación y sincronización entre ViewModels.
    /// </summary>
    public partial class UserStateService : ObservableObject
    {
        private readonly IAuthService _authService;
        private readonly IFirestoreService _firestoreService;

        [ObservableProperty]
        private User? currentUser;

        [ObservableProperty]
        private bool isLoading;

        public UserStateService(IAuthService authService, IFirestoreService firestoreService)
        {
            _authService = authService;
            _firestoreService = firestoreService;
        }

        /// <summary>
        /// Inicializa el estado del usuario desde el servicio de autenticación.
        /// Debe ser llamado al inicio de la aplicación o después del login.
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                CurrentUser = _authService.CurrentUser;

                // Si hay usuario autenticado, cargar datos frescos de Firestore
                if (CurrentUser != null && !string.IsNullOrEmpty(CurrentUser.Uid))
                {
                    await RefreshUserAsync();
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Refresca los datos del usuario actual desde Firestore.
        /// </summary>
        public async Task<bool> RefreshUserAsync()
        {
            if (CurrentUser == null || string.IsNullOrEmpty(CurrentUser.Uid))
                return false;

            try
            {
                IsLoading = true;
                var freshUser = await _firestoreService.GetUserAsync(CurrentUser.Uid);

                if (freshUser != null)
                {
                    CurrentUser = freshUser;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing user: {ex.Message}");
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Actualiza los puntos del usuario actual localmente.
        /// Útil para actualizaciones inmediatas en la UI sin esperar Firestore.
        /// </summary>
        public void UpdatePoints(int newPoints)
        {
            if (CurrentUser != null)
            {
                CurrentUser.Points = newPoints;
                // Forzar notificación de cambio
                OnPropertyChanged(nameof(CurrentUser));
            }
        }

        /// <summary>
        /// Limpia el estado del usuario (útil al cerrar sesión).
        /// </summary>
        public void ClearUser()
        {
            CurrentUser = null;
        }

        /// <summary>
        /// Verifica si el usuario actual es administrador.
        /// </summary>
        public bool IsAdmin => CurrentUser?.Role == UserRole.Admin;

        /// <summary>
        /// Verifica si hay un usuario autenticado.
        /// </summary>
        public bool IsAuthenticated => CurrentUser != null && !string.IsNullOrEmpty(CurrentUser.Uid);
    }
}
