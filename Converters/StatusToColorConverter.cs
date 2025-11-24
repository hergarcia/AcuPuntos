using System;
using System.Globalization;
using AcuPuntos.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace AcuPuntos.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AppointmentStatus status || (value is string s && Enum.TryParse(s, out status)))
            {
                // If parameter is "Text", return text color (usually white or dark gray)
                // Otherwise return background color
                bool isText = parameter as string == "Text";

                return status switch
                {
                    AppointmentStatus.Available => isText ? Colors.Black : Colors.LightGray, // Or a specific available color
                    AppointmentStatus.PendingApproval => isText ? Colors.Black : Colors.Gold, // Warning
                    AppointmentStatus.Confirmed => isText ? Colors.White : Colors.Green, // Success
                    AppointmentStatus.Cancelled => isText ? Colors.White : Colors.Red, // Danger
                    AppointmentStatus.ModificationRequested => isText ? Colors.Black : Colors.Orange,
                    _ => isText ? Colors.Black : Colors.LightGray
                };
            }
            
            // Fallback for StatusString binding if it's a raw string not matching enum
            if (value is string statusString)
            {
                 bool isText = parameter as string == "Text";
                 return statusString switch {
                     "Available" => isText ? Colors.Black : Colors.LightGray,
                     "PendingApproval" => isText ? Colors.Black : Colors.Gold,
                     "Confirmed" => isText ? Colors.White : Colors.Green,
                     "Cancelled" => isText ? Colors.White : Colors.Red,
                     "ModificationRequested" => isText ? Colors.Black : Colors.Orange,
                     _ => isText ? Colors.Black : Colors.LightGray
                 };
            }

            return Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
