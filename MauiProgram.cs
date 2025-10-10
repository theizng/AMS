using Microsoft.Extensions.Logging;
using AMS.Services;     // Cho IAuthService và AuthService
using AMS.ViewModels;   // Cho LoginViewModel
using AMS.Views;        // Cho LoginPage

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
            //Đăng ký Services
            builder.Services.AddSingleton<IAuthService, AuthService>();
            //Đăng ký ViewModels
            builder.Services.AddTransient<LoginViewModel>(); 
            //Đăng ký Pages
            builder.Services.AddTransient<LoginPage>();
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
