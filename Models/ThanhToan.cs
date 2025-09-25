using AMS.Models;

namespace AMS.Models;

public class ThanhToan
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? PhongId { get; set; }
    public Guid? KhachThueId { get; set; }
    public Guid? HoaDonId { get; set; }

    public DateTime Ngay { get; set; }
    public decimal SoTien { get; set; }
    public string PhuongThuc { get; set; } = "TienMat/ChuyenKhoan";
    public string? ThamChieu { get; set; } // mã giao dịch, số phiếu, ...
    public string TrangThai { get; set; } = "DaHachToan"; // hoặc "TamUng", "Huy"
    public string? GhiChu { get; set; }

    // Navigation
    public Phong? Phong { get; set; }
    public KhachThue? KhachThue { get; set; }
    public HoaDon? HoaDon { get; set; }
}