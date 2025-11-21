using AMS.Services.Interfaces;
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
            //Trang dashboard
            SetDiTemplate(DashboardShell, typeof(MainPage));
            //Trang Quản lý
            SetDiTemplate(MaintenancesShell, typeof(MaintenancesPage));
            SetDiTemplate(HousesShell, typeof(HousesPage));
            SetDiTemplate(TenantsShell, typeof(TenantsPage));
            SetDiTemplate(ContractsShell, typeof(ContractsPage));
            //Trang Tài chính
            SetDiTemplate(Payment_OverviewShell, typeof(PaymentsPage));
            SetDiTemplate(Payment_FeesShell, typeof(PaymentFeesPage));
            SetDiTemplate(Payment_MeterEntryShell, typeof(PaymentMeterEntryPage));
            SetDiTemplate(Payment_InvoicesShell, typeof(PaymentInvoicesPage));
            SetDiTemplate(Payment_SettingsShell, typeof(PaymentSettingsPage));
            //Trang Cài đặt
            SetDiTemplate(SettingsShell, typeof(SettingsPage));
            //Trang báo cáo thống kê
            SetDiTemplate(ReportOverviewShell, typeof(ReportsPage));
            SetDiTemplate(RevenueShell, typeof(ReportRevenuePage));
            SetDiTemplate(UtilitiesShell, typeof(ReportUtilitiesPage));
            SetDiTemplate(ProfitShell, typeof(ReportProfitsPage));
            SetDiTemplate(RoomStatusShell, typeof(ReportRoomStatusPage));
            SetDiTemplate(DebtShell, typeof(ReportDebtPage));
            //SetDiTemplate(ReportsShell, typeof(ReportsPage));
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
            //Trang quản lý hợp đồng
            Routing.RegisterRoute("contracts", typeof(ContractsPage));
            Routing.RegisterRoute("editcontract", typeof(EditContractPage));
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