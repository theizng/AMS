using AMS;

namespace AMS
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnManageRoomsTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Thông báo", "Chuyển đến trang Quản lý Phòng", "OK");
            // await Navigation.PushAsync(new RoomListPage());
        }

        private async void OnManageTenantsTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Thông báo", "Chuyển đến trang Quản lý Người thuê", "OK");
            // await Navigation.PushAsync(new TenantListPage());
        }

        private async void OnManageContractsTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Thông báo", "Chuyển đến trang Hợp đồng", "OK");
            // await Navigation.PushAsync(new ContractListPage());
        }

        private async void OnManagePaymentsTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Thông báo", "Chuyển đến trang Thanh toán", "OK");
            // await Navigation.PushAsync(new PaymentListPage());
        }

        private async void OnReportsTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Thông báo", "Chuyển đến trang Báo cáo", "OK");
            // await Navigation.PushAsync(new ReportsPage());
        }

        private async void OnSettingsTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Thông báo", "Chuyển đến trang Cài đặt", "OK");
            // await Navigation.PushAsync(new SettingsPage());
        }
    }
}