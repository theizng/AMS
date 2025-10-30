using AMS.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace AMS.Data
{
    public partial class AMSDbContext : DbContext
    {
        public DbSet<Admin> Admin { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<House> Houses { get; set; }
        public DbSet<RoomOccupancy> RoomOccupancies { get; set; } = null!;
        public DbSet<Bike> Bikes { get; set; } = null!;

        public AMSDbContext(DbContextOptions<AMSDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed admin account (fixed dates to keep migrations stable)
            var fixedDate = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
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

            // HOUSE
            modelBuilder.Entity<House>(entity =>
            {
                entity.HasKey(e => e.IdHouse);
                entity.Property(e => e.Address).IsRequired();
                entity.Property(e => e.TotalRooms).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // ROOM
            modelBuilder.Entity<Room>(entity =>
            {
                entity.HasKey(e => e.IdRoom);

                entity.Property(e => e.HouseID).IsRequired();
                entity.Property(e => e.RoomCode).IsRequired().HasMaxLength(32);

                // Unique RoomCode per House
                entity.HasIndex(e => new { e.HouseID, e.RoomCode }).IsUnique();

                // Money/number columns (SQLite: affinity Numeric)
                entity.Property(e => e.Area).HasColumnType("decimal(10,2)");
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.Property(e => e.BikeExtraFee).HasColumnType("decimal(18,2)").HasDefaultValue(100000m);

                // Defaults
                entity.Property(e => e.MaxOccupants).HasDefaultValue(1);
                entity.Property(e => e.FreeBikeAllowance).HasDefaultValue(1);

                // Status as int, default Available (optional)
                entity.Property(e => e.RoomStatus)
                      .HasConversion<int>()
                      .HasDefaultValue(Room.Status.Available);

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Relationship with House
                entity.HasOne(e => e.House)
                      .WithMany()
                      .HasForeignKey(e => e.HouseID)
                      .OnDelete(DeleteBehavior.Restrict);

                // Keep history when deleting Room (do not cascade delete RoomOccupancies)
                entity.HasMany(e => e.RoomOccupancies)
                      .WithOne(ro => ro.Room!)
                      .HasForeignKey(ro => ro.RoomId)
                      .OnDelete(DeleteBehavior.Restrict);

                // CHECK constraints (SQLite supports)
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Room_Area_Positive", "[Area] > 0");
                    t.HasCheckConstraint("CK_Room_Price_NonNegative", "[Price] >= 0");
                    t.HasCheckConstraint("CK_Room_MaxOccupants_Positive", "[MaxOccupants] >= 1");
                    t.HasCheckConstraint("CK_Room_FreeBikeAllowance_NonNegative", "[FreeBikeAllowance] >= 0");
                    t.HasCheckConstraint("CK_Room_BikeExtraFee_NonNegative", "[BikeExtraFee] IS NULL OR [BikeExtraFee] >= 0");
                });

                // If your Room has UI-only properties, ensure EF skips them (harmless if property absent)
                // entity.Ignore(r => r.ActiveOccupants);

                // OPTIONAL (commented): If you add Room.EmergencyContactRoomOccupancyId (int?),
                // map it as a nullable FK pointing to RoomOccupancy

                entity.HasOne<RoomOccupancy>()
                      .WithMany()
                      .HasForeignKey(r => r.EmergencyContactRoomOccupancyId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ROOM OCCUPANCY
            modelBuilder.Entity<RoomOccupancy>(entity =>
            {
                entity.HasKey(e => e.IdRoomOccupancy);

                entity.Property(e => e.DepositContribution).HasColumnType("decimal(18,2)");
                entity.Property(e => e.BikeCount).HasDefaultValue(0);

                // Room 1 - N RoomOccupancy (keep history)
                entity.HasOne(e => e.Room)
                      .WithMany(r => r.RoomOccupancies)
                      .HasForeignKey(e => e.RoomId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Tenant 1 - N RoomOccupancy (keep history)
                entity.HasOne(e => e.Tenant)
                      .WithMany(t => t.RoomOccupancies)
                      .HasForeignKey(e => e.TenantId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Useful indexes for active/ended lookups
                entity.HasIndex(e => new { e.RoomId, e.MoveOutDate });
                entity.HasIndex(e => new { e.TenantId, e.MoveOutDate });
            });

            // BIKE
            modelBuilder.Entity<Bike>(entity =>
            {
                entity.ToTable("Bikes");
                entity.HasKey(b => b.Id);

                entity.Property(b => b.RoomId).IsRequired();
                entity.Property(b => b.Plate).IsRequired().HasMaxLength(32);
                entity.Property(b => b.OwnerId).IsRequired();

                entity.Property(b => b.IsActive).HasDefaultValue(true);
                entity.Property(b => b.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Indexes
                entity.HasIndex(b => b.Plate);
                entity.HasIndex(b => new { b.RoomId, b.Plate }).IsUnique();

                // FK -> Room
                entity.HasOne(b => b.Room)
                      .WithMany()
                      .HasForeignKey(b => b.RoomId)
                      .OnDelete(DeleteBehavior.Cascade);

                // FK -> Tenant as Owner
                entity.HasOne(b => b.OwnerTenant)
                      .WithMany() // if later you add Tenant.Bikes, change to .WithMany(t => t.Bikes)
                      .HasForeignKey(b => b.OwnerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // TENANT
            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.HasKey(e => e.IdTenant);
                entity.Property(e => e.FullName).IsRequired();
                entity.Property(e => e.IdCardNumber).IsRequired();
                entity.Property(e => e.ContractUrl).IsRequired(false);

                entity.Property(e => e.MonthlyRent).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.DepositAmount).HasColumnType("decimal(18, 2)");

                // Optional: add quick-search indexes if you often search by name/phone
                // entity.HasIndex(e => e.FullName);
                // entity.HasIndex(e => e.PhoneNumber);
            });
        }
    }
}