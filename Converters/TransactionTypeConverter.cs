using System.Globalization;
using AcuPuntos.Models;

namespace AcuPuntos.Converters
{
    public class TransactionTypeConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is TransactionType type)
            {
                return type switch
                {
                    TransactionType.Received => "Recibido",
                    TransactionType.Reward => "Recompensa",
                    TransactionType.Sent => "Enviado",
                    TransactionType.Redemption => "Canje",
                    _ => "Desconocido"
                };
            }
            return "Desconocido";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
