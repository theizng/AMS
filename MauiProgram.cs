using AMS.Data;
using AMS.Services;
using AMS.ViewModels;
using AMS.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO;
using Microsoft.Maui;
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
            RegisterDataBase(builder);
            RegisterServices(builder);
            RegisterViewModels(builder);
            RegisterPages(builder);
            RegisterShells(builder);
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        public static MauiAppBuilder RegisterDataBase(this MauiAppBuilder builder)
        {
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "ams.db");
            builder.Services.AddDbContext<AMSDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}", sqlite =>
                {
                    sqlite.MigrationsAssembly("AMS"); // migrations live in the MAUI project
                }));
            return builder;
        }
        public static MauiAppBuilder RegisterServices(this MauiAppBuilder builder)
        {
            // Đăng ký các dịch vụ khác tại đây nếu cần
            //Đăng ký Services
            builder.Services.AddSingleton<IDatabaseSyncService, DatabaseSyncService>();
            builder.Services.AddSingleton<ISecureStorage>(SecureStorage.Default);
            builder.Services.AddSingleton<IAuthService, AuthService>();
            builder.Services.AddSingleton<IThemeService, ThemeService>();
            return builder;
        }
        public static MauiAppBuilder RegisterViewModels(this MauiAppBuilder builder)
        {
            // Đăng ký các ViewModel khác tại đây nếu cần
            //Đăng ký ViewModels
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<MainPageViewModel>();
            //Đăng ký Viewmodels CRUD House:
            builder.Services.AddTransient<HousesViewModel>();
            builder.Services.AddTransient<HouseEditViewModel>();
            //Đăng ký Viewmodels CRUD Room:
            builder.Services.AddTransient<RoomsViewModel>();
            builder.Services.AddTransient<RoomEditViewModel>();
            builder.Services.AddTransient<RoomDetailViewModel>();
            //Đăng ký Viewmodels CRUD cho Tenant:
            builder.Services.AddTransient<TenantsViewModel>();
            builder.Services.AddTransient<TenantEditViewModel>();
            //Đăng ký Viewmodels CRUD cho Maintenance:

            //Đăng ký Viewmodels cho Settings:
            builder.Services.AddTransient<SettingsViewModel>();
            return builder;
        }
        public static MauiAppBuilder RegisterPages(this MauiAppBuilder builder)
        {
            // Đăng ký các Page khác tại đây nếu cần
            //Đăng ký Pages
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<MainPage>();
            //Đăng ký Pages CRUD cho các Entity
            builder.Services.AddTransient<HousesPage>();
            builder.Services.AddTransient<EditHousePage>();
            builder.Services.AddTransient<TenantsPage>();
            builder.Services.AddTransient<PaymentsPage>();
            builder.Services.AddTransient<ReportsPage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<RoomsPage>();
            builder.Services.AddTransient<EditRoomPage>();
            builder.Services.AddTransient<RoomDetailPage>();
            builder.Services.AddTransient<EditTenantPage>();
            builder.Services.AddTransient<TenantsPage>();
            builder.Services.AddTransient<MaintenancesPage>();
            builder.Services.AddTransient<EditMaintenancePage>();
            return builder;
        }
        public static MauiAppBuilder RegisterShells(this MauiAppBuilder builder)
        {
            // Đăng ký các Shell khác tại đây nếu cần
            //Đăng ký Shells
            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddSingleton<LoginShell>();
            return builder;
        }
    }
}
