using System.Globalization;
using Microsoft.Maui.Graphics;

namespace AcuPuntos.Converters
{
    public class SelectedColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isSelected && isSelected)
                return Color.FromArgb("#2ECC71");
            
            return Color.FromArgb("#E0E0E0");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
