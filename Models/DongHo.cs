using AMS.Models;

namespace AMS.Models;

public enum LoaiDongHo { Dien, Nuoc }

public class DongHo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PhongId { get; set; }
    public LoaiDongHo Loai { get; set; }
    public string MaDongHo { get; set; } = string.Empty; // ví dụ: COCONUT-DIEN

    // Navigation
    public Phong? Phong { get; set; }
    public ICollection<ChiSoDongHo> CacChiSo { get; set; } = new List<ChiSoDongHo>();
}