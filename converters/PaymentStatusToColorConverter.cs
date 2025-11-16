using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using AMS.Models;

namespace AMS.Converters
{
    public class PaymentStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value?.ToString() ?? "";
            return status switch
            {
                nameof(PaymentStatus.MissingData) => Color.FromArgb("#FFF3CD"),
                nameof(PaymentStatus.ReadyToSend) => Color.FromArgb("#BBDEFB"),
                nameof(PaymentStatus.SentFirst) => Color.FromArgb("#C8E6C9"),
                nameof(PaymentStatus.PartiallyPaid) => Color.FromArgb("#FFE0B2"),
                nameof(PaymentStatus.Paid) => Color.FromArgb("#C8E6C9"),
                nameof(PaymentStatus.Late) => Color.FromArgb("#FFCDD2"),
                nameof(PaymentStatus.Closed) => Color.FromArgb("#E0E0E0"),
                _ => Color.FromArgb("#ECEFF1")
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}