using System;
using System.Collections.Generic;
using System.Text;

namespace AMS.Models
{
    public class Phong
    {
        public int Id { get; set; }
        public int NhaID { get; set; } //Foreign Key đến Nhà
        public string MaPhong { get; set; } //BANANA, APPLE

        public double DienTich { get; set; }
        public decimal GiaThue { get; set; }
        public string Status { get; set; } //Available, Occupied, Maintenance
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation property
        public Nha Nha { get; set; }
    }
}
