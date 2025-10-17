using AMS.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace AMS.Data
{
    public partial class AMSDbContext : DbContext
    {
        public DbSet<Admin> Admin { get; set; }
        public DbSet<NguoiThue> NguoiThues { get; set; }
        //public DbSet<Phong>
        public DbSet<Phong> Phongs { get; set; }
        public DbSet<Nha> Nhas { get; set; }
        public AMSDbContext(DbContextOptions<AMSDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed admin account
            string passwordHash = BCrypt.Net.BCrypt.HashPassword("admin123");

            modelBuilder.Entity<Admin>().HasData(
                new Admin
                {
                    AdminId = 1,
                    Username = "admin",
                    Email = "admin@example.com",
                    PhoneNumber = "0123456789",
                    PasswordHash = passwordHash,
                    FullName = "Quản Trị Viên",
                    LastLogin = DateTime.UtcNow
                }
            );
            //Cấu hình cho Nhà
            modelBuilder.Entity<Nha>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DiaChi).IsRequired();
                entity.Property(e => e.TotalRooms).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            }
            );
            // Cấu hình cho Phòng
            modelBuilder.Entity<Phong>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MaPhong).IsRequired();
                entity.Property(e => e.GiaThue).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                //Relationship với House
                entity.HasOne(e => e.Nha)
                    .WithMany()
                    .HasForeignKey(e => e.NhaID);
            });

            // Seed dữ liệu mẫu cho House
            modelBuilder.Entity<Nha>().HasData(
                new Nha
                {
                    Id = 1,
                    DiaChi = "123 Đường ABC, Quận XYZ, TP HCM",
                    TotalRooms = 10,
                    Notes = "Nhà cho thuê 10 phòng",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );

            // Seed dữ liệu mẫu cho Room
            modelBuilder.Entity<Phong>().HasData(
                new Phong
                {
                    Id = 1,
                    NhaID = 1,
                    MaPhong = "COCONUT",
                    DienTich = 25.5,
                    GiaThue = 3000000M,
                    Status = "Renting",  // Đang thuê
                    Notes = "Phòng thường",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Phong
                {
                    Id = 2,
                    NhaID = 1,
                    MaPhong = "APPLE",
                    DienTich = 25.0,
                    GiaThue = 3000000M,
                    Status = "Renting",  // Đang thuê
                    Notes = "Phòng thường",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Phong
                {
                    Id = 3,
                    NhaID = 1,
                    MaPhong = "BANANA",
                    DienTich = 30.0,
                    GiaThue = 3500000M,
                    Status = "Renting",  // Đang thuê
                    Notes = "Phòng thường, có 1 con mèo",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Phong
                {
                    Id = 4,
                    NhaID = 1,
                    MaPhong = "PAPAYA",
                    DienTich = 35.0,
                    GiaThue = 4000000M,
                    Status = "Renting",  // Đang thuê
                    Notes = "Phòng có đồ cơ bản, có 2 con chó",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Phong
                {
                    Id = 5,
                    NhaID = 1,
                    MaPhong = "STRAWBERRY",
                    DienTich = 35.0,
                    GiaThue = 4000000M,
                    Status = "Renting",  // Đang thuê
                    Notes = "Phòng có đồ cơ bản",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
            // Cấu hình cho Người Thuê
            modelBuilder.Entity<NguoiThue>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FullName).IsRequired();
                entity.Property(e => e.IdCardNumber).IsRequired();
                entity.Property(e => e.MonthlyRent).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.DepositAmount).HasColumnType("decimal(18, 2)");
            });
        }
    }
}