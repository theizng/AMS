using System;

namespace AMS.Models
{
    public class WaterReading
    {
        public string WaterReadingId { get; set; } = Guid.NewGuid().ToString("N");
        public string RoomChargeId { get; set; } //Liên kết tới RoomCharge
        public int Previous { get; set; } //Chỉ số nước kỳ trước, có thể điền vào Sheet cho WaterTemplate Invoice
        public int Current { get; set; } //Chỉ số nước kỳ này, có thể điền vào Sheet cho WaterTemplate Invoice
        public int Consumption => Current - Previous; //Lượng nước tiêu thụ trong kỳ, có thể điền vào Sheet cho WaterTemplate
        public decimal Rate { get; set; }  // VND / m3, Có thể điền vào UnitPrice trong Sheet cho WaterTemplate Invoice, tham chiếu DefaultWaterRate trong PaymentSettings khi tạo mới
        public decimal Amount => Consumption * Rate; //Tổng tiền nước, có thể điền vào Amount trong Sheet cho WaterTemplate Invoice
        public bool Confirmed { get; set; } //Đánh dấu đã xác nhận chỉ số nước và tiền nước đúng (Trang PaymentMeterEntryPage).
    }
}