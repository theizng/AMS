using AMS.Models;
using AMS.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace AMS.ViewModels
{
    public partial class PaymentSettingsViewModel : ObservableObject
    {
        private readonly IPaymentSettingsProvider _provider;

        // Banking
        [ObservableProperty] private string nameAccount = "";
        [ObservableProperty] private string bankAccount = "";
        [ObservableProperty] private string bankName = "";
        [ObservableProperty] private string branch = "";

        // Billing
        [ObservableProperty] private int defaultDueDay = 5;
        [ObservableProperty] private int graceDays = 0;

        // Optional defaults (keep if you still use them)
        [ObservableProperty] private decimal defaultElectricRate;
        [ObservableProperty] private decimal defaultWaterRate;
        [ObservableProperty] private decimal defaultInternetFlat;
        [ObservableProperty] private decimal defaultCleaningFlat;

        // Email templates
        [ObservableProperty] private string emailSubject = "Thông báo phí {month}/{year} - {room}";
        [ObservableProperty] private string emailBody = "Kính gửi {room}, hóa đơn (PDF đính kèm).";

        [ObservableProperty] private string status = "";

        public IAsyncRelayCommand LoadCommand { get; }
        public IAsyncRelayCommand SaveCommand { get; }

        public PaymentSettingsViewModel(IPaymentSettingsProvider provider)
        {
            _provider = provider;
            LoadCommand = new AsyncRelayCommand(LoadAsync);
            SaveCommand = new AsyncRelayCommand(SaveAsync);
        }

        public Task OnAppearAsync() => LoadAsync();

        public Task LoadAsync()
        {
            var s = _provider.Get();

            NameAccount = s.NameAccount ?? "";
            BankAccount = s.BankAccount ?? "";
            BankName = s.BankName ?? "";
            Branch = s.Branch ?? "";

            DefaultDueDay = s.DefaultDueDay;
            GraceDays = s.GraceDays;

            DefaultElectricRate = s.DefaultElectricRate;
            DefaultWaterRate = s.DefaultWaterRate;


            Status = "Đã tải cấu hình.";
            return Task.CompletedTask;
        }

        private async Task SaveAsync()
        {
            // Clamp due day (avoid invalid date)
            var due = DefaultDueDay;
            if (due < 1) due = 1;
            if (due > 28) due = 28;
            DefaultDueDay = due;

            var s = new PaymentSettings
            {
                NameAccount = NameAccount?.Trim() ?? "",
                BankAccount = BankAccount?.Trim() ?? "",
                BankName = BankName?.Trim() ?? "",
                Branch = Branch?.Trim() ?? "",

                DefaultDueDay = DefaultDueDay,
                GraceDays = GraceDays,

                DefaultElectricRate = DefaultElectricRate,
                DefaultWaterRate = DefaultWaterRate,

            };

            await _provider.SaveAsync(s);
            Status = "Đã lưu cấu hình.";
        }
    }
}