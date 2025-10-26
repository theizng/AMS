using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.IO;

namespace AMS.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AMSDbContext>
    {
        public AMSDbContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<AMSDbContext>();

            // Design-time DB path (only for scaffolding); runtime uses FileSystem.AppDataDirectory.
            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "ams.design.db");
            options.UseSqlite($"Data Source={dbPath}");

            return new AMSDbContext(options.Options);
        }
    }
}