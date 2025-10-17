using AMS.Services;

namespace AMS.Views;

public partial class SettingsPage : ContentPage
{
    private readonly IThemeService _theme;

    // DI constructor
    public SettingsPage(IThemeService theme)
    {
        InitializeComponent();
        _theme = theme;

        // Reflect current setting into radio buttons
        switch (_theme.Current)
        {
            case ThemeOption.System: RbSystem.IsChecked = true; break;
            case ThemeOption.Light: RbLight.IsChecked = true; break;
            case ThemeOption.Dark: RbDark.IsChecked = true; break;
        }

        // Handle changes
        RbSystem.CheckedChanged += OnThemeRadioChanged;
        RbLight.CheckedChanged += OnThemeRadioChanged;
        RbDark.CheckedChanged += OnThemeRadioChanged;
    }

    private void OnThemeRadioChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (!e.Value) return; // only act on checked=true

        if (sender == RbSystem) _theme.Apply(ThemeOption.System);
        else if (sender == RbLight) _theme.Apply(ThemeOption.Light);
        else if (sender == RbDark) _theme.Apply(ThemeOption.Dark);
    }

    private void OnToggleClicked(object? sender, EventArgs e) => _theme.ToggleLightDark();
}