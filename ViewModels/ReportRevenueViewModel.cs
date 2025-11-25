using AMS.Helpers;
using AMS.Models;
using AMS.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microcharts;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
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

        private async Task ExportPdfAsync()
        {
            if (IsBusy) return;
            if (RevenueByMonth.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync("Xuất PDF", "Không có dữ liệu để xuất.", "OK");
                return;
            }

            try
            {
                var folder = Path.Combine(FileSystem.AppDataDirectory, "reports");
                Directory.CreateDirectory(folder);

                var fileName = $"Revenue_{FromDate:yyyyMM}-{ToDate:yyyyMM}.pdf";
                var path = Path.Combine(folder, fileName);

                var months = EnumerateMonths(FromDate, ToDate).ToList();
                var count = Math.Min(months.Count, RevenueByMonth.Count);

                var sb = new StringBuilder();
                sb.AppendLine("BÁO CÁO DOANH THU");
                sb.AppendLine($"Khoảng thời gian: {FromDate:MM/yyyy} → {ToDate:MM/yyyy}");
                sb.AppendLine($"Tổng doanh thu: {TotalRevenue:N0} đ");
                sb.AppendLine($"Doanh thu chưa thu: {UnpaidRevenue:N0} đ");
                sb.AppendLine(new string('-', 60));
                sb.AppendLine($"{"Tháng",-12} {"Doanh thu",-20}");
                sb.AppendLine(new string('-', 60));

                for (int i = 0; i < count; i++)
                {
                    var dt = months[i];
                    var mv = RevenueByMonth[i];
                    sb.AppendLine($"{dt:MM/yyyy,-12} {mv.Revenue,-20:N0}");
                }

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
            if (RevenueByMonth.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync("Xuất Excel", "Không có dữ liệu để xuất.", "OK");
                return;
            }

            try
            {
                var folder = Path.Combine(FileSystem.AppDataDirectory, "reports");
                Directory.CreateDirectory(folder);

                var fileName = $"Revenue_{FromDate:yyyyMM}-{ToDate:yyyyMM}.csv";
                var path = Path.Combine(folder, fileName);

                var months = EnumerateMonths(FromDate, ToDate).ToList();
                var count = Math.Min(months.Count, RevenueByMonth.Count);

                var sb = new StringBuilder();
                sb.AppendLine("# Báo cáo doanh thu");
                sb.AppendLine($"# Khoảng thời gian: {FromDate:MM/yyyy} → {ToDate:MM/yyyy}");
                sb.AppendLine($"# Tổng doanh thu: {TotalRevenue:N0} đ");
                sb.AppendLine($"# Doanh thu chưa thu: {UnpaidRevenue:N0} đ");
                sb.AppendLine("Month,Revenue");

                for (int i = 0; i < count; i++)
                {
                    var dt = months[i];
                    var mv = RevenueByMonth[i];
                    sb.AppendLine($"{dt:yyyy-MM},{mv.Revenue:0.##}");
                }

                await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
                await Shell.Current.DisplayAlertAsync("Đã xuất Excel", $"Đã lưu: {fileName}\nThư mục: {folder}", "OK");
                try { await Launcher.OpenAsync(new OpenFileRequest(fileName, new ReadOnlyFile(path))); } catch { }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi xuất Excel", ex.Message, "OK");
            }
        }

        private static System.Collections.Generic.IEnumerable<DateTime> EnumerateMonths(DateTime from, DateTime to)
        {
            var start = new DateTime(from.Year, from.Month, 1);
            var end = new DateTime(to.Year, to.Month, 1);
            for (var c = start; c <= end; c = c.AddMonths(1))
                yield return c;
        }
    }
}