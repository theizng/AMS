using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace AMS.Models
{
    public class Room
    {
        public enum Status
        {
            Available = 0,
            Occupied = 1,
            Maintaining = 2,
            Inactive = 3
        }
        // Primary Key
        public int IdRoom { get; set; }
        // Foreign Key đến Nhà
        public int HouseID { get; set; } 
        // Mã phòng, duy nhất trong cùng một nhà. Ví dụ: BANANA, APPLE
        public string RoomCode { get; set; } //BANANA, APPLE
        // Trạng thái phòng, mặc định Available.
        public Status RoomStatus { get; set; } = Status.Available;
        //Diện tích phòng (m2)
        public decimal Area { get; set; }
        // Giá thuê phòng
        public decimal Price { get; set; }
        //Ghi chú thêm về phòng (nếu có)
        public string? Notes { get; set; }
        // Số người tối đa được ở trong phòng (Chủ nhà quy định theo loại phòng)
        public int MaxOccupants { get; set; }
        // Số xe máy được giữ miễn phí, mặc định 1 (theo yêu cầu đề bài)
        public int MaxBikeAllowance { get; set; }
        public int FreeBikeAllowance { get; set; } = 1;
        // Phí giữ xe máy thêm (nếu có)
        public decimal? BikeExtraFee { get; set; }
        public int? EmergencyContactRoomOccupancyId { get; set; } // FK -> RoomOccupancy.IdRoomOccupancy
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation property
        public House? House { get; set; }
        public ICollection<RoomOccupancy>? RoomOccupancies { get; set; }

        [NotMapped]
        public int ActiveOccupants { get; set; }
    }
}
