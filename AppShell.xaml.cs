using AMS.Services;
using AMS.Views;

namespace AMS
{
    public partial class AppShell : Shell
    {
        private readonly IAuthService _authService;
        public AppShell(IAuthService authService)
        {
            InitializeComponent();
            _authService = authService;
            // Register navigation routes
            Routing.RegisterRoute("rooms", typeof(RoomsPage));
            //Routing.RegisterRoute("edithouse", typeof(EditHousePage));
            //Routing.RegisterRoute("editroom", typeof(EditRoomPage));
        }
        private async void OnLogOutClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert(
                "Đăng xuất",
                "Bạn có chắc muốn đăng xuất?",
                "Đăng xuất",
                "Hủy"
            );

            if (confirm)
            {
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
}
