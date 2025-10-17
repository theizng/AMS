using AMS.Resources.Styles;

namespace AMS.Services
{
    public class ThemeService : IThemeService
    {
        private const string PrefKey = "app_theme";
        public ThemeOption Current { get; private set; } = ThemeOption.System;

        public ThemeService()
        {
            // Load saved preference
            var saved = Preferences.Default.Get(PrefKey, nameof(ThemeOption.System));
            if (Enum.TryParse<ThemeOption>(saved, out var opt))
                Current = opt;

            Apply(Current);
        }

        public void Apply(ThemeOption option)
        {
            Current = option;
            Preferences.Default.Set(PrefKey, option.ToString());

            // 1) Set the app-level theme (affects AppThemeBinding / OnAppTheme)
            Application.Current!.UserAppTheme = option switch
            {
                ThemeOption.Light => AppTheme.Light,
                ThemeOption.Dark => AppTheme.Dark,
                _ => AppTheme.Unspecified // follows system
            };

            // 2) Swap our theme dictionary
            var appResources = Application.Current!.Resources;

            // Remove any prior theme dictionaries
            var toRemove = appResources.MergedDictionaries
                .Where(d => d is LightTheme || d is DarkTheme)
                .ToList();
            foreach (var d in toRemove)
                appResources.MergedDictionaries.Remove(d);

            // Choose the dictionary to add
            ResourceDictionary themeDict = option switch
            {
                ThemeOption.Light => new LightTheme(),
                ThemeOption.Dark => new DarkTheme(),
                _ => (Application.Current!.RequestedTheme == AppTheme.Dark)
                        ? new DarkTheme()
                        : new LightTheme()
            };

            appResources.MergedDictionaries.Add(themeDict);

            // React to OS theme changes if we’re following system
            Application.Current!.RequestedThemeChanged -= OnRequestedThemeChanged;
            if (option == ThemeOption.System)
                Application.Current!.RequestedThemeChanged += OnRequestedThemeChanged;
        }

        public void ToggleLightDark()
        {
            var next = Current switch
            {
                ThemeOption.Light => ThemeOption.Dark,
                ThemeOption.Dark => ThemeOption.Light,
                _ => (Application.Current!.RequestedTheme == AppTheme.Dark) ? ThemeOption.Light : ThemeOption.Dark
            };
            Apply(next);
        }

        private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
        {
            if (Current != ThemeOption.System) return;

            // Swap dictionaries to match OS
            var appResources = Application.Current!.Resources;

            var toRemove = appResources.MergedDictionaries
                .Where(d => d is LightTheme || d is DarkTheme)
                .ToList();
            foreach (var d in toRemove)
                appResources.MergedDictionaries.Remove(d);

            if (e.RequestedTheme == AppTheme.Dark)
                appResources.MergedDictionaries.Add(new DarkTheme());
            else
                appResources.MergedDictionaries.Add(new LightTheme());
        }
    }
}