using System.Text;

namespace AcuPuntos.Services
{
    /// <summary>
    /// Implementación del servicio de navegación
    /// </summary>
    public class NavigationService : INavigationService
    {
        /// <summary>
        /// Navega a una ruta específica
        /// </summary>
        public async Task NavigateToAsync(string route)
        {
            if (string.IsNullOrWhiteSpace(route))
                throw new ArgumentException("La ruta no puede estar vacía", nameof(route));

            await Shell.Current.GoToAsync(route);
        }

        /// <summary>
        /// Navega a una ruta específica con parámetros
        /// </summary>
        public async Task NavigateToAsync(string route, IDictionary<string, object> parameters)
        {
            if (string.IsNullOrWhiteSpace(route))
                throw new ArgumentException("La ruta no puede estar vacía", nameof(route));

            if (parameters == null || !parameters.Any())
            {
                await NavigateToAsync(route);
                return;
            }

            var queryString = BuildQueryString(parameters);
            var fullRoute = $"{route}?{queryString}";

            await Shell.Current.GoToAsync(fullRoute, parameters);
        }

        /// <summary>
        /// Navega a una ruta específica con un solo parámetro
        /// </summary>
        public async Task NavigateToAsync(string route, string parameterName, object value)
        {
            if (string.IsNullOrWhiteSpace(route))
                throw new ArgumentException("La ruta no puede estar vacía", nameof(route));

            if (string.IsNullOrWhiteSpace(parameterName))
                throw new ArgumentException("El nombre del parámetro no puede estar vacío", nameof(parameterName));

            var parameters = new Dictionary<string, object>
            {
                { parameterName, value }
            };

            await NavigateToAsync(route, parameters);
        }

        /// <summary>
        /// Navega a una ruta como modal (fuera del TabBar)
        /// Utiliza Shell.PresentationMode para presentar la página fuera del contexto del TabBar
        /// </summary>
        public async Task PushModalAsync(string route, bool animated = true)
        {
            if (string.IsNullOrWhiteSpace(route))
                throw new ArgumentException("La ruta no puede estar vacía", nameof(route));

            // Usa el modo de presentación modal para navegar fuera del TabBar
            var parameters = new Dictionary<string, object>
            {
                { "PresentationMode", PresentationMode.ModalAnimated }
            };

            await Shell.Current.GoToAsync(route, animated, parameters);
        }

        /// <summary>
        /// Navega hacia atrás en el stack de navegación
        /// </summary>
        public async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        /// <summary>
        /// Navega hacia atrás con parámetros
        /// </summary>
        public async Task GoBackAsync(IDictionary<string, object> parameters)
        {
            if (parameters == null || !parameters.Any())
            {
                await GoBackAsync();
                return;
            }

            var queryString = BuildQueryString(parameters);
            await Shell.Current.GoToAsync($"..?{queryString}", parameters);
        }

        /// <summary>
        /// Navega a la raíz de la aplicación (login o main)
        /// </summary>
        public async Task NavigateToRootAsync(string root)
        {
            if (string.IsNullOrWhiteSpace(root))
                throw new ArgumentException("La raíz no puede estar vacía", nameof(root));

            await Shell.Current.GoToAsync($"//{root}");
        }

        /// <summary>
        /// Cierra el modal actual y regresa a la página anterior
        /// </summary>
        public async Task PopModalAsync(bool animated = true)
        {
            await Shell.Current.Navigation.PopModalAsync(animated);
        }

        /// <summary>
        /// Navega a la pestaña especificada en el TabBar
        /// </summary>
        public async Task NavigateToTabAsync(string tabRoute)
        {
            if (string.IsNullOrWhiteSpace(tabRoute))
                throw new ArgumentException("La ruta de la pestaña no puede estar vacía", nameof(tabRoute));

            await Shell.Current.GoToAsync(tabRoute);
        }

        /// <summary>
        /// Construye una cadena de consulta a partir de un diccionario de parámetros
        /// </summary>
        private string BuildQueryString(IDictionary<string, object> parameters)
        {
            var stringBuilder = new StringBuilder();
            var isFirst = true;

            foreach (var param in parameters)
            {
                if (param.Key == "PresentationMode")
                    continue; // No incluir PresentationMode en la query string

                if (!isFirst)
                    stringBuilder.Append('&');

                stringBuilder.Append(Uri.EscapeDataString(param.Key));
                stringBuilder.Append('=');
                stringBuilder.Append(Uri.EscapeDataString(param.Value?.ToString() ?? string.Empty));

                isFirst = false;
            }

            return stringBuilder.ToString();
        }
    }
}
