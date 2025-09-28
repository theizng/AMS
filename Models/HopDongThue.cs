using System;
using System.Collections.Generic;
using System.Text;

namespace AMS.Models;
public class HopDongThue
{
    public Guid HopDongId { get; set; }
    public Guid KhachThueId { get; set; }

    // Thời gian người này ở thực tế trong khuôn khổ hợp đồng
    public DateTime TuNgay { get; set; }
    public DateTime? DenNgay { get; set; }

    // Đánh dấu người chịu trách nhiệm thanh toán
    public bool LaNguoiTraTien { get; set; }

    // Navigation
    public HopDong? HopDong { get; set; }
    public KhachThue? KhachThue { get; set; }
}