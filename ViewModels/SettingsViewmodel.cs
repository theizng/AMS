using AMS.Services;
using AMS.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AMS.ViewModels
{
    public partial class SettingsViewModel : ObservableObject, INotifyPropertyChanged
    {
        // Email setting keys
        private const string K_SmtpHost = "email:smtp:host";
        private const string K_SmtpPort = "email:smtp:port";
        private const string K_SmtpSsl = "email:smtp:ssl";
        private const string K_SmtpUser = "email:smtp:user";
        private const string K_SmtpPwd = "email:smtp:pwd";
        private const string K_SenderName = "email:sender:name";
        private const string K_SenderAddr = "email:sender:addr";

        private readonly IThemeService _themeService;
        private readonly IDatabaseSyncService _syncService;
        private readonly IEmailService _email;
        private readonly IAuthService _auth;

        [ObservableProperty] private bool isSystemTheme;
        [ObservableProperty] private bool isLightTheme;
        [ObservableProperty] private bool isDarkTheme;
        [ObservableProperty] private bool isBusy;

        // Admin profile (NEW)
        [ObservableProperty] private string? adminFullName;
        [ObservableProperty] private string? adminEmail;
        [ObservableProperty] private string? adminPhone;
        [ObservableProperty] private string? adminIdCard;

        // Email config
        [ObservableProperty] private string? smtpHost;
        [ObservableProperty] private string? smtpPort;     // string for UI binding
        [ObservableProperty] private bool smtpUseSsl = true;
        [ObservableProperty] private string? smtpUser;
        [ObservableProperty] private string? smtpPassword;
        [ObservableProperty] private string? senderName = "AMS";
        [ObservableProperty] private string? senderAddress;

        // Test email
        [ObservableProperty] private string? testEmailTo;

        // Change password
        [ObservableProperty] private string? currentPassword;
        [ObservableProperty] private string? newPassword;
        [ObservableProperty] private string? confirmPassword;

        public ICommand ToggleThemeCommand { get; }
        public ICommand BackupCommand { get; }
        public ICommand RestoreCommand { get; }
        public IAsyncRelayCommand SaveEmailSettingsCommand { get; }
        public IAsyncRelayCommand TestEmailCommand { get; }
        public IAsyncRelayCommand ChangePasswordCommand { get; }
        // NEW
        public IAsyncRelayCommand SaveAdminProfileCommand { get; }

        public SettingsViewModel(
            IThemeService themeService,
            IDatabaseSyncService syncService,
            IEmailService email,
            IAuthService auth)
        {
            _themeService = themeService;
            _syncService = syncService;
            _email = email;
            _auth = auth;

            UpdateThemeProperties();

            ToggleThemeCommand = new RelayCommand(ToggleTheme);
            BackupCommand = new AsyncRelayCommand(BackupAsync);
            RestoreCommand = new AsyncRelayCommand(RestoreAsync);

            SaveEmailSettingsCommand = new AsyncRelayCommand(SaveEmailSettingsAsync);
            TestEmailCommand = new AsyncRelayCommand(TestEmailAsync);
            ChangePasswordCommand = new AsyncRelayCommand(ChangePasswordAsync);

            // NEW
            SaveAdminProfileCommand = new AsyncRelayCommand(SaveAdminProfileAsync);

            LoadEmailSettings();
            LoadAdminProfile();
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
            if (IsBusy || Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi", "Không có kết nối internet.", "OK");
                return;
            }
            IsBusy = true;
            try
            {
                await _syncService.UploadDatabaseAsync("ams.db");
                await Shell.Current.DisplayAlertAsync("Thành công", "Đã sao lưu cơ sở dữ liệu lên đám mây.", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Backup error: {ex.Message}");
                await Shell.Current.DisplayAlertAsync("Lỗi", $"Không thể sao lưu: {ex.Message}", "OK");
            }
            finally { IsBusy = false; }
        }

        private async Task RestoreAsync()
        {
            if (IsBusy || Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi", "Không có kết nối internet.", "OK");
                return;
            }
            IsBusy = true;

            try
            {
                await _syncService.DownloadDatabaseAsync("ams.db");

                var restart = await Shell.Current.DisplayAlertAsync(
                    "Khôi phục đã tải xong",
                    "Cơ sở dữ liệu đã được tải về và sẽ được áp dụng sau khi khởi động lại ứng dụng. Khởi động lại ngay bây giờ?",
                    "Khởi động lại",
                    "Để sau");

                if (restart) AppRestarter.RestartApp();
                else await Shell.Current.DisplayAlertAsync("Thông báo", "Khôi phục sẽ áp dụng sau khi bạn khởi động lại ứng dụng.", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Restore error: {ex.Message}");
                await Shell.Current.DisplayAlertAsync("Lỗi", $"Không thể khôi phục: {ex.Message}", "OK");
            }
            finally { IsBusy = false; }
        }

        // ====== Admin profile (NEW) ======
        private void LoadAdminProfile()
        {
            var a = _auth.CurrentAdmin;
            if (a != null)
            {
                AdminFullName = a.FullName;
                AdminEmail = a.Email;
                AdminPhone = a.PhoneNumber;
                AdminIdCard = a.IdCardNumber;
            }
            else
            {
                AdminFullName = "";
                AdminEmail = "";
                AdminPhone = "";
                AdminIdCard = "";
            }
        }

        private async Task SaveAdminProfileAsync()
        {
            try
            {
                var name = (AdminFullName ?? "").Trim();
                var email = (AdminEmail ?? "").Trim();
                var phone = (AdminPhone ?? "").Trim();
                var idCard = (AdminIdCard ?? "").Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    await Shell.Current.DisplayAlertAsync("Thiếu thông tin", "Nhập Họ tên.", "OK");
                    return;
                }
                if (!string.IsNullOrEmpty(email) && !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    await Shell.Current.DisplayAlertAsync("Email không hợp lệ", "Vui lòng kiểm tra lại địa chỉ email.", "OK");
                    return;
                }

                // Save via auth service (now accepts idCard)
                await _auth.UpdateProfileAsync(name, email, phone, string.IsNullOrWhiteSpace(idCard) ? null : idCard);

                // Reload to reflect persisted data
                LoadAdminProfile();

                await Shell.Current.DisplayAlertAsync("Thành công", "Đã cập nhật thông tin quản trị.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi", ex.Message, "OK");
            }
        }

        // ====== Email settings ======
        private void LoadEmailSettings()
        {
            SmtpHost = Preferences.Get(K_SmtpHost, "");
            SmtpPort = Preferences.Get(K_SmtpPort, "587");
            SmtpUseSsl = Preferences.Get(K_SmtpSsl, true);
            SmtpUser = Preferences.Get(K_SmtpUser, "");
            SenderName = Preferences.Get(K_SenderName, "AMS");
            SenderAddress = Preferences.Get(K_SenderAddr, SmtpUser ?? "");
            _ = LoadPwdAsync();
        }

        private async Task LoadPwdAsync()
        {
            try
            {
                var pwd = await SecureStorage.GetAsync(K_SmtpPwd);
                SmtpPassword = string.IsNullOrEmpty(pwd) ? Preferences.Get(K_SmtpPwd, "") : pwd;
            }
            catch
            {
                SmtpPassword = Preferences.Get(K_SmtpPwd, "");
            }
        }

        private async Task SaveEmailSettingsAsync()
        {
            try
            {
                Preferences.Set(K_SmtpHost, SmtpHost ?? "");
                Preferences.Set(K_SmtpPort, string.IsNullOrWhiteSpace(SmtpPort) ? "587" : SmtpPort!);
                Preferences.Set(K_SmtpSsl, SmtpUseSsl);
                Preferences.Set(K_SmtpUser, SmtpUser ?? "");
                Preferences.Set(K_SenderName, string.IsNullOrWhiteSpace(SenderName) ? "AMS" : SenderName!);
                Preferences.Set(K_SenderAddr, string.IsNullOrWhiteSpace(SenderAddress) ? (SmtpUser ?? "") : SenderAddress!);

                try { await SecureStorage.SetAsync(K_SmtpPwd, SmtpPassword ?? ""); }
                catch { Preferences.Set(K_SmtpPwd, SmtpPassword ?? ""); }

                await Shell.Current.DisplayAlertAsync("Đã lưu", "Đã lưu cấu hình email.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi", $"Không thể lưu cấu hình: {ex.Message}", "OK");
            }
        }

        private async Task TestEmailAsync()
        {
            var to = (TestEmailTo ?? "").Trim();
            if (string.IsNullOrWhiteSpace(to))
            {
                await Shell.Current.DisplayAlertAsync("Thiếu email", "Nhập email người nhận để kiểm tra.", "OK");
                return;
            }

            await SaveEmailSettingsAsync();

            var host = Preferences.Get(K_SmtpHost, "");
            var portStr = Preferences.Get(K_SmtpPort, "587");
            var ssl = Preferences.Get(K_SmtpSsl, true);
            var user = Preferences.Get(K_SmtpUser, "");
            var pwd = await SecureStorage.GetAsync(K_SmtpPwd) ?? Preferences.Get(K_SmtpPwd, "");
            var senderName = Preferences.Get(K_SenderName, "AMS");
            var senderAddr = Preferences.Get(K_SenderAddr, user);

            if (!int.TryParse(portStr, out var port)) port = 587;

            try
            {
                await _email.SendAsync(
                    to: to,
                    subject: "[AMS] Email kiểm tra",
                    body: "Xin chào,\n\nĐây là email kiểm tra cấu hình SMTP từ ứng dụng AMS.\n\nNếu bạn thấy email này, cấu hình đã OK.",
                    smtpHost: host,
                    smtpPort: port,
                    smtpUser: user,
                    smtpPassword: pwd,
                    useSsl: ssl,
                    senderName: senderName,
                    senderAddress: senderAddr);

                await Shell.Current.DisplayAlertAsync("Thành công", "Đã gửi email kiểm tra.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi gửi email", ex.Message, "OK");
            }
        }

        private async Task ChangePasswordAsync()
        {
            try
            {
                var cur = CurrentPassword ?? "";
                var nw = NewPassword ?? "";
                var cf = ConfirmPassword ?? "";

                if (string.IsNullOrWhiteSpace(cur) || string.IsNullOrWhiteSpace(nw))
                {
                    await Shell.Current.DisplayAlertAsync("Thiếu thông tin", "Nhập đầy đủ mật khẩu hiện tại và mật khẩu mới.", "OK");
                    return;
                }
                if (nw.Length < 6)
                {
                    await Shell.Current.DisplayAlertAsync("Mật khẩu yếu", "Mật khẩu mới phải từ 6 ký tự.", "OK");
                    return;
                }
                if (nw != cf)
                {
                    await Shell.Current.DisplayAlertAsync("Không khớp", "Xác nhận mật khẩu không khớp.", "OK");
                    return;
                }

                await _auth.ChangePasswordAsync(cur, nw);

                CurrentPassword = "";
                NewPassword = "";
                ConfirmPassword = "";

                await Shell.Current.DisplayAlertAsync("Thành công", "Đổi mật khẩu thành công.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi", ex.Message, "OK");
            }
        }

        // Radio changes
        partial void OnIsSystemThemeChanged(bool value)
        {
            if (value) { _themeService.Apply(ThemeOption.System); UpdateThemeProperties(); }
        }
        partial void OnIsLightThemeChanged(bool value)
        {
            if (value) { _themeService.Apply(ThemeOption.Light); UpdateThemeProperties(); }
        }
        partial void OnIsDarkThemeChanged(bool value)
        {
            if (value) { _themeService.Apply(ThemeOption.Dark); UpdateThemeProperties(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}