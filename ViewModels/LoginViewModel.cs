using AMS.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AMS.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly IAuthService _authService;

        private string _username;
        private string _password;
        private bool _isBusy;
        private string _errorMessage;

        // Properties
        public string Username
        {
            get => _username;
            set
            {
                if (_username != value)
                {
                    _username = value?.Trim();
                    OnPropertyChanged();
                    (LoginCommand as Command)?.ChangeCanExecute();
                    (ForgotPasswordNavigateCommand as Command)?.ChangeCanExecute();
                }
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (_password != value)
                {
                    _password = value;
                    OnPropertyChanged();
                    (LoginCommand as Command)?.ChangeCanExecute();
                    (ForgotPasswordNavigateCommand as Command)?.ChangeCanExecute();
                }
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged();
                    (LoginCommand as Command)?.ChangeCanExecute();
                    (ForgotPasswordNavigateCommand as Command)?.ChangeCanExecute();
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        // Commands
        public ICommand LoginCommand { get; }
        public ICommand ForgotPasswordNavigateCommand { get; }

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService;

            LoginCommand = new Command(async () => await ExecuteLoginCommand(), CanLogin);
            ForgotPasswordNavigateCommand = new Command(async () => await ExecuteForgotPasswordNavigate(), CanNavigateForgot);
        }

        private bool CanLogin()
        {
            return !IsBusy &&
                   !string.IsNullOrWhiteSpace(Username) &&
                   !string.IsNullOrWhiteSpace(Password);
        }

        private bool CanNavigateForgot()
        {
            // We only need to prevent spamming while busy
            return !IsBusy;
        }

        private async Task ExecuteLoginCommand()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                var result = await _authService.LoginAsync(Username, Password);

                if (result.Success)
                {
                    // Navigate to main shell (DI)
                    var appShell = App.Services.GetRequiredService<AppShell>();
                    App.SetRootPage(appShell);

                    // Clear sensitive data
                    Password = string.Empty;
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "Đăng nhập thất bại.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi đăng nhập: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteForgotPasswordNavigate()
        {
            if (IsBusy)
                return;

            try
            {
                // Route must be registered in AppShell: <ShellContent Route="forgotpassword" ... />
                await Shell.Current.GoToAsync("///forgotpassword");
            }
            catch (Exception ex)
            {
                // Fallback message if route not found
                await Shell.Current.DisplayAlertAsync("Lỗi", $"Không thể mở trang 'Quên mật khẩu': {ex.Message}", "OK");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}