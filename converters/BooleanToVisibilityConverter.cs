using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace AMS.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var b = value is bool v && v;
            if (Invert) b = !b;
            return b;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}