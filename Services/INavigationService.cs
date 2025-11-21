namespace AcuPuntos.Services
{
    /// <summary>
    /// Servicio de navegación centralizado para gestionar la navegación entre páginas
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// Navega a una ruta específica
        /// </summary>
        /// <param name="route">Nombre de la ruta</param>
        Task NavigateToAsync(string route);

        /// <summary>
        /// Navega a una ruta específica con parámetros
        /// </summary>
        /// <param name="route">Nombre de la ruta</param>
        /// <param name="parameters">Diccionario de parámetros</param>
        Task NavigateToAsync(string route, IDictionary<string, object> parameters);

        /// <summary>
        /// Navega a una ruta específica con un solo parámetro
        /// </summary>
        /// <param name="route">Nombre de la ruta</param>
        /// <param name="parameterName">Nombre del parámetro</param>
        /// <param name="value">Valor del parámetro</param>
        Task NavigateToAsync(string route, string parameterName, object value);

        /// <summary>
        /// Navega a una ruta como modal (fuera del TabBar)
        /// </summary>
        /// <param name="route">Nombre de la ruta</param>
        /// <param name="animated">Si la navegación debe ser animada</param>
        Task PushModalAsync(string route, bool animated = true);

        /// <summary>
        /// Navega hacia atrás en el stack de navegación
        /// </summary>
        Task GoBackAsync();

        /// <summary>
        /// Navega hacia atrás con parámetros
        /// </summary>
        /// <param name="parameters">Diccionario de parámetros</param>
        Task GoBackAsync(IDictionary<string, object> parameters);

        /// <summary>
        /// Navega a la raíz de la aplicación (login o main)
        /// </summary>
        /// <param name="root">Nombre de la raíz (login o main)</param>
        Task NavigateToRootAsync(string root);

        /// <summary>
        /// Cierra el modal actual y regresa a la página anterior
        /// </summary>
        Task PopModalAsync(bool animated = true);

        /// <summary>
        /// Navega a la pestaña especificada en el TabBar
        /// </summary>
        /// <param name="tabRoute">Ruta de la pestaña (ej: "//main/HomePage")</param>
        Task NavigateToTabAsync(string tabRoute);
    }
}
