using AMS.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace AMS.Data
{
    public partial class AMSDbContext : DbContext
    {
        public DbSet<Admin> Admin { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<House> Houses { get; set; }
        public DbSet<RoomOccupancy> RoomOccupancies { get; set; } = null!;

        public AMSDbContext(DbContextOptions<AMSDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed admin account
            //string passwordHash = BCrypt.Net.BCrypt.HashPassword("admin123");
            var fixedDate = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);  // Fixed date cho tất cả seed (thay năm/tháng/ngày nếu cần)
            modelBuilder.Entity<Admin>().HasData(
                new Admin
                {
                    AdminId = 1,
                    Username = "admin",
                    Email = "admin@example.com",
                    PhoneNumber = "0123456789",
                    PasswordHash = "$2b$12$Dvin/fmQwvI7yF8PVrC//uRlbRmkTCzsFG1xO7xGOcG/N2QBITIqS",
                    FullName = "Quản Trị Viên",
                    LastLogin = fixedDate
                }
            );

            //Cấu hình cho Nhà
            modelBuilder.Entity<House>(entity =>
            {
                entity.HasKey(e => e.IdHouse);
                entity.Property(e => e.Address).IsRequired();
                entity.Property(e => e.TotalRooms).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            }
            );

            // Cấu hình cho Phòng
            modelBuilder.Entity<Room>(entity =>
            {
                entity.HasKey(e => e.IdRoom);
                entity.Property(e => e.RoomCode).IsRequired();
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.Property(e => e.RoomStatus).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                //Relationship với House
                entity.HasOne(e => e.House)
                    .WithMany()
                    .HasForeignKey(e => e.HouseID);
            });
            //Các quy định cho Room
            // Room policy defaults (optional; you can seed values per house/room)
            modelBuilder.Entity<Room>()
                .Property(r => r.MaxOccupants)
                .HasDefaultValue(1);

            modelBuilder.Entity<Room>()
                .Property(r => r.FreeBikeAllowance)
                .HasDefaultValue(1);

            modelBuilder.Entity<Room>()
                .Property(r => r.BikeExtraFee)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(100000m);
            // Cấu hình cho RoomOccupancy
            modelBuilder.Entity<RoomOccupancy>(entity =>
            {
                entity.HasKey(e => e.IdRoomOccupancy);
                entity.Property(e => e.DepositContribution).HasColumnType("decimal(18,2)");
                entity.Property(e => e.BikeCount).HasDefaultValue(0);

                //Room 1 - N RoomOccupancy: vì 1 phòng có thể có nhiều người thuê và RoomOccupancy lưu thông tin người thuê trong phòng đó
                entity.HasOne(e => e.Room)
                    .WithMany(r => r.RoomOccupancies) //
                    .HasForeignKey(e => e.RoomId)
                    .OnDelete(DeleteBehavior.Restrict); // Giữ lại lịch sử thuê khi phòng bị xóa
                //Tenant 1 - N RoomOccupancy: vì 1 người thuê có thể thuê nhiều phòng qua các thời kỳ khác nhau
                entity.HasOne(e => e.Tenant)
                    .WithMany(t => t.RoomOccupancies)
                    .HasForeignKey(e => e.TenantId)
                    .OnDelete(DeleteBehavior.Restrict); // Giữ lại lịch sử thuê khi người thuê bị xóa

                // Tạo index để tối ưu truy vấn theo RoomId và TenantId
                entity.HasIndex(e => new { e.RoomId, e.MoveOutDate });
                entity.HasIndex(e => new { e.TenantId, e.MoveOutDate });
            });

            // Seed dữ liệu mẫu cho House
            modelBuilder.Entity<House>().HasData(
                new House
                {
                    IdHouse = 1,
                    Address = "123 Đường ABC, Quận XYZ, TP HCM",
                    TotalRooms = 10,
                    Notes = "Nhà cho thuê 10 phòng",
                    CreatedAt = fixedDate,
                    UpdatedAt = fixedDate
                }
            );

            // Seed dữ liệu mẫu cho Room
            modelBuilder.Entity<Room>().HasData(
                new Room
                {
                    IdRoom = 1,
                    HouseID = 1,
                    RoomCode = "COCONUT",
                    Area = 25,
                    Price = 3000000M,
                    RoomStatus = Room.Status.Available,  // Đang thuê
                    Notes = "Phòng thường",
                    CreatedAt = fixedDate,
                    UpdatedAt = fixedDate
                },
                new Room
                {
                    IdRoom = 2,
                    HouseID = 1,
                    RoomCode = "APPLE",
                    Area = 25,
                    Price = 3000000M,
                    RoomStatus = Room.Status.Available,  // Đang thuê
                    Notes = "Phòng thường",
                    CreatedAt = fixedDate,
                    UpdatedAt = fixedDate
                },
                new Room
                {
                    IdRoom = 3,
                    HouseID = 1,
                    RoomCode = "BANANA",
                    Area = 30,
                    Price = 3500000M,
                    RoomStatus = Room.Status.Available,  // Đang thuê
                    Notes = "Phòng thường, có 1 con mèo",
                    CreatedAt = fixedDate,
                    UpdatedAt = fixedDate
                },
                new Room
                {
                    IdRoom = 4,
                    HouseID = 1,
                    RoomCode = "PAPAYA",
                    Area = 35,
                    Price = 4000000M,
                    RoomStatus = Room.Status.Available,  // Đang thuê
                    Notes = "Phòng có đồ cơ bản, có 2 con chó",
                    CreatedAt = fixedDate,
                    UpdatedAt = fixedDate
                },
                new Room
                {
                    IdRoom = 5,
                    HouseID = 1,
                    RoomCode = "STRAWBERRY",
                    Area = 35,
                    Price = 4000000M,
                    RoomStatus = Room.Status.Available,  // Đang thuê
                    Notes = "Phòng có đồ cơ bản",
                    CreatedAt = fixedDate,
                    UpdatedAt = fixedDate
                }
            );

            // Cấu hình cho Người Thuê
            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.HasKey(e => e.IdTenant);
                entity.Property(e => e.ContractUrl).IsRequired(false);
                entity.Property(e => e.FullName).IsRequired();
                entity.Property(e => e.IdCardNumber).IsRequired();
                entity.Property(e => e.MonthlyRent).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.DepositAmount).HasColumnType("decimal(18, 2)");
            });
        }
    }
}