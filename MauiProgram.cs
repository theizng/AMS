using AMS.Data;
using AMS.Services;
using AMS.ViewModels;
using AMS.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO;
using Microsoft.Maui;
using AMS.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using AMS.Converters;
using QuestPDF.Infrastructure;
using DocumentFormat.OpenXml.VariantTypes;
using Microcharts.Maui;
namespace AMS
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMicrocharts()
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
                    sqlite.MigrationsAssembly("AMS")));
            builder.Services.AddDbContextFactory<AMSDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}", sqlite =>
                    sqlite.MigrationsAssembly("AMS")));
            return builder;
        }
        public static MauiAppBuilder RegisterServices(this MauiAppBuilder builder)
        {
            var scriptUrl = "https://script.google.com/macros/s/AKfycbxo0tnS474z74gRp6Xgpnbbws8UynNwgM81kgmAvI8n0Hc0OVhfjzoxc1wXrmKoVmDM/exec";
            var token = "PbR6tUEJDxdKVvheO7SCLb7IXufOVh1KlQQtGmm4l7294s9d3D6bgHueJ7xZOMqK";


            // Đăng ký các dịch vụ khác tại đây nếu cần
            //Đăng ký Services
            //used for pdf contract
#if WINDOWS || MACCATALYST || LINUX
            QuestPDF.Settings.License = LicenseType.Community;
            builder.Services.AddSingleton<IContractPdfService, ContractPdfService>();
#else
            builder.Services.AddSingleton<IContractPdfService, DisabledContractPdfService>();
#endif
            builder.Services.AddSingleton<IPdfCapabilityService, PdfCapabilityService>();


            builder.Services.AddSingleton<IContractAddendumService, ContractAddendumService>();
    
            
            //used for contract
            builder.Services.AddSingleton<IContractRoomGuard, ContractRoomGuard>();
            builder.Services.AddSingleton<IRoomStatusService, RoomStatusService>();
            builder.Services.AddSingleton<IContractsRepository, ContractsRepository>();
            builder.Services.AddScoped<TenantsNamesConverter>();
            builder.Services.AddScoped<IRoomOccupancyAdminService, RoomOccupancyAdminService>();

            //used for tenant
            builder.Services.AddSingleton<IRoomOccupancyProvider, RoomOccupancyEfProvider>();
            builder.Services.AddSingleton<IRoomsRepository, RoomsEfRepository>();
            builder.Services.AddSingleton<IRoomTenantQuery, RoomTenantQuery>();

            //used for mail
            builder.Services.AddSingleton<IEmailService, EmailService>();
            builder.Services.AddSingleton<IEmailNotificationService, EmailNotificationService>();
            builder.Services.AddSingleton<IRoomsProvider, RoomsEfProvider>();

            //used for payments
            builder.Services.AddScoped<IPaymentsRepository, PaymentsRepository>(); //CRUD Payments
            builder.Services.AddSingleton<IInvoiceScriptClient>(sp =>
            {
                var http = sp.GetRequiredService<HttpClient>();
                return new GoogleScriptInvoiceClient(http, scriptUrl, token);
            }); //Write data to template Invoices on Sheet
            builder.Services.AddTransient<IInvoiceGenerator, InvoiceGenerator>(); // Generate PDF Invoice from InvoiceScriptClient
            builder.Services.AddTransient<IPaymentSettingsProvider, PaymentSettingsProvider>(); //Provide Payment Settings data


            builder.Services.AddSingleton<HttpClient>();
            builder.Services.AddSingleton<IMeterSheetReader, ClosedXMLSimpleMeterSheetReader>(); //READ METERS SHEET AND LOAD FOR PaymentEntryMeterPage
            builder.Services.AddSingleton<IOnlineMeterSheetReader, GoogleSheetSimpleMeterReader>(); //READ METERS SHEET AND LOAD FOR PaymentEntryMeterPage
            builder.Services.AddSingleton<IOnlineMeterSheetWriter, GoogleScriptMeterWriter>(); //WRITE METERS SHEET AND LOAD FOR PaymentEntryMeterPage

            //builder.Services.AddSingleton<IMeterSheetWriter, ClosedXMLSimpleMeterSheetWriter>(); 
            builder.Services.AddSingleton<IOnlineMeterSheetWriter>(sp =>
            {
                var http = sp.GetRequiredService<HttpClient>();
                return new GoogleScriptMeterWriter (http, scriptUrl, token);

            }
            ); 
            builder.Services.AddSingleton<IMaintenanceSheetWriter>(sp =>
            {
                var http = sp.GetRequiredService<HttpClient>();
                return new GoogleAppScriptMaintenanceWriter(http, scriptUrl, token);

            });  //ONLINE WRITE SHEET STATUS FOR MaintenanceRequest
            builder.Services.AddSingleton<IOnlineMaintenanceReader, GoogleSheetXlsxMaintenanceReader>(); //READ MAINTENANCE SHEET AND LOAD FOR MaintenanceRequest
            builder.Services.AddSingleton<IMaintenanceSheetReader, ClosedXMLMaintenanceSheetReader>(); //READ MAINTENANCE SHEET AND LOAD FOR MaintenanceRequest

            //used for database sync
            builder.Services.AddSingleton<IDatabaseSyncService, DatabaseSyncService>();

            //used for auth
            builder.Services.AddSingleton<ISecureStorage>(SecureStorage.Default);
            builder.Services.AddSingleton<IAuthService, AuthService>();
            builder.Services.AddSingleton<IThemeService, ThemeService>();
            builder.Services.AddSingleton<IPlatformExportGuard, PlatformExportGuard>();
            return builder;
        }
        public static MauiAppBuilder RegisterViewModels(this MauiAppBuilder builder)
        {
            // Đăng ký các ViewModel khác tại đây nếu cần
            //Đăng ký ViewModels cho LOGIN
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<ForgotPasswordViewModel>();
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
            builder.Services.AddTransient<MaintenancesViewModel>();
            //Đăng ký Viewmodels cho Settings:
            builder.Services.AddTransient<SettingsViewModel>();
            //Đăng ký Viewmodels cho Payments:
            builder.Services.AddTransient<PaymentMeterEntryViewModel>();
            builder.Services.AddTransient<PaymentsViewModel>();
            builder.Services.AddTransient<PaymentInvoicesViewModel>();
            builder.Services.AddTransient<PaymentSettingsViewModel>();
            builder.Services.AddTransient<PaymentFeesViewModel>();
            //Đăng ký Viewmodels cho Contracts:
            builder.Services.AddTransient<ContractsViewModel>();
            builder.Services.AddTransient<ContractEditViewModel>();
            //Đăng ký Viewmodels cho Reports:
            builder.Services.AddTransient<ReportsViewModel>();
            builder.Services.AddTransient<ReportRevenueViewModel>();
            builder.Services.AddTransient<ReportUtilitiesViewModel>();
            builder.Services.AddTransient<ReportProfitsViewModel>();
            builder.Services.AddTransient<ReportDebtViewModel>();
            builder.Services.AddTransient<ReportRoomStatusSimpleViewModel>();
            return builder;
        }
        public static MauiAppBuilder RegisterPages(this MauiAppBuilder builder)
        {
            // Đăng ký các Page khác tại đây nếu cần
            //Đăng ký Pages cho Auth:
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<ForgotPasswordPage>();
            //Đăng ký Pages cho Main:
            builder.Services.AddTransient<MainPage>();
            //Đăng ký Pages cho House:
            builder.Services.AddTransient<HousesPage>();
            builder.Services.AddTransient<EditHousePage>();
            //Đăng ký Pages cho Tenants:
            builder.Services.AddTransient<TenantsPage>();
            //Đăng ký Pages cho Payments:
            builder.Services.AddTransient<PaymentsPage>();
            //Đăng ký Pages cho Reports:
            builder.Services.AddTransient<ReportsPage>();
            builder.Services.AddTransient<ReportRevenuePage>();
            builder.Services.AddTransient<ReportUtilitiesPage>();
            builder.Services.AddTransient<ReportDebtPage>();
            builder.Services.AddTransient<ReportProfitsPage>();
            builder.Services.AddTransient<ReportRoomStatusPage>();
            //Đăng ký Pages cho Room:
            builder.Services.AddTransient<RoomsPage>();
            builder.Services.AddTransient<EditRoomPage>();
            builder.Services.AddTransient<RoomDetailPage>();
            //Đăng ký Pages cho Tenants:
            builder.Services.AddTransient<TenantsPage>();
            builder.Services.AddTransient<EditTenantPage>();
            //Đăng ký Pages cho Maintenances:
            builder.Services.AddTransient<MaintenancesPage>();
            builder.Services.AddTransient<EditMaintenancePage>();
            //Đăng ký Pages cho Payments:
            builder.Services.AddTransient<PaymentsPage>();
            builder.Services.AddTransient<PaymentFeesPage>();
            builder.Services.AddTransient<PaymentInvoicesPage>();
            builder.Services.AddTransient<PaymentSettingsPage>();
            builder.Services.AddTransient<PaymentMeterEntryPage>();
            //Đăng ký Pages cho Contracts:
            builder.Services.AddTransient<ContractsPage>();
            builder.Services.AddTransient<EditContractPage>();
            //Đăng ký Pages cho Settings:
            builder.Services.AddTransient<SettingsPage>();
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
