using System;
using System.Collections.Generic;
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
                if (FromDate > ToDate) ToDate = FromDate;

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

        // Export a readable text report saved with .pdf extension (mirrors UI: range + monthly profits + total)
        private async Task ExportPdfAsync()
        {
            if (IsBusy) return;
            if (ProfitByMonth.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync("Xuất PDF", "Không có dữ liệu để xuất.", "OK");
                return;
            }

            try
            {
                var folder = Path.Combine(FileSystem.AppDataDirectory, "reports");
                Directory.CreateDirectory(folder);

                var fileName = $"Profits_{FromDate:yyyyMM}-{ToDate:yyyyMM}.pdf";
                var path = Path.Combine(folder, fileName);

                var months = EnumerateMonths(FromDate, ToDate).ToList();
                var count = Math.Min(months.Count, ProfitByMonth.Count);

                var sb = new StringBuilder();
                sb.AppendLine($"BÁO CÁO LỢI NHUẬN");
                sb.AppendLine($"Khoảng thời gian: {FromDate:MM/yyyy} → {ToDate:MM/yyyy}");
                sb.AppendLine($"Tổng lợi nhuận: {TotalProfit:N0} đ");
                sb.AppendLine(new string('-', 60));
                sb.AppendLine($"{"Tháng",-12} {"Lợi nhuận",-20}");
                sb.AppendLine(new string('-', 60));

                for (int i = 0; i < count; i++)
                {
                    var dt = months[i];
                    var mv = ProfitByMonth[i];
                    sb.AppendLine($"{dt:MM/yyyy,-12} {mv.Profit, -20:N0}");
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

        // Export CSV (Excel-friendly) that mirrors the UI’s monthly profits and total
        private async Task ExportExcelAsync()
        {
            if (IsBusy) return;
            if (ProfitByMonth.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync("Xuất Excel", "Không có dữ liệu để xuất.", "OK");
                return;
            }

            try
            {
                var folder = Path.Combine(FileSystem.AppDataDirectory, "reports");
                Directory.CreateDirectory(folder);

                var fileName = $"Profits_{FromDate:yyyyMM}-{ToDate:yyyyMM}.csv";
                var path = Path.Combine(folder, fileName);

                var months = EnumerateMonths(FromDate, ToDate).ToList();
                var count = Math.Min(months.Count, ProfitByMonth.Count);

                var sb = new StringBuilder();
                sb.AppendLine($"# Báo cáo lợi nhuận");
                sb.AppendLine($"# Khoảng thời gian: {FromDate:MM/yyyy} → {ToDate:MM/yyyy}");
                sb.AppendLine($"# Tổng lợi nhuận: {TotalProfit:N0} đ");
                sb.AppendLine("Month,Profit");

                for (int i = 0; i < count; i++)
                {
                    var dt = months[i];
                    var mv = ProfitByMonth[i];
                    sb.AppendLine($"{dt:yyyy-MM},{mv.Profit:0.##}");
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

        private static IEnumerable<DateTime> EnumerateMonths(DateTime from, DateTime to)
        {
            var start = new DateTime(from.Year, from.Month, 1);
            var end = new DateTime(to.Year, to.Month, 1);
            for (var c = start; c <= end; c = c.AddMonths(1))
                yield return c;
        }
    }
}