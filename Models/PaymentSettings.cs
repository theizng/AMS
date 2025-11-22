namespace AMS.Models
{
    public class PaymentSettings
    {
        // Banking info để điền vào ThongBaoPhi sheet template.
        public string NameAccount { get; set; } = ""; // Tên chủ tài khoản
        public string BankAccount { get; set; } = ""; // Số tài khoản
        public string BankName { get; set; } = ""; // Tên ngân hàng
        public string Branch { get; set; } = ""; // Chi nhánh ngân hàng

        // Billing cycle settings
        public int DefaultDueDay { get; set; } = 5;   // Ngày thu tiền (1..28 recommended)
        public int GraceDays { get; set; } = 0;       // Ngày ân hạn

        // Optional defaults you are already using elsewhere
        public decimal DefaultElectricRate { get; set; } = 0; //Số tiền mặc định cho mỗi kWh điện, cập nhật vào ElectricReading.Rate khi tạo mới
        public decimal DefaultWaterRate { get; set; } = 0; //Số tiền mặc định cho mỗi m3 nước, cập nhật vào WaterReading.Rate khi tạo mới

        // NEW: Mức phí phạt đóng tiền trễ (áp dụng khi RoomCharge chuyển Late)
        public decimal LateFeeRate { get; set; } = 50000m;
    }
}