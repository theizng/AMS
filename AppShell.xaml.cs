using AMS.Services;
using AMS.ViewModels;
using AMS.Views;
using Microsoft.Extensions.DependencyInjection;

namespace AMS
{
    public partial class AppShell : Shell
    {
        private readonly IAuthService _authService;

        public AppShell(IAuthService authService, IServiceProvider service)
        {
            InitializeComponent();

            _authService = authService;

            // Wire ShellContent items to DI-created pages
            SetDiTemplate(DashboardShell, typeof(MainPage));
            SetDiTemplate(HousesShell, typeof(HousesPage));
            SetDiTemplate(TenantsShell, typeof(TenantsPage));
            SetDiTemplate(PaymentsShell, typeof(PaymentsPage));
            SetDiTemplate(ReportsShell, typeof(ReportsPage));
            SetDiTemplate(SettingsShell, typeof(SettingsPage));
            SetDiTemplate(MaintenancesShell, typeof(MaintenancesPage));
            SetDiTemplate(PaymentsShell, typeof(PaymentsPage));
            // Register routes for pages not in Shell tree (or for parameterized navigation)
            //Trang quản lý nhà
            Routing.RegisterRoute("edithouse", typeof(EditHousePage));
            Routing.RegisterRoute("rooms", typeof(RoomsPage));
            //Trang quản lý phòng
            Routing.RegisterRoute("editroom", typeof(EditRoomPage));
            Routing.RegisterRoute("detailroom", typeof(RoomDetailPage));
            //Trang người thuê nhà
            Routing.RegisterRoute("tenants", typeof(TenantsPage));
            Routing.RegisterRoute("edittenant", typeof(EditTenantPage));
            //Trang quản lý bảo trì
            Routing.RegisterRoute("maintenances", typeof(MaintenancesPage));
            Routing.RegisterRoute("editmaintenance", typeof(EditMaintenancePage));
            Navigated += OnShellNavigated;



        }
        private async void OnShellNavigated(object sender, ShellNavigatedEventArgs e)
        {
            var currentLocation = Shell.Current.CurrentState.Location.ToString().ToLowerInvariant();
            System.Diagnostics.Debug.WriteLine($"[APPSHELL] Đã điều hướng đến trang: {currentLocation}");
            //Làm mới lại trang để hiển thị các thông tin sau khi điều hướng tới/về
            if(currentLocation.Contains("houses"))
            {
                if(Shell.Current.CurrentPage?.BindingContext is HousesViewModel vm)
                {
                    System.Diagnostics.Debug.WriteLine("[APPSHELL] Đang tải lại trang House");
                    await vm.LoadHousesAsync();  
                }
            }
            //TODO: thêm các trang khác dưới này nếu cần.
        }
        private static void SetDiTemplate(ShellContent shellContent, Type pageType)
        {
            // Factory that resolves the page via DI so constructor injection works
            shellContent.ContentTemplate = new DataTemplate(() =>
                App.Services.GetRequiredService(pageType));
        }

        private async void OnLogOutClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlertAsync("Đăng xuất", "Bạn có chắc muốn đăng xuất?", "Đăng xuất", "Hủy");
            if (!confirm) return;

            try
            {
                await _authService.LogoutAsync();
                var loginShell = App.Services.GetRequiredService<LoginShell>();
                App.SetRootPage(loginShell);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logout error: {ex.Message}");
                await DisplayAlertAsync("Lỗi", "Không thể đăng xuất", "OK");
            }
        }
    }
}