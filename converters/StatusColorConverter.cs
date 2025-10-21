using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using AMS.Models;

namespace AMS.Converters
{
    public class StatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Room.Status status)  // FIXED: Direct enum cast (value is Room.Status enum)
            {
                return status switch
                {
                    Room.Status.Available => Colors.Green,
                    Room.Status.Occupied => Colors.Red,
                    Room.Status.Maintaining => Colors.Orange,
                    Room.Status.Inactive => Colors.Gray,  // Explicit for completeness
                    _ => Colors.Gray
                };
            }

            // Fallback (if non-enum somehow)
            System.Diagnostics.Debug.WriteLine($"[StatusColor] Unexpected value: {value} (type: {value?.GetType()})");  // Temp debug
            return Colors.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}