using AMS.Services;
using AMS.Views;
using Microsoft.Extensions.DependencyInjection;

namespace AMS
{
    public partial class AppShell : Shell
    {
        private readonly IAuthService _authService;

        public AppShell(IAuthService authService)
        {
            InitializeComponent();
            _authService = authService;

            // Use DI to resolve pages for ShellContent so constructors can get VMs from DI
            // App.Services is set in App’s constructor
            HousesShell.ContentTemplate = new DataTemplate(() => App.Services.GetRequiredService<HousesPage>());

            // Register navigation routes
            Routing.RegisterRoute("edithouse", typeof(EditHousePage));
            Routing.RegisterRoute("rooms", typeof(RoomsPage));
        }

        private async void OnLogOutClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert(
                "Đăng xuất",
                "Bạn có chắc muốn đăng xuất?",
                "Đăng xuất",
                "Hủy"
            );

            if (!confirm) return;

            try
            {
                await _authService.LogoutAsync();
                Application.Current.MainPage = new LoginShell();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logout error: {ex.Message}");
                await DisplayAlert("Lỗi", "Không thể đăng xuất", "OK");
            }
        }
    }
}