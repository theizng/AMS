using System;
using System.Collections.Generic;
using System.Text;

namespace AMS.Models;
public enum TrangThaiHopDong { HieuLuc, KetThuc, ChoKy }

public class HopDong
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PhongId { get; set; }
    public string SoHopDong { get; set; } = string.Empty;
    public DateTime NgayBatDau { get; set; }
    public DateTime? NgayKetThuc { get; set; }
    public decimal TienCoc { get; set; }
    public string? TepHopDong { get; set; } // đường dẫn file
    public TrangThaiHopDong TrangThai { get; set; } = TrangThaiHopDong.HieuLuc;

    public Phong? Phong { get; set; }
    public ICollection<HopDongThue> HopDongThues { get; set; } = new List<HopDongThue>();
}