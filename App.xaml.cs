using AMS.Services;
using AMS.ViewModels;
using AMS.Views;

namespace AMS
{
    public partial class App : Application
    {
        private readonly IAuthService _authService;

        // Inject thêm LoginViewModel qua constructor
        public App(IAuthService authService, LoginViewModel loginViewModel)
        {
            InitializeComponent();
            _authService = authService;

            // Kiểm tra trạng thái đăng nhập để quyết định trang khởi đầu
            if (_authService.IsLoggedIn())
            {
                MainPage = new AppShell();
            }
            else
            {
                // Dùng loginViewModel đã được inject
                MainPage = new LoginShell();
            }
        }
    }
}