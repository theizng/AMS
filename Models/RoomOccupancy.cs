    using System;

namespace AMS.Models
{
    // Đại diện cho việc một cá nhân thuê phòng trong một khoảng thời gian cụ thể.
    public class RoomOccupancy
    {
        // Primary Key
        public int IdRoomOccupancy { get; set; }
        // Foreign Keys
        public int RoomId { get; set; }
        // Foreign Key to Tenant
        public int TenantId { get; set; }
        // Ngày bắt đầu ở
        public DateTime MoveInDate { get; set; }
        // Ngày dọn đi (nếu có)
        public DateTime? MoveOutDate { get; set; } // null = still active
        // Mức đặt cọc mà người thuê đã đóng cho chủ nhà (cá nhân)
        public decimal DepositContribution { get; set; }
        // Số lượng xe máy đang giữ trong phòng của cá nhân này
        public int BikeCount { get; set; }

        public Room? Room { get; set; }
        public Tenant? Tenant { get; set; }


        // Nếu MoveOutDate là null, tức là vẫn đang ở.
        public bool IsActive => !MoveOutDate.HasValue;
    }
}