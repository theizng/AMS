using System.Globalization;

namespace AMS.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public Color ActiveColor { get; set; } = Colors.Green;
        public Color InactiveColor { get; set; } = Colors.Gray;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = value is bool v && v;
            return b ? ActiveColor : InactiveColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}