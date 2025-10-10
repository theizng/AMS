/*
using Microsoft.EntityFrameworkCore;
using AMS.Models;

namespace AMS.Data;

public class QltDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public QltDbContext(DbContextOptions<QltDbContext> options) : base(options) { }

    public DbSet<Nha> Nhas { get; set; } = null!;
    public DbSet<Phong> Phongs { get; set; } = null!;
    public DbSet<KhachThue> KhachThues { get; set; } = null!;
    public DbSet<HopDong> HopDongs { get; set; } = null!;
    public DbSet<HopDongThue> HopDongThues { get; set; } = null!;
    public DbSet<HoaDon> HoaDons { get; set; } = null!;
    public DbSet<DongHoaDon> DongHoaDons { get; set; } = null!;
    public DbSet<ThanhToan> ThanhToans { get; set; } = null!;
    public DbSet<DongHo> DongHos { get; set; } = null!;
    public DbSet<ChiSoDongHo> ChiSoDongHos { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // MaPhong unique
        modelBuilder.Entity<Phong>()
            .HasIndex(p => p.MaPhong)
            .IsUnique();

        // HopDongThue: composite key
            modelBuilder.Entity<HopDongThue>()
                .HasKey(x => new { x.HopDongId, x.KhachThueId, x.TuNgay });

            modelBuilder.Entity<HopDongThue>()
                .HasOne(x => x.HopDong)
                .WithMany(h => h.HopDongThues)
                .HasForeignKey(x => x.HopDongId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HopDongThue>()
                .HasOne(x => x.KhachThue)
                .WithMany(k => k.HopDongThues)
                .HasForeignKey(x => x.KhachThueId)
                .OnDelete(DeleteBehavior.Cascade);

        // DongHo: one per type per room
        modelBuilder.Entity<DongHo>()
            .HasIndex(d => new { d.PhongId, d.Loai })
            .IsUnique();

        // ChiSoDongHo: one per meter per period
        modelBuilder.Entity<ChiSoDongHo>()
            .HasIndex(c => new { c.DongHoId, c.Ky })
            .IsUnique();

        /*
        // Money/decimal types for SQL Server
        modelBuilder.Entity<Phong>().Property(p => p.GiaThueCoBan).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<HopDong>().Property(p => p.TienCoc).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<HoaDon>().Property(p => p.TongTien).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<DongHoaDon>().Property(p => p.SoLuong).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<DongHoaDon>().Property(p => p.DonGia).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<DongHoaDon>().Property(p => p.ThanhTien).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<ThanhToan>().Property(p => p.SoTien).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<ChiSoDongHo>().Property(p => p.ChiSo).HasColumnType("decimal(18,3)");

        base.OnModelCreating(modelBuilder);
        */
//   }
//}

