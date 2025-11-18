using System;

namespace AMS.Models
{
    public class FeeType
    {
        public string FeeTypeId { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } //"Phí dọn phòng", "Phí đậu xe", ...
        public bool IsRecurring { get; set; } //Boolean đánh dấu phí này có áp dụng cho mỗi kỳ thanh toán hay không
        public string? UnitLabel { get; set; }  // Đơn vị tính của phí này e.g ., "lần", "xe", ....
        public decimal DefaultRate { get; set; } // Mức phí mặc định cho mỗi đơn vị tính
        public bool ApplyAllRooms { get; set; } // Boolean đánh dấu phí này có áp dụng cho tất cả các phòng hay không
        public bool Active { get; set; } = true; // Đánh dấu phí này có đang được sử dụng hay không
    }
}