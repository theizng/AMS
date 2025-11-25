using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AMS.Helpers;
using AMS.Models;
using AMS.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microcharts;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

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

                // Load chu kỳ thanh toán hiện tại -> Là tháng hiện tại truy cập app, nếu không có -> Tạo mới.
                var currentCycle = await _payments.GetCycleAsync(today.Year, today.Month)
                                  ?? await _payments.CreateCycleAsync(today.Year, today.Month);
                //Load đối tượng tình trạng thanh toán của phòng -> Là tình trạng thanh toán thuộc chu kỳ hiện tại.
                var chargesCurrent = await _payments.GetRoomChargesForCycleAsync(currentCycle.CycleId);

                //Doanh thu = Tổng
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

        private async Task ExportPdfAsync()
        {
            if (IsBusy) return;

            try
            {
                var folder = Path.Combine(FileSystem.AppDataDirectory, "reports");
                Directory.CreateDirectory(folder);

                var today = DateTime.Today;
                var fileName = $"Overview_{today:yyyyMM}.pdf";
                var path = Path.Combine(folder, fileName);

                var sb = new StringBuilder();
                sb.AppendLine("BÁO CÁO TỔNG QUAN");
                sb.AppendLine($"Tháng hiện tại: {today:MM/yyyy}");
                sb.AppendLine($"Doanh thu tháng này: {CurrentMonthRevenue:N0} đ");
                sb.AppendLine($"Lợi nhuận tháng này: {CurrentMonthProfit:N0} đ");
                sb.AppendLine($"Số người thuê hiện tại: {CurrentTenantCount}");
                sb.AppendLine();
                sb.AppendLine("THANH TOÁN THÁNG NÀY");
                sb.AppendLine($"Đã trả: {PaidCount} | Chưa trả: {UnpaidCount}");

                sb.AppendLine();
                sb.AppendLine("DANH SÁCH PHÒNG NỢ");
                sb.AppendLine($"{"Phòng",-12} {"Còn nợ",-16}");
                sb.AppendLine(new string('-', 40));
                if (Debts.Count == 0)
                {
                    sb.AppendLine("(Không có)");
                }
                else
                {
                    foreach (var d in Debts.OrderByDescending(x => x.AmountRemaining))
                        sb.AppendLine($"{d.RoomCode,-12} {d.AmountRemaining,-16:N0}");
                }
                sb.AppendLine();
                sb.AppendLine("DOANH THU & LỢI NHUẬN NĂM NAY");
                sb.AppendLine($"{"Tháng",-10} {"Doanh thu",-16} {"Lợi nhuận",-16}");
                sb.AppendLine(new string('-', 60));
                var yearRev = 0m;
                var yearProf = 0m;
                foreach (var mv in RevenueProfitByMonth.OrderBy(x => x.Month))
                {
                    yearRev += mv.Revenue;
                    yearProf += mv.Profit;
                    sb.AppendLine($"{mv.Month:00}/{today.Year,-10} {mv.Revenue,-16:N0} {mv.Profit,-16:N0}");
                }
                sb.AppendLine(new string('-', 60));
                sb.AppendLine($"Tổng năm: {yearRev:N0} đ | Lợi nhuận năm: {yearProf:N0} đ");

                await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
                await Shell.Current.DisplayAlertAsync("Đã xuất PDF", $"Đã lưu: {fileName}\nThư mục: {folder}", "OK");
                try { await Launcher.OpenAsync(new OpenFileRequest(fileName, new ReadOnlyFile(path))); } catch { }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi xuất PDF", ex.Message, "OK");
            }
        }

        private async Task ExportExcelAsync()
        {
            if (IsBusy) return;

            try
            {
                var folder = Path.Combine(FileSystem.AppDataDirectory, "reports");
                Directory.CreateDirectory(folder);

                var today = DateTime.Today;
                var fileName = $"Overview_{today:yyyyMM}.csv";
                var path = Path.Combine(folder, fileName);

                var sb = new StringBuilder();

                // Section: Summary
                sb.AppendLine("# Tổng quan");
                sb.AppendLine($"# Tháng hiện tại: {today:MM/yyyy}");
                sb.AppendLine($"# Doanh thu tháng này: {CurrentMonthRevenue:N0} đ");
                sb.AppendLine($"# Lợi nhuận tháng này: {CurrentMonthProfit:N0} đ");
                sb.AppendLine($"# Số người thuê hiện tại: {CurrentTenantCount}");
                sb.AppendLine();

                // Section: Paid/Unpaid
                sb.AppendLine("# Thanh toán tháng này");
                sb.AppendLine("Paid,Unpaid");
                sb.AppendLine($"{PaidCount},{UnpaidCount}");
                sb.AppendLine();

                // Section: Debts
                sb.AppendLine("# Danh sách phòng nợ");
                sb.AppendLine("RoomCode,AmountRemaining");
                if (Debts.Count == 0)
                {
                    sb.AppendLine(",");
                }
                else
                {
                    foreach (var d in Debts.OrderByDescending(x => x.AmountRemaining))
                        sb.AppendLine($"{d.RoomCode},{d.AmountRemaining:0.##}");
                }
                sb.AppendLine();

                // Section: Revenue & Profit by month
                sb.AppendLine("# Doanh thu & Lợi nhuận năm nay");
                sb.AppendLine("Month,Revenue,Profit");
                foreach (var mv in RevenueProfitByMonth.OrderBy(x => x.Month))
                    sb.AppendLine($"{today.Year}-{mv.Month:00},{mv.Revenue:0.##},{mv.Profit:0.##}");

                await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
                await Shell.Current.DisplayAlertAsync("Đã xuất Excel", $"Đã lưu: {fileName}\nThư mục: {folder}", "OK");
                try { await Launcher.OpenAsync(new OpenFileRequest(fileName, new ReadOnlyFile(path))); } catch { }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi xuất Excel", ex.Message, "OK");
            }
        }
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