using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace AcuPuntos.ViewModels
{
    public partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool isBusy;

        [ObservableProperty]
        private string? title;

        [ObservableProperty]
        private string? subtitle;

        public bool IsNotBusy => !IsBusy;

        /// <summary>
        /// Indica si el ViewModel ya ha sido inicializado con datos.
        /// Usar esto para evitar recargas innecesarias en OnAppearing.
        /// </summary>
        protected bool IsInitialized { get; set; }

        /// <summary>
        /// Método que se debe sobreescribir para cargar datos la primera vez.
        /// Solo se ejecutará una vez a menos que se llame a InvalidateData().
        /// </summary>
        protected virtual async Task InitializeAsync()
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Invalida los datos para forzar una recarga en el próximo OnAppearing.
        /// Útil después de operaciones que modifiquen datos.
        /// </summary>
        protected void InvalidateData()
        {
            IsInitialized = false;
        }

        protected virtual async Task OnAppearingAsync()
        {
            // Solo inicializar la primera vez
            if (!IsInitialized)
            {
                await InitializeAsync();
                IsInitialized = true;
            }
        }

        protected virtual async Task OnDisappearingAsync()
        {
            await Task.CompletedTask;
        }

        [RelayCommand]
        private async Task AppearingAsync()
        {
            await OnAppearingAsync();
        }

        [RelayCommand]
        private async Task DisappearingAsync()
        {
            await OnDisappearingAsync();
        }

        protected async Task ExecuteAsync(Func<Task> operation, string? loadingMessage = null)
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                
                if (!string.IsNullOrEmpty(loadingMessage))
                {
                    Subtitle = loadingMessage;
                }

                await operation();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
                await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
                Subtitle = null;
            }
        }

        protected async Task<T?> ExecuteAsync<T>(Func<Task<T>> operation, string? loadingMessage = null)
        {
            if (IsBusy)
                return default;

            try
            {
                IsBusy = true;

                if (!string.IsNullOrEmpty(loadingMessage))
                {
                    Subtitle = loadingMessage;
                }

                return await operation();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
                await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
                return default;
            }
            finally
            {
                IsBusy = false;
                Subtitle = null;
            }
        }

        /// <summary>
        /// Muestra un diálogo de confirmación con mensajes personalizados.
        /// </summary>
        protected async Task<bool> ShowConfirmationAsync(string title, string message, string acceptButton = "Sí", string cancelButton = "No")
        {
            return await Shell.Current.DisplayAlertAsync(title, message, acceptButton, cancelButton);
        }

        /// <summary>
        /// Muestra un diálogo de información.
        /// </summary>
        protected async Task ShowAlertAsync(string title, string message, string button = "OK")
        {
            await Shell.Current.DisplayAlertAsync(title, message, button);
        }

        /// <summary>
        /// Muestra un mensaje de error.
        /// </summary>
        protected async Task ShowErrorAsync(string message, string title = "Error")
        {
            await Shell.Current.DisplayAlertAsync(title, message, "OK");
        }

        /// <summary>
        /// Muestra un mensaje de éxito.
        /// </summary>
        protected async Task ShowSuccessAsync(string message, string title = "¡Éxito!")
        {
            await Shell.Current.DisplayAlertAsync(title, message, "OK");
        }
    }
}
