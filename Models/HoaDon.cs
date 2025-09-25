using System;
using System.Collections.Generic;
using System.Text;

namespace AMS.Models;
public enum TrangThaiHoaDon { NhaNhap, DaGui, ThuMotPhan, DaThu, QuaHan }

public class HoaDon
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PhongId { get; set; }
    public string Ky { get; set; } = string.Empty; // yyyy-MM
    public DateTime NgayPhatHanh { get; set; }
    public DateTime HanThanhToan { get; set; }
    public decimal TongTien { get; set; }
    public TrangThaiHoaDon TrangThai { get; set; } = TrangThaiHoaDon.NhaNhap;
    public string? TepPdf { get; set; }

    public Phong? Phong { get; set; }
    public ICollection<DongHoaDon> CacDong { get; set; } = new List<DongHoaDon>();
}

public class DongHoaDon
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HoaDonId { get; set; }
    public string MoTa { get; set; } = string.Empty;
    public decimal SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public decimal ThanhTien { get; set; }

    public HoaDon? HoaDon { get; set; }
}