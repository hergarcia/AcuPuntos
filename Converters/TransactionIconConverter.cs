using System.Globalization;
using AcuPuntos.Models;

namespace AcuPuntos.Converters
{
    public class TransactionIconConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is TransactionType type)
            {
                return type switch
                {
                    TransactionType.Received => "📩",
                    TransactionType.Reward => "🎁",
                    TransactionType.Sent => "📤",
                    TransactionType.Redemption => "🎯",
                    _ => "📝"
                };
            }
            return "📝";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
