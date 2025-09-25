using System;
using System.Collections.Generic;
using System.Text;

namespace AMS.Models;
public enum TrangThaiPhong { Trong, DangThue, DuKien }
public enum LoaiPhong { Trong, CoDoCoBan }

public class Phong
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid NhaId { get; set; }
    public string MaPhong { get; set; } = string.Empty;  // ví dụ: COCONUT
    public string TenPhong { get; set; } = string.Empty;
    public LoaiPhong Loai { get; set; }
    public double DienTich { get; set; }
    public decimal GiaThueCoBan { get; set; }
    public TrangThaiPhong TrangThai { get; set; }
    public string? GhiChu { get; set; }

    // Navigation
    public Nha? Nha { get; set; }
    public ICollection<HopDong> HopDongs { get; set; } = new List<HopDong>();
}