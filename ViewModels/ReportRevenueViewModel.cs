using AMS.Helpers;
using AMS.Models;
using AMS.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microcharts;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace AMS.ViewModels
{
    public partial class ReportRevenueViewModel : ObservableObject
    {
        private readonly IPaymentsRepository _payments;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private DateTime fromDate = new(DateTime.Today.Year, 1, 1);
        [ObservableProperty] private DateTime toDate = DateTime.Today;

        [ObservableProperty] private decimal totalRevenue;
        [ObservableProperty] private decimal unpaidRevenue;

        [ObservableProperty] private ObservableCollection<MonthlyValue> revenueByMonth = new();

        [ObservableProperty] private Chart? revenueChart;

        public IAsyncRelayCommand LoadCommand { get; }
        public IAsyncRelayCommand FilterCommand { get; }
        public IAsyncRelayCommand ExportPdfCommand { get; }
        public IAsyncRelayCommand ExportExcelCommand { get; }

        public ReportRevenueViewModel(IPaymentsRepository payments)
        {
            _payments = payments;
            LoadCommand = new AsyncRelayCommand(LoadAsync);
            FilterCommand = new AsyncRelayCommand(LoadAsync);
            ExportPdfCommand = new AsyncRelayCommand(ExportPdfAsync);
            ExportExcelCommand = new AsyncRelayCommand(ExportExcelAsync);
        }

        public async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                if (FromDate > ToDate) ToDate = FromDate;

                RevenueByMonth.Clear();
                decimal total = 0m;
                decimal unpaid = 0m;

                var start = new DateTime(FromDate.Year, FromDate.Month, 1);
                var end = new DateTime(ToDate.Year, ToDate.Month, 1);
                for (var d = start; d <= end; d = d.AddMonths(1))
                {
                    var cycle = await _payments.GetCycleAsync(d.Year, d.Month);
                    if (cycle == null) continue;

                    var charges = await _payments.GetRoomChargesForCycleAsync(cycle.CycleId);
                    var rev = charges.Sum(rc => rc.TotalDue);
                    var rem = charges.Sum(rc => rc.AmountRemaining);

                    RevenueByMonth.Add(new MonthlyValue { Month = d.Month, Revenue = rev });
                    total += rev;
                    unpaid += rem;
                }

                TotalRevenue = total;
                UnpaidRevenue = unpaid;

                RevenueChart = new BarChart
                {
                    Entries = RevenueByMonth.ToRevenueEntries().ToList(),
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