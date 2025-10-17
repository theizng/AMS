namespace AMS.Services
{
    public enum ThemeOption
    {
        System,
        Light,
        Dark
    }

    public interface IThemeService
    {
        ThemeOption Current { get; }
        void Apply(ThemeOption option);
        void ToggleLightDark(); // convenience toggle between Light/Dark
    }
}