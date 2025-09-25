namespace AMS
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        // Menu button tap handlers
        private async void OnManageRoomsTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Thông báo", "Chuyển đến trang Quản lý Phòng", "OK");
        }

        private async void OnManageTenantsTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Thông báo", "Chuyển đến trang Người thuê", "OK");
        }

        private async void OnManageContractsTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Thông báo", "Chuyển đến trang Hợp đồng", "OK");
        }

        private async void OnManagePaymentsTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Thông báo", "Chuyển đến trang Thanh toán", "OK");
        }

        private async void OnReportsTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Thông báo", "Chuyển đến trang Báo cáo", "OK");
        }

        private async void OnSettingsTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Thông báo", "Chuyển đến trang Cài đặt", "OK");
        }

        // Quick action handlers
        private async void OnAddRoomTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Thông báo", "Thêm phòng mới", "OK");
        }

        private async void OnCreateContractTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Thông báo", "Tạo hợp đồng mới", "OK");
        }

        private async void OnRecordPaymentTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Thông báo", "Ghi nhận thanh toán", "OK");
        }

        private async void OnExportReportTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Thông báo", "Xuất báo cáo", "OK");
        }
    }
}