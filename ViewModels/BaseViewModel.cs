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
    }
}
