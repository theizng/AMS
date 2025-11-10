using System;
using System.Globalization;
using System.Linq;
using Microsoft.Maui.Controls;
using AMS.Models;
using System.Collections.Generic;

namespace AMS.Converters
{
    public class TenantsNamesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable<ContractTenant> tenants)
            {
                var names = tenants.Select(t => t.Name).Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                if (names.Count == 0) return "(Không có người thuê)";
                var joined = string.Join(", ", names);
                return joined.Length > 80 ? joined.Substring(0, 77) + "..." : joined;
            }
            return "(Không có)";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}