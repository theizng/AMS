using System;

namespace AMS.Models
{
    public class ElectricReading
    {
        public string ElectricReadingId { get; set; } = Guid.NewGuid().ToString("N");
        public string RoomChargeId { get; set; } //Liên kết tới RoomCharge
        public int Previous { get; set; } //Chỉ số điện kỳ trước, có thể điền vào Sheet cho ElectricTemplate Invoice
        public int Current { get; set; } //Chỉ số điện kỳ này, có thể điền vào Sheet cho ElectricTemplate Invoice
        public int Consumption => Current - Previous; //Lượng điện tiêu thụ trong kỳ, có thể điền vào Sheet cho ElectricTemplate
        public decimal Rate { get; set; }  // VND / kWh Có thể điền vào UnitPrice trong Sheet cho ElectricTemplate Invoice, tham chiếu DefaultElectricRate trong PaymentSettings khi tạo mới
        public decimal Amount => Consumption * Rate; //Tổng tiền điện, có thể điền vào Amount trong Sheet cho ElectricTemplate Invoice
        public bool Confirmed { get; set; } //Đánh dấu đã xác nhận chỉ số điện và tiền điện đúng (Trang PaymentMeterEntryPage).
    }
}