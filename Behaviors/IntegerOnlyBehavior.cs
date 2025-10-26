using Microsoft.Maui.Controls;

namespace AMS.Behaviors
{
    // Allows only digits (0-9). Good for counts, VND amounts (no decimals).
    public class IntegerOnlyBehavior : Behavior<Entry>
    {
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

        private static void OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (sender is not Entry entry) return;
            if (string.IsNullOrEmpty(e.NewTextValue)) return;

            // keep only digits
            var filtered = new string(e.NewTextValue.Where(char.IsDigit).ToArray());
            if (filtered != e.NewTextValue)
                entry.Text = filtered;
        }
    }
}