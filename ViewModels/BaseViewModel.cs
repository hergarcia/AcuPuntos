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

        protected virtual async Task OnAppearingAsync()
        {
            await Task.CompletedTask;
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
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
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
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
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
            return await Shell.Current.DisplayAlert(title, message, acceptButton, cancelButton);
        }

        /// <summary>
        /// Muestra un diálogo de información.
        /// </summary>
        protected async Task ShowAlertAsync(string title, string message, string button = "OK")
        {
            await Shell.Current.DisplayAlert(title, message, button);
        }

        /// <summary>
        /// Muestra un mensaje de error.
        /// </summary>
        protected async Task ShowErrorAsync(string message, string title = "Error")
        {
            await Shell.Current.DisplayAlert(title, message, "OK");
        }

        /// <summary>
        /// Muestra un mensaje de éxito.
        /// </summary>
        protected async Task ShowSuccessAsync(string message, string title = "¡Éxito!")
        {
            await Shell.Current.DisplayAlert(title, message, "OK");
        }
    }
}
