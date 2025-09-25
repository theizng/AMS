using System;
using System.Collections.Generic;
using System.Text;

namespace AMS.Models;

public class Nha
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Ten { get; set; } = "Nha QLT";
    public string DiaChi { get; set; } = string.Empty;
    public int TongSoPhong { get; set; }
    public string? GhiChu { get; set; }

    public ICollection<Phong> Phongs { get; set; } = new List<Phong>();
}
