using System.Globalization;
using Microsoft.Maui.Graphics;
using AcuPuntos.Models;

namespace AcuPuntos.Converters
{
    public class TransactionColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is TransactionType type)
            {
                return type switch
                {
                    TransactionType.Received => Color.FromArgb("#2ECC71"),
                    TransactionType.Reward => Color.FromArgb("#3498DB"),
                    TransactionType.Sent => Color.FromArgb("#E74C3C"),
                    TransactionType.Redemption => Color.FromArgb("#F39C12"),
                    _ => Color.FromArgb("#95A5A6")
                };
            }
            return Color.FromArgb("#95A5A6");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
