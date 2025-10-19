using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AMS.Data; // Đảm bảo namespace đúng

namespace AMS.Data
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AMSDbContext>();

            try
            {
                // Tạo DB + schema + seed nếu chưa tồn tại (an toàn cho production SQLite)
                bool created = db.Database.EnsureCreated();
                if (created)
                {
                    System.Diagnostics.Debug.WriteLine("✅ DB created and seeded successfully!");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ℹ️ DB already exists. No changes applied.");
                }

                // Verify seed data
                int adminCount = await db.Admin.CountAsync();
                int houseCount = await db.Houses.CountAsync();
                int roomCount = await db.Rooms.CountAsync();
                System.Diagnostics.Debug.WriteLine($"📊 Data verification: Admins={adminCount}, Houses={houseCount}, Rooms={roomCount}");

                if (adminCount == 0)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Warning: No Admin seeded! Check OnModelCreating.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ DB Init Error: {ex.Message}\nStack: {ex.StackTrace}");
                throw; // Re-throw để App xử lý
            }

#if DEBUG
            try
            {
                var conn = db.Database.GetDbConnection();
                System.Diagnostics.Debug.WriteLine($"📁 SQLite DB Path: {conn.DataSource}");
                // Test query đơn giản
                var firstAdmin = await db.Admin.FirstOrDefaultAsync();
                if (firstAdmin != null)
                {
                    System.Diagnostics.Debug.WriteLine($"👤 Seeded Admin: {firstAdmin.Username} (ID: {firstAdmin.AdminId})");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ DB Debug Error: {ex.Message}");
            }
#endif
        }
    }
}