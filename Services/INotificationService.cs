using System.Threading.Tasks;

namespace AcuPuntos.Services
{
    public interface INotificationService
    {
        /// <summary>
        /// Muestra una notificación local al usuario
        /// </summary>
        Task ShowLocalNotificationAsync(string title, string message);

        /// <summary>
        /// Inicializa el servicio de notificaciones (permisos, etc.)
        /// </summary>
        Task InitializeAsync();
    }
}
