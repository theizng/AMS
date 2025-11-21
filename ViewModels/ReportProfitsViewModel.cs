using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AMS.Models;
using AMS.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microcharts;
using AMS.Helpers;

namespace AMS.ViewModels
{
    public partial class ReportProfitsViewModel : ObservableObject
    {
        private readonly IPaymentsRepository _payments;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private DateTime fromDate = new(DateTime.Today.Year, 1, 1);
        [ObservableProperty] private DateTime toDate = DateTime.Today;

        [ObservableProperty] private decimal totalProfit;

        [ObservableProperty] private ObservableCollection<MonthlyValue> profitByMonth = new();
        [ObservableProperty] private Chart? profitChart;

        public IAsyncRelayCommand LoadCommand { get; }
        public IAsyncRelayCommand FilterCommand { get; }
        public IAsyncRelayCommand ExportPdfCommand { get; }
        public IAsyncRelayCommand ExportExcelCommand { get; }

        public ReportProfitsViewModel(IPaymentsRepository payments)
        {
            _payments = payments;
            LoadCommand = new AsyncRelayCommand(LoadAsync);
            FilterCommand = new AsyncRelayCommand(LoadAsync);
            ExportPdfCommand = new AsyncRelayCommand(ExportPdfAsync);
            ExportExcelCommand = new AsyncRelayCommand(ExportExcelAsync);
        }

        private async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                if (FromDate > ToDate) ToDate = FromDate; // basic guard

                ProfitByMonth.Clear();
                TotalProfit = 0m;

                var start = new DateTime(FromDate.Year, FromDate.Month, 1);
                var end = new DateTime(ToDate.Year, ToDate.Month, 1);

                for (var cursor = start; cursor <= end; cursor = cursor.AddMonths(1))
                {
                    var cycle = await _payments.GetCycleAsync(cursor.Year, cursor.Month);
                    if (cycle == null) continue;

                    var charges = await _payments.GetRoomChargesForCycleAsync(cycle.CycleId);

                    var revenue = charges.Sum(rc => rc.TotalDue);
                    var utilTotal = charges.Sum(rc => rc.UtilityFeesTotal);
                    var customTotal = charges.Sum(rc => rc.CustomFeesTotal);
                    var profit = revenue - utilTotal - customTotal;

                    ProfitByMonth.Add(new MonthlyValue
                    {
                        Month = cursor.Month,
                        Profit = profit
                    });

                    TotalProfit += profit;
                }

                ProfitChart = new BarChart
                {
                    Entries = ProfitByMonth.ToProfitEntries().ToList(),
                    LabelTextSize = 24,
                    Margin = 20
                };
            }
            finally
            {
                IsBusy = false;
            }
        }

        private Task ExportPdfAsync() => Task.CompletedTask;
        private Task ExportExcelAsync() => Task.CompletedTask;
    }
}