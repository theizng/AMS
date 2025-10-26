using Microsoft.Maui.Controls;
using System.Globalization;
using System.Linq;

namespace AMS.Behaviors
{
    // Allows digits and a single decimal separator (based on current culture).
    public class DecimalNumberBehavior : Behavior<Entry>
    {
        private readonly string _sep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

        protected override void OnAttachedTo(Entry entry)
        {
            entry.TextChanged += OnTextChanged;
            base.OnAttachedTo(entry);
        }

        protected override void OnDetachingFrom(Entry entry)
        {
            entry.TextChanged -= OnTextChanged;
            base.OnDetachingFrom(entry);
        }

        private void OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (sender is not Entry entry) return;
            if (string.IsNullOrEmpty(e.NewTextValue)) return;

            var s = e.NewTextValue;

            // Keep digits and decimal separator
            var filtered = new string(s.Where(c => char.IsDigit(c) || _sep.Contains(c)).ToArray());

            // Ensure at most one separator
            int first = filtered.IndexOf(_sep, StringComparison.Ordinal);
            if (first >= 0)
            {
                int next = filtered.IndexOf(_sep, first + _sep.Length, StringComparison.Ordinal);
                if (next >= 0)
                    filtered = filtered.Remove(next, _sep.Length);
            }

            if (filtered != s)
                entry.Text = filtered;
        }
    }
}