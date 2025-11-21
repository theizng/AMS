using System;
using System.Globalization;
using AMS.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace AMS.Converters
{
    public class PaymentStatusToBadgeColorConverter : IValueConverter
    {
        // Map domain status to badge background colors
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not PaymentStatus status) return Colors.Gray;

            return status switch
            {
                PaymentStatus.Paid => Color.FromArgb("#2E7D32"),            // green
                PaymentStatus.PartiallyPaid => Color.FromArgb("#F9A825"),   // amber
                PaymentStatus.Late => Color.FromArgb("#D92C54"),            // red
                PaymentStatus.MissingData => Color.FromArgb("#3E0703"),     // gray
                PaymentStatus.ReadyToSend => Color.FromArgb("#1976D2"),     // blue
                PaymentStatus.SentFirst => Color.FromArgb("#1565C0"),       // deeper blue
                PaymentStatus.Closed => Color.FromArgb("#455A64"),          // blue gray
                _ => Color.FromArgb("#9E9E9E")
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}