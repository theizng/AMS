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
        public DbSet<Invoice> Invoices { get; set; } = null!;
        public DbSet<Contract> Contracts { get; set; } = null!;
        public DbSet<ContractAddendum> ContractAddendums { get; set; } = null!;

        // NEW: Payments domain
        public DbSet<PaymentCycle> PaymentCycles { get; set; } = null!;
        public DbSet<RoomCharge> RoomCharges { get; set; } = null!;
        public DbSet<FeeType> FeeTypes { get; set; } = null!;
        public DbSet<FeeInstance> FeeInstances { get; set; } = null!;
        public DbSet<PaymentRecord> PaymentRecords { get; set; } = null!;

        public AMSDbContext(DbContextOptions<AMSDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed admin account
            var fixedDate = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            modelBuilder.Entity<Admin>().HasData(new Admin
            {
                AdminId = 1,
                Username = "admin",
                Email = "admin@example.com",
                PhoneNumber = "0123456789",
                PasswordHash = "$2b$12$Dvin/fmQwvI7yF8PVrC//uRlbRmkTCzsFG1xO7xGOcG/N2QBITIqS",
                FullName = "Quản Trị Viên",
                LastLogin = fixedDate
            });

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

                entity.HasIndex(e => e.RoomCode).IsUnique();
                entity.Property(e => e.HouseID).IsRequired();
                entity.Property(e => e.RoomCode).IsRequired().HasMaxLength(32);

                // Unique RoomCode per House
                entity.HasIndex(e => new { e.HouseID, e.RoomCode }).IsUnique();

                entity.Property(e => e.Area).HasColumnType("decimal(10,2)");
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.Property(e => e.BikeExtraFee).HasColumnType("decimal(18,2)").HasDefaultValue(100000m);

                entity.Property(e => e.MaxOccupants).HasDefaultValue(1);
                entity.Property(e => e.FreeBikeAllowance).HasDefaultValue(1);
                entity.Property(e => e.MaxBikeAllowance).HasDefaultValue(1);

                entity.Property(e => e.RoomStatus)
                      .HasConversion<int>()
                      .HasDefaultValue(Room.Status.Available);

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.House)
                      .WithMany()
                      .HasForeignKey(e => e.HouseID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.RoomOccupancies)
                      .WithOne(ro => ro.Room!)
                      .HasForeignKey(ro => ro.RoomId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Room_MaxBikeAllowance_NonNegative", "[MaxBikeAllowance] >= 0");
                    t.HasCheckConstraint("CK_Room_Area_Positive", "[Area] > 0");
                    t.HasCheckConstraint("CK_Room_Price_NonNegative", "[Price] >= 0");
                    t.HasCheckConstraint("CK_Room_MaxOccupants_Positive", "[MaxOccupants] >= 1");
                    t.HasCheckConstraint("CK_Room_FreeBikeAllowance_NonNegative", "[FreeBikeAllowance] >= 0");
                    t.HasCheckConstraint("CK_Room_FreeBikeAllowance_Within_Max", "([MaxBikeAllowance] = 0 OR [FreeBikeAllowance] <= [MaxBikeAllowance])");
                });

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

                entity.HasOne(e => e.Room)
                      .WithMany(r => r.RoomOccupancies)
                      .HasForeignKey(e => e.RoomId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Tenant)
                      .WithMany(t => t.RoomOccupancies)
                      .HasForeignKey(e => e.TenantId)
                      .OnDelete(DeleteBehavior.Restrict);

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

                entity.HasIndex(b => b.Plate);
                entity.HasIndex(b => new { b.RoomId, b.Plate }).IsUnique();

                entity.HasOne(b => b.Room)
                      .WithMany()
                      .HasForeignKey(b => b.RoomId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(b => b.OwnerTenant)
                      .WithMany(t => t.Bikes!)
                      .HasForeignKey(b => b.OwnerId)
                      .HasPrincipalKey(t => t.IdTenant)
                      .OnDelete(DeleteBehavior.Cascade);
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
                entity.HasIndex(e => e.FullName);
                entity.HasIndex(e => e.PhoneNumber);
            });

            // PAYMENTS: legacy Invoices (kept)
            modelBuilder.Entity<Invoice>(e =>
            {
                e.ToTable("Invoices");
                e.HasKey(i => i.Id);

                e.Property(i => i.RoomId).IsRequired();
                e.Property(i => i.BillingMonth).IsRequired();

                e.Property(i => i.Status).HasConversion<int>().HasDefaultValue(InvoiceStatus.Unpaid);
                e.Property(i => i.BaseRent).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                e.Property(i => i.Utilities).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                e.Property(i => i.Extras).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                e.Property(i => i.TotalAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                e.Property(i => i.PaidAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0);

                e.HasOne(i => i.Room)
                    .WithMany()
                    .HasForeignKey(i => i.RoomId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(i => new { i.RoomId, i.BillingMonth }).IsUnique();

                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Invoice_Total_NonNegative", "[TotalAmount] >= 0");
                    t.HasCheckConstraint("CK_Invoice_Paid_NonNegative", "[PaidAmount] >= 0");
                    t.HasCheckConstraint("CK_Invoice_Paid_Le_Total", "[PaidAmount] <= [TotalAmount]");
                });
            });

            // CONTRACT
            modelBuilder.Entity<Contract>(entity =>
            {
                entity.HasKey(e => e.ContractId);
                entity.Property(e => e.ContractId).IsRequired().HasMaxLength(64);

                entity.Property(e => e.ContractNumber).HasMaxLength(64).IsRequired(false);

                entity.Property(e => e.RoomCode).IsRequired().HasMaxLength(64);
                entity.Property(e => e.HouseAddress).HasMaxLength(256).IsRequired(false);

                entity.Property(e => e.TenantsJson).HasColumnType("TEXT").IsRequired();

                entity.Property(e => e.RentAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.SecurityDeposit).HasColumnType("decimal(18,2)").HasDefaultValue(0m);

                entity.Property(e => e.PropertyDescription).HasColumnType("TEXT");
                entity.Property(e => e.PdfUrl).HasMaxLength(1024).IsRequired(false);

                entity.Property(e => e.Status)
                      .HasConversion<int>()
                      .HasDefaultValue(ContractStatus.Draft);

                entity.Property(e => e.NeedsAddendum).HasDefaultValue(false);
                entity.Property(e => e.AddendumNotifiedAt).IsRequired(false);

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasMany(entity => entity.Addendums)
                      .WithOne(a => a.Parent!)
                      .HasForeignKey(ca => ca.ParentContractId)
                      .HasPrincipalKey(c => c.ContractId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.RoomCode);
                entity.HasIndex(e => e.ContractNumber).IsUnique(false);
            });

            // CONTRACT ADDENDUM
            modelBuilder.Entity<ContractAddendum>(entity =>
            {
                entity.HasKey(e => e.AddendumId);
                entity.Property(e => e.ParentContractId).IsRequired().HasMaxLength(64);
                entity.Property(e => e.AddendumNumber).HasMaxLength(64).IsRequired(false);
                entity.Property(e => e.PdfUrl).HasMaxLength(1024).IsRequired(false);

                entity.Property(e => e.OldSnapshotJson).HasColumnType("TEXT").IsRequired();
                entity.Property(e => e.NewSnapshotJson).HasColumnType("TEXT").IsRequired();

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.AddendumNumber).IsUnique(false);
                entity.HasIndex(e => e.ParentContractId);
            });

            // ========== NEW: Payments Domain mappings ==========

            modelBuilder.Entity<PaymentCycle>(e =>
            {
                e.HasKey(x => x.CycleId);
                e.Property(x => x.Year).IsRequired();
                e.Property(x => x.Month).IsRequired();

                e.HasIndex(x => new { x.Year, x.Month }).IsUnique();
                e.Property(x => x.Closed).HasDefaultValue(false);

                e.HasMany(x => x.RoomCharges)
                  .WithOne()
                  .HasForeignKey(rc => rc.CycleId)
                  .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RoomCharge>(e =>
            {
                e.HasKey(x => x.RoomChargeId);
                e.Property(x => x.CycleId).IsRequired();
                e.Property(x => x.RoomCode).IsRequired().HasMaxLength(64);

                e.Property(x => x.BaseRent).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                e.Property(x => x.UtilityFeesTotal).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                e.Property(x => x.CustomFeesTotal).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                e.Property(x => x.ElectricAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                e.Property(x => x.WaterAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                e.Property(x => x.AmountPaid).HasColumnType("decimal(18,2)").HasDefaultValue(0);

                e.Property(x => x.Status).HasConversion<int>().HasDefaultValue(PaymentStatus.MissingData);

                e.HasIndex(x => new { x.CycleId, x.RoomCode }).IsUnique();

                // Fees 1-N
                e.HasMany(x => x.Fees)
                  .WithOne()
                  .HasForeignKey(f => f.RoomChargeId)
                  .OnDelete(DeleteBehavior.Cascade);

                // Payments 1-N
                e.HasMany(x => x.Payments)
                  .WithOne()
                  .HasForeignKey(p => p.RoomChargeId)
                  .OnDelete(DeleteBehavior.Cascade);

                // OwnsOne Electric/Water readings to avoid extra tables
                e.OwnsOne(x => x.ElectricReading, builder =>
                {
                    // If your ElectricReading has Id/RoomChargeId, ignore them for owned type
                    builder.Ignore(p => p.ElectricReadingId);
                    builder.Ignore(p => p.RoomChargeId);
                    builder.Property(p => p.Previous).HasColumnName("ElectricPrev");
                    builder.Property(p => p.Current).HasColumnName("ElectricCur");
                    builder.Property(p => p.Rate).HasColumnName("ElectricRate").HasColumnType("decimal(18,2)").HasDefaultValue(0);
                    builder.Property(p => p.Confirmed).HasColumnName("ElectricConfirmed").HasDefaultValue(false);
                });

                e.OwnsOne(x => x.WaterReading, builder =>
                {
                    builder.Ignore(p => p.WaterReadingId);
                    builder.Ignore(p => p.RoomChargeId);
                    builder.Property(p => p.Previous).HasColumnName("WaterPrev");
                    builder.Property(p => p.Current).HasColumnName("WaterCur");
                    builder.Property(p => p.Rate).HasColumnName("WaterRate").HasColumnType("decimal(18,2)").HasDefaultValue(0);
                    builder.Property(p => p.Confirmed).HasColumnName("WaterConfirmed").HasDefaultValue(false);
                });

                // CHECKS
                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_RoomCharge_BaseRent_NonNegative", "[BaseRent] >= 0");
                    t.HasCheckConstraint("CK_RoomCharge_Amounts_NonNegative", "[UtilityFeesTotal] >= 0 AND [CustomFeesTotal] >= 0 AND [ElectricAmount] >= 0 AND [WaterAmount] >= 0 AND [AmountPaid] >= 0");
                });
            });

            modelBuilder.Entity<FeeType>(e =>
            {
                e.HasKey(x => x.FeeTypeId);
                e.Property(x => x.Name).IsRequired().HasMaxLength(128);
                e.Property(x => x.UnitLabel).HasMaxLength(32).IsRequired(false);
                e.Property(x => x.DefaultRate).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                e.Property(x => x.IsRecurring).HasDefaultValue(true);
                e.Property(x => x.ApplyAllRooms).HasDefaultValue(false);
                e.Property(x => x.Active).HasDefaultValue(true);
                e.HasIndex(x => x.Name);
            });

            modelBuilder.Entity<FeeInstance>(e =>
            {
                e.HasKey(x => x.FeeInstanceId);
                e.Property(x => x.RoomChargeId).IsRequired();
                e.Property(x => x.FeeTypeId).IsRequired();
                e.Property(x => x.Name).IsRequired().HasMaxLength(128);
                e.Property(x => x.Rate).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                e.Property(x => x.Quantity).HasColumnType("decimal(18,2)").HasDefaultValue(1);
                e.ToTable(t => t.HasCheckConstraint("CK_FeeInstance_NonNegative", "[Rate] >= 0 AND [Quantity] >= 0"));
            });

            modelBuilder.Entity<PaymentRecord>(e =>
            {
                e.HasKey(x => x.PaymentRecordId);
                e.Property(x => x.RoomChargeId).IsRequired();
                e.Property(x => x.Amount).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                e.Property(x => x.PaidAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                e.Property(x => x.IsPartial).HasDefaultValue(false);
                e.ToTable(t => t.HasCheckConstraint("CK_PaymentRecord_Amount_Positive", "[Amount] > 0"));
            });
        }
    }
}