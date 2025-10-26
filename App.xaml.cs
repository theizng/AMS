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

            try
            {
                System.Diagnostics.Debug.WriteLine("🔵 Bắt đầu khởi tạo database (migrate)...");
                DatabaseInitializer.Initialize(Services); // sync migrate
                System.Diagnostics.Debug.WriteLine("✅ Khởi tạo database thành công.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Không thể khởi tạo database: {ex.Message}");
            }
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
    }
}