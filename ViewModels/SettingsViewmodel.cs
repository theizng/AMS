// SettingsViewModel.cs (updated to match your existing ThemeService)
using AMS.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace AMS.ViewModels
{
    public partial class SettingsViewModel : ObservableObject, INotifyPropertyChanged
    {
        private readonly IThemeService _themeService;
        private readonly IDatabaseSyncService _syncService;

        [ObservableProperty]
        private bool isSystemTheme;

        [ObservableProperty]
        private bool isLightTheme;

        [ObservableProperty]
        private bool isDarkTheme;

        [ObservableProperty]
        private bool isBusy;

        public ICommand ToggleThemeCommand { get; }
        public ICommand BackupCommand { get; }
        public ICommand RestoreCommand { get; }

        public SettingsViewModel(IThemeService themeService, IDatabaseSyncService syncService)
        {
            _themeService = themeService;
            _syncService = syncService;

            // Load current theme from service
            UpdateThemeProperties();

            ToggleThemeCommand = new RelayCommand(ToggleTheme);
            BackupCommand = new AsyncRelayCommand(BackupAsync);
            RestoreCommand = new AsyncRelayCommand(RestoreAsync);
        }

        private void UpdateThemeProperties()
        {
            IsSystemTheme = _themeService.Current == ThemeOption.System;
            IsLightTheme = _themeService.Current == ThemeOption.Light;
            IsDarkTheme = _themeService.Current == ThemeOption.Dark;
        }

        private void ToggleTheme()
        {
            _themeService.ToggleLightDark();
            UpdateThemeProperties();
        }

        private async Task BackupAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                await _syncService.UploadDatabaseAsync("ams.db");
                await Application.Current.MainPage.DisplayAlert("Thành công", "Đã sao lưu cơ sở dữ liệu lên đám mây.", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Backup error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Lỗi", "Không thể sao lưu cơ sở dữ liệu.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RestoreAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                await _syncService.DownloadDatabaseAsync("ams.db");
                await Application.Current.MainPage.DisplayAlert("Thành công", "Đã khôi phục cơ sở dữ liệu từ đám mây.", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Restore error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Lỗi", "Không thể khôi phục cơ sở dữ liệu.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Handle radio button changes
        partial void OnIsSystemThemeChanged(bool value)
        {
            if (value)
            {
                _themeService.Apply(ThemeOption.System);
                UpdateThemeProperties();
            }
        }

        partial void OnIsLightThemeChanged(bool value)
        {
            if (value)
            {
                _themeService.Apply(ThemeOption.Light);
                UpdateThemeProperties();
            }
        }

        partial void OnIsDarkThemeChanged(bool value)
        {
            if (value)
            {
                _themeService.Apply(ThemeOption.Dark);
                UpdateThemeProperties();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}