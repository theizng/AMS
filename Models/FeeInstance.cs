using System;

namespace AMS.Models
{
    public class FeeInstance
    {
        //Từ FeeInstace này, có thể điền vào {{ITEMS_START}} trong Sheet của ThongBaoPhi invoice -> dùng để liệt kê các phí phát sinh ngoài tiền phòng, tiền điện, tiền nước
        public string FeeInstanceId { get; set; } = Guid.NewGuid().ToString("N");
        public string RoomChargeId { get; set; } //Liên kết tới RoomCharge
        public string? FeeTypeId { get; set; } //Liên kết tới FeeType
        public string Name { get; set; } // Tên phí, sao chép từ FeeType.Name tại thời điểm tạo FeeInstance
        public decimal Rate { get; set; } // Mức phí cho mỗi đơn vị tính, có thể tùy chỉnh so với FeeType.DefaultRate
        public decimal Quantity { get; set; } = 1; // Số lượng đơn vị tính, mặc định là 1
        public decimal Amount => Rate * Quantity; // Tổng tiền phí, tính toán dựa trên Rate và Quantity
    }
}