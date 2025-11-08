using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using AMS.Models;

namespace AMS.Converters
{
    public class ContractStatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ContractStatus st)
            {
                return st switch
                {
                    ContractStatus.Draft => Color.FromArgb("#FFF3CD"),
                    ContractStatus.Active => Color.FromArgb("#C8E6C9"),
                    ContractStatus.Expired => Color.FromArgb("#FFE0B2"),
                    ContractStatus.Terminated => Color.FromArgb("#FFCDD2"),
                    _ => Color.FromArgb("#EEEEEE")
                };
            }
            return Color.FromArgb("#EEEEEE");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}