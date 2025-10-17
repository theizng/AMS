using AMS.Data;
using AMS.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AMS
{
    public partial class App : Application
    {
        private readonly IAuthService _authService;

        public static IServiceProvider Services { get; private set; }

        public App(IServiceProvider serviceProvider, IAuthService authService)
        {
            InitializeComponent();
            Services = serviceProvider;
            _authService = authService;

            // Khởi tạo database
            InitializeDatabaseAsync();
        }
        protected override Window CreateWindow(IActivationState activationState)
        {
            Page root = _authService.IsLoggedIn()
                ? Services.GetRequiredService<AppShell>()
                : Services.GetRequiredService<LoginShell>();

            return new Window
            {
                Page = root
            };
        }
        public static void SetRootPage(Page page)
        {
            var window = Current?.Windows.FirstOrDefault();
            if (window != null)
            {
                window.Page = page;
            }
        }
        private static async void InitializeDatabaseAsync()
        {
            try
            {
                await DatabaseInitializer.InitializeAsync(Services);
            }
            catch (Exception ex)
            {
                // Xử lý lỗi khởi tạo database
                Console.WriteLine($"Không thể khởi tạo database, check App.xaml.cs: {ex.Message}");
            }
        }
        
    }
}