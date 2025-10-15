using AMS.Data;
using AMS.Services;
using AMS.ViewModels;
using AMS.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO;

namespace AMS
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });


            // Đường dẫn tới file SQLite
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "ams.db");
            // Đăng ký DbContext
            builder.Services.AddDbContext<AMSDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            //Đăng ký Services
            builder.Services.AddSingleton<ISecureStorage>(SecureStorage.Default);
            builder.Services.AddSingleton<IAuthService, AuthService>();

            //Đăng ký ViewModels
            builder.Services.AddTransient<LoginViewModel>(); 
            builder.Services.AddTransient<MainPageViewModel>();
            //Đăng ký Viewmodels CRUD:
            builder.Services.AddTransient<HousesViewModel>();
            builder.Services.AddTransient<HouseEditViewModel>();
            //Đăng ký Pages
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<MainPage>();
            //Đăng ký Trang CRUD cho các Entity
            builder.Services.AddTransient<HousesPage>();
            builder.Services.AddTransient<EditHousePage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
