namespace AMS.Models;

public class ChiSoDongHo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DongHoId { get; set; }

    public string Ky { get; set; } = string.Empty; // yyyy-MM
    public decimal ChiSo { get; set; }            // chỉ số chốt kỳ
    public DateTime NgayDoc { get; set; }
    public string? AnhChiSo { get; set; }         // đường dẫn ảnh
    public string Nguon { get; set; } = "ThuCong"; // ThuCong/CSV/AI
    public double? DoTinCay { get; set; }

    // Navigation
    public DongHo? DongHo { get; set; }
}