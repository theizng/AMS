using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AMS.Helpers;
using AMS.Models;
using AMS.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microcharts;

namespace AMS.ViewModels
{
    public partial class ReportsViewModel : ObservableObject
    {
        private readonly IPaymentsRepository _payments;
        private readonly IRoomsRepository _rooms;
        private readonly IRoomOccupancyProvider _occupancy;

        [ObservableProperty] private bool isBusy;

        [ObservableProperty] private ObservableCollection<MonthlyValue> revenueProfitByMonth = new();

        [ObservableProperty] private Chart? revenueProfitChart;
        [ObservableProperty] private Chart? paidUnpaidPie;

        [ObservableProperty] private decimal currentMonthRevenue;
        [ObservableProperty] private decimal currentMonthProfit;
        [ObservableProperty] private int currentTenantCount;

        [ObservableProperty] private int paidCount;
        [ObservableProperty] private int unpaidCount;

        [ObservableProperty] private ObservableCollection<DebtRow> debts = new();

        public IAsyncRelayCommand LoadCommand { get; }
        public IAsyncRelayCommand ExportPdfCommand { get; }
        public IAsyncRelayCommand ExportExcelCommand { get; }

        public ReportsViewModel(
            IPaymentsRepository payments,
            IRoomsRepository rooms,
            IRoomOccupancyProvider occupancy)
        {
            _payments = payments;
            _rooms = rooms;
            _occupancy = occupancy;

            LoadCommand = new AsyncRelayCommand(LoadAsync);
            ExportPdfCommand = new AsyncRelayCommand(ExportPdfAsync);
            ExportExcelCommand = new AsyncRelayCommand(ExportExcelAsync);
        }

        public async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var today = DateTime.Today;
                var thisYear = today.Year;

                // Current cycle summary
                var currentCycle = await _payments.GetCycleAsync(today.Year, today.Month)
                                  ?? await _payments.CreateCycleAsync(today.Year, today.Month);
                var chargesCurrent = await _payments.GetRoomChargesForCycleAsync(currentCycle.CycleId);

                CurrentMonthRevenue = chargesCurrent.Sum(rc => rc.TotalDue);
                var utilCurr = chargesCurrent.Sum(rc => rc.UtilityFeesTotal);
                var customCurr = chargesCurrent.Sum(rc => rc.CustomFeesTotal);
                CurrentMonthProfit = CurrentMonthRevenue - utilCurr - customCurr;

                // Tenant count
                var allRooms = await _rooms.GetAllAsync(includeInactive: false);
                var tenantCount = 0;
                foreach (var r in allRooms)
                {
                    var tenants = await _occupancy.GetTenantsForRoomAsync(r.RoomCode);
                    tenantCount += tenants.Count;
                }
                CurrentTenantCount = tenantCount;

                // Build year-to-date months
                RevenueProfitByMonth.Clear();
                for (int m = 1; m <= 12; m++)
                {
                    var c = await _payments.GetCycleAsync(thisYear, m);
                    if (c == null) continue;
                    var charges = await _payments.GetRoomChargesForCycleAsync(c.CycleId);
                    var rev = charges.Sum(rc => rc.TotalDue);
                    var util = charges.Sum(rc => rc.UtilityFeesTotal);
                    var custom = charges.Sum(rc => rc.CustomFeesTotal);
                    var prof = rev - util - custom;

                    RevenueProfitByMonth.Add(new MonthlyValue
                    {
                        Month = m,
                        Revenue = rev,
                        Profit = prof
                    });
                }

                // Paid/Unpaid for current month
                PaidCount = chargesCurrent.Count(rc => rc.Status == PaymentStatus.Paid);
                UnpaidCount = chargesCurrent.Count(rc => rc.Status != PaymentStatus.Paid);
                PaidUnpaidPie = new DonutChart
                {
                    Entries = ChartHelper.BuildPaidUnpaidEntries(PaidCount, UnpaidCount).ToList(),
                    HoleRadius = 0.5f,
                    LabelTextSize = 28
                };

                // Debts for current month (only RoomCode + AmountRemaining)
                Debts = new ObservableCollection<DebtRow>(
                    chargesCurrent
                        .Where(rc => rc.AmountRemaining > 0)
                        .OrderByDescending(rc => rc.AmountRemaining)
                        .Select(rc => new DebtRow(rc)));

                // Double-bar chart: interleave Doanh thu and Lợi nhuận
                RevenueProfitChart = new BarChart
                {
                    Entries = ChartHelper.BuildRevenueProfitPairedEntries(RevenueProfitByMonth).ToList(),
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

    //CÁC DÒNG DANH SÁCH NỢ
    public class DebtRow
    {
        public RoomCharge Source { get; }
        public string RoomCode => Source.RoomCode;
        public decimal AmountRemaining => Source.AmountRemaining;
        public DebtRow(RoomCharge rc) => Source = rc;
    }
}