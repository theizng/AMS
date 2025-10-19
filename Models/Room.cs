using System;
using System.Collections.Generic;
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
        public int IdRoom { get; set; }
        public int HouseID { get; set; } //Foreign Key đến Nhà
        public string RoomCode { get; set; } //BANANA, APPLE
        public Status RoomStatus { get; set; } = Status.Available;
        public decimal Area { get; set; }
        public decimal Price { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation property
        public House? House { get; set; }
    }
}
