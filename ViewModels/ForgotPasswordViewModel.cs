using AMS.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AMS.ViewModels
{
    public partial class ForgotPasswordViewModel : ObservableObject
    {
        private readonly IAuthService _auth;
        private readonly IEmailNotificationService _notify;

        [ObservableProperty] private string email = "";
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? statusMessage;
        [ObservableProperty] private bool success;

        public IAsyncRelayCommand ResetCommand { get; }
        public ICommand BackCommand { get; }

        public ForgotPasswordViewModel(IAuthService auth, IEmailNotificationService notify)
        {
            _auth = auth;
            _notify = notify;
            ResetCommand = new AsyncRelayCommand(ResetAsync, () => !IsBusy);
            BackCommand = new Command(async () => await NavigateBackAsync());
        }

        private async Task NavigateBackAsync()
        {
            
            await Shell.Current.GoToAsync("///LoginPage");
        }

        private async Task ResetAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusMessage = null;
            Success = false;

            try
            {
                var addr = (Email ?? "").Trim();
                if (string.IsNullOrWhiteSpace(addr))
                {
                    StatusMessage = "Vui lòng nhập email.";
                    return;
                }

                var result = await _auth.ForgotPasswordAsync(addr);
                if (!result.Success)
                {
                    StatusMessage = result.ErrorMessage ?? "Không thể đặt lại.";
                    return; 
                }

                var adminName = _auth.CurrentAdmin?.FullName ?? "Quản trị";
                await _notify.SendPasswordResetAsync(addr, adminName, result.TempPassword!);

                StatusMessage = "Đã gửi mật khẩu tạm thời. Kiểm tra email của bạn.";
                Success = true;
            }
            catch (System.Exception ex)
            {
                StatusMessage = $"Lỗi: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                (ResetCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                (BackCommand as Command)?.ChangeCanExecute();
            }
        }
    }
}