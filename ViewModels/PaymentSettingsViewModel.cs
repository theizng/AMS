using AMS.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace AMS.ViewModels
{
    public partial class PaymentSettingsViewModel : ObservableObject
    {
        private readonly IPaymentsRepository _repo;
        //private readonly ISheetReader _sheetReader;

        [ObservableProperty] private int defaultDueDay;
        [ObservableProperty] private int graceDays;
        [ObservableProperty] private bool autoReminderEnabled;

        [ObservableProperty] private string spreadsheetId;
        [ObservableProperty] private string electricSheetName;
        [ObservableProperty] private string waterSheetName;
        [ObservableProperty] private string invoiceSheetName;
        [ObservableProperty] private string baseRange;

        [ObservableProperty] private decimal defaultElectricRate;
        [ObservableProperty] private decimal defaultWaterRate;
        [ObservableProperty] private decimal defaultInternetFlat;
        [ObservableProperty] private decimal defaultCleaningFlat;

        public IAsyncRelayCommand SaveSettingsCommand { get; }
        public IAsyncRelayCommand TestSheetsCommand { get; }

        public PaymentSettingsViewModel(IPaymentsRepository repo)
        {
            _repo = repo;
            //_sheetReader = sheetReader;

            SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
            TestSheetsCommand = new AsyncRelayCommand(TestSheetsAsync);
        }

        private async Task SaveSettingsAsync()
        {
            // TODO: persist settings
            await Task.CompletedTask;
        }

        private async Task TestSheetsAsync()
        {
            // TODO: try a ReadMeterAsync or ping Google API to validate
            await Task.CompletedTask;
        }
    }
}