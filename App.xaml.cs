using AMS.Data;
using AMS.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AMS
{
    public partial class App : Application
    {
        private readonly IAuthService _authService;

        public static IServiceProvider Services { get; private set; }

        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            Services = serviceProvider;
            _authService = serviceProvider.GetRequiredService<IAuthService>();

            // Khởi tạo database
            InitializeDatabaseAsync();

            // Xác định trang khởi đầu dựa trên trạng thái đăng nhập
            if (_authService.IsLoggedIn())
            {
                MainPage = new AppShell(_authService);
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
                await DatabaseInitializer.InitializeAsync(Services);
            }
            catch (Exception ex)
            {
                // Xử lý lỗi khởi tạo database
                Console.WriteLine($"Database initialization error: {ex.Message}");
            }
        }
    }
}