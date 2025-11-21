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
    public partial class ReportUtilitiesViewModel : ObservableObject
    {
        private readonly IPaymentsRepository _payments;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private int month = DateTime.Today.Month;
        [ObservableProperty] private int year = DateTime.Today.Year;

        [ObservableProperty] private decimal totalElectric;
        [ObservableProperty] private decimal totalWater;
        [ObservableProperty] private decimal totalRepairs; // tổng FeeInstance (phí chung)

        [ObservableProperty] private ObservableCollection<MonthlyValue> utilitiesByMonth = new();

        [ObservableProperty] private Chart? utilitiesChart;

        public IAsyncRelayCommand LoadCommand { get; }
        public IAsyncRelayCommand ExportPdfCommand { get; }
        public IAsyncRelayCommand ExportExcelCommand { get; }

        public ReportUtilitiesViewModel(IPaymentsRepository payments)
        {
            _payments = payments;
            LoadCommand = new AsyncRelayCommand(LoadAsync);
            ExportPdfCommand = new AsyncRelayCommand(ExportPdfAsync);
            ExportExcelCommand = new AsyncRelayCommand(ExportExcelAsync);
        }

        private async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                // Totals for selected month/year
                var cycle = await _payments.GetCycleAsync(Year, Month);
                if (cycle != null)
                {
                    var charges = await _payments.GetRoomChargesForCycleAsync(cycle.CycleId);
                    TotalElectric = charges.Sum(rc => rc.ElectricAmount);
                    TotalWater = charges.Sum(rc => rc.WaterAmount);
                    TotalRepairs = charges.Sum(rc => rc.Fees?.Sum(f => f.Amount) ?? 0);
                }
                else
                {
                    TotalElectric = TotalWater = TotalRepairs = 0;
                }

                // Monthly breakdown for selected year
                UtilitiesByMonth.Clear();
                for (int m = 1; m <= 12; m++)
                {
                    var c = await _payments.GetCycleAsync(Year, m);
                    if (c == null) continue;
                    var rs = await _payments.GetRoomChargesForCycleAsync(c.CycleId);
                    var elec = rs.Sum(rc => rc.ElectricAmount);
                    var water = rs.Sum(rc => rc.WaterAmount);
                    var general = rs.Sum(rc => rc.Fees?.Sum(f => f.Amount) ?? 0);
                    UtilitiesByMonth.Add(new MonthlyValue
                    {
                        Month = m,
                        Utilities1 = elec,
                        Utilities2 = water,
                        GeneralFees = general
                    });
                }

                // Combined triple bar chart (Điện, Nước, Phí chung)
                UtilitiesChart = new BarChart
                {
                    Entries = ChartHelper.BuildUtilitiesTripleEntries(UtilitiesByMonth).ToList(),
                    LabelTextSize = 22,
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