using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace AcuPuntos.Views.Dialogs
{
    public partial class TimePickerDialog : ContentPage
    {
        private readonly TaskCompletionSource<TimeSpan?> _tcs;
        private readonly TimePicker _timePicker;

        public TimePickerDialog(TimeSpan initialTime)
        {
            _tcs = new TaskCompletionSource<TimeSpan?>();

            BackgroundColor = Colors.Transparent;

            var overlay = new BoxView
            {
                BackgroundColor = Colors.Black,
                Opacity = 0.5
            };

            _timePicker = new TimePicker
            {
                Time = initialTime,
                Format = "HH:mm",
                FontSize = 20,
                TextColor = Application.Current.RequestedTheme == AppTheme.Dark ? Colors.White : Color.FromArgb("#0F172A"),
                HorizontalOptions = LayoutOptions.Fill
            };

            var saveButton = new Button
            {
                Text = "Guardar",
                BackgroundColor = Color.FromArgb("#4F46E5"),
                TextColor = Colors.White,
                CornerRadius = 8,
                Margin = new Thickness(0, 10, 5, 0)
            };
            saveButton.Clicked += (s, e) =>
            {
                _tcs.SetResult(_timePicker.Time);
                Navigation.PopModalAsync();
            };

            var cancelButton = new Button
            {
                Text = "Cancelar",
                BackgroundColor = Color.FromArgb("#EF4444"),
                TextColor = Colors.White,
                CornerRadius = 8,
                Margin = new Thickness(5, 10, 0, 0)
            };
            cancelButton.Clicked += (s, e) =>
            {
                _tcs.SetResult(null);
                Navigation.PopModalAsync();
            };

            var buttonsGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                }
            };
            buttonsGrid.Add(saveButton, 0, 0);
            buttonsGrid.Add(cancelButton, 1, 0);

            var dialogContent = new VerticalStackLayout
            {
                Spacing = 15,
                Children =
                {
                    new Label
                    {
                        Text = "🕐 Editar Hora del Turno",
                        FontSize = 18,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Application.Current.RequestedTheme == AppTheme.Dark ? Colors.White : Color.FromArgb("#0F172A")
                    },
                    _timePicker,
                    buttonsGrid
                }
            };

            var frame = new Border
            {
                BackgroundColor = Application.Current.RequestedTheme == AppTheme.Dark ? Color.FromArgb("#1E293B") : Colors.White,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = 20,
                WidthRequest = 320,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Content = dialogContent
            };

            Content = new Grid
            {
                Children = { overlay, frame }
            };
        }

        public Task<TimeSpan?> GetResultAsync()
        {
            return _tcs.Task;
        }
    }
}
