using System.Globalization;
using Microsoft.Maui.Graphics;

namespace AcuPuntos.Converters
{
    public class UserSelectionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] = Uid del usuario actual (del DataTemplate)
            // values[1] = Uid del SelectedUser (del ViewModel)

            if (values.Length >= 2 &&
                values[0] is string currentUid &&
                values[1] is string selectedUid &&
                !string.IsNullOrEmpty(currentUid) &&
                !string.IsNullOrEmpty(selectedUid) &&
                currentUid == selectedUid)
            {
                return Color.FromArgb("#2ECC71"); // Color verde para seleccionado
            }

            return Color.FromArgb("#E0E0E0"); // Color gris para no seleccionado
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
