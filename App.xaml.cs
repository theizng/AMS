using AMS.Data;
using AMS.Services;

namespace AMS
{
    public partial class App : Application
    {
        private readonly IAuthService _authService;

        public App(IAuthService authService)
        {
            InitializeComponent();
            _authService = authService;

            // Khởi tạo database
            InitializeDatabaseAsync();

            // Xác định trang khởi đầu dựa trên trạng thái đăng nhập
            if (_authService.IsLoggedIn())
            {
                MainPage = new AppShell();
            }
            else
            {
                MainPage = new LoginShell();
            }
        }

        private async void InitializeDatabaseAsync()
        {
            try
            {
                await DatabaseInitializer.InitializeAsync(IPlatformApplication.Current.Services);
            }
            catch (Exception ex)
            {
                // Xử lý lỗi khởi tạo database
                Console.WriteLine($"Database initialization error: {ex.Message}");
            }
        }
    }
}