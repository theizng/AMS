using AMS.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace AMS.Data
{
    public class AMSDbContext : DbContext
    {
        public DbSet<Admin> Admin { get; set; }
        public DbSet<NguoiThue> NguoiThues { get; set; }
        //public DbSet<Phong>
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

            // Cấu hình cho NguoiThue
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