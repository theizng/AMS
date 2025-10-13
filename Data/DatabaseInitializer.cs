namespace AMS.Data
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            using (var scope = services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AMSDbContext>();

                // Tạo database nếu chưa tồn tại
                await dbContext.Database.EnsureCreatedAsync();
            }
        }
    }
}