using AMS.Models;

namespace AMS.Models;

public enum LoaiGiayTo { CMND, CCCD, HoChieu }

public class KhachThue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string HoTen { get; set; } = string.Empty;
    public string SoDienThoai { get; set; } = string.Empty;
    public LoaiGiayTo LoaiGiayTo { get; set; }
    public string SoGiayTo { get; set; } = string.Empty;
    public string? Email { get; set; }

    // Navigation
    public ICollection<HopDongThue> HopDongThues { get; set; } = new List<HopDongThue>();
}