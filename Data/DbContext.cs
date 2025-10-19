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

            // Seed dữ liệu mẫu cho House
            modelBuilder.Entity<House>().HasData(
                new House
                {
                    IdHouse = 1,
                    Address = "123 Đường ABC, Quận XYZ, TP HCM",
                    TotalRooms = 10,
                    Notes = "Nhà cho thuê 10 phòng",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
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
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
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
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
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
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
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
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
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
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
            // Cấu hình cho Người Thuê
            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.HasKey(e => e.IdTenant);
                entity.Property(e => e.FullName).IsRequired();
                entity.Property(e => e.IdCardNumber).IsRequired();
                entity.Property(e => e.MonthlyRent).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.DepositAmount).HasColumnType("decimal(18, 2)");
            });
        }
    }
}