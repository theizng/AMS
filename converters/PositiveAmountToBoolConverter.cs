using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace AMS.Converters
{
    public class PositiveAmountToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is decimal d && d > 0m;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}