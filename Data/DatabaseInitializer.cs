using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AMS.Data
{
    public static class DatabaseInitializer
    {
        public static void Initialize(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AMSDbContext>();

            System.Diagnostics.Debug.WriteLine($"🔵 [DataBase] Đường dẫn kết nối database: {db.Database.GetDbConnection().DataSource}");
            var pending = db.Database.GetPendingMigrations().ToList();
            if (pending.Count > 0)
                System.Diagnostics.Debug.WriteLine($"🔵 [DataBase] Áp dụng mô hình database: {string.Join(", ", pending)}");

            db.Database.Migrate();

            var applied = db.Database.GetAppliedMigrations().ToList();
            System.Diagnostics.Debug.WriteLine($"🔵 [DB] Đã áp dụng mô hình mới ({applied.Count}): {string.Join(", ", applied)}");
        }
    }
}