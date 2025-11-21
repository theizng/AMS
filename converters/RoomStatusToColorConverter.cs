using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace AMS.Converters
{
    public enum RoomStatus
    {
        Empty,
        Occupied,
        Reserved
    }

    public class RoomStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not RoomStatus st) return Colors.Gray;
            return st switch
            {
                RoomStatus.Empty => Colors.Gray,
                RoomStatus.Occupied => Colors.Green,
                RoomStatus.Reserved => Colors.Orange,
                _ => Colors.Gray
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}