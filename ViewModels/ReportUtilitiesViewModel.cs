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
    public partial class ReportUtilitiesViewModel : ObservableObject
    {
        private readonly IPaymentsRepository _payments;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private int month = DateTime.Today.Month;
        [ObservableProperty] private int year = DateTime.Today.Year;

        [ObservableProperty] private decimal totalElectric;
        [ObservableProperty] private decimal totalWater;
        [ObservableProperty] private decimal totalRepairs;

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

                UtilitiesChart = new BarChart
                {
                    Entries = ChartHelper.BuildUtilitiesTripleEntries(UtilitiesByMonth).ToList(),
                    LabelTextSize = 22,
                    Margin = 20
                };
            }
            finally { IsBusy = false; }
        }

        private async Task ExportPdfAsync()
        {
            if (IsBusy) return;
            if (UtilitiesByMonth.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync("Xuất PDF", "Không có dữ liệu để xuất.", "OK");
                return;
            }
            try
            {
                var folder = Path.Combine(FileSystem.AppDataDirectory, "reports");
                Directory.CreateDirectory(folder);
                var fileName = $"Utilities_{Year}.pdf";
                var path = Path.Combine(folder, fileName);

                var sb = new StringBuilder();
                sb.AppendLine("BÁO CÁO CHI PHÍ TIỆN ÍCH");
                sb.AppendLine($"Năm: {Year}  | Tháng đang chọn: {Month:00}/{Year}");
                sb.AppendLine($"Tổng Điện (tháng chọn): {TotalElectric:N0} đ");
                sb.AppendLine($"Tổng Nước (tháng chọn): {TotalWater:N0} đ");
                sb.AppendLine($"Tổng Phí chung (tháng chọn): {TotalRepairs:N0} đ");
                sb.AppendLine(new string('-', 70));
                sb.AppendLine($"{"Tháng",-10} {"Điện",-15} {"Nước",-15} {"Phí chung",-15}");
                sb.AppendLine(new string('-', 70));
                foreach (var mv in UtilitiesByMonth.OrderBy(x => x.Month))
                {
                    sb.AppendLine($"{mv.Month:00}/{Year,-10} {mv.Utilities1,-15:N0} {mv.Utilities2,-15:N0} {mv.GeneralFees,-15:N0}");
                }
                var yearElec = UtilitiesByMonth.Sum(x => x.Utilities1);
                var yearWater = UtilitiesByMonth.Sum(x => x.Utilities2);
                var yearGen = UtilitiesByMonth.Sum(x => x.GeneralFees);
                sb.AppendLine(new string('-', 70));
                sb.AppendLine($"Tổng năm Điện: {yearElec:N0} đ | Nước: {yearWater:N0} đ | Phí chung: {yearGen:N0} đ");

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
            if (UtilitiesByMonth.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync("Xuất Excel", "Không có dữ liệu để xuất.", "OK");
                return;
            }
            try
            {
                var folder = Path.Combine(FileSystem.AppDataDirectory, "reports");
                Directory.CreateDirectory(folder);
                var fileName = $"Utilities_{Year}.csv";
                var path = Path.Combine(folder, fileName);

                var sb = new StringBuilder();
                sb.AppendLine("# Báo cáo tiện ích");
                sb.AppendLine($"# Năm: {Year} | Tháng chọn: {Month:00}/{Year}");
                sb.AppendLine($"# Tổng Điện tháng chọn: {TotalElectric:N0} đ");
                sb.AppendLine($"# Tổng Nước tháng chọn: {TotalWater:N0} đ");
                sb.AppendLine($"# Tổng Phí chung tháng chọn: {TotalRepairs:N0} đ");
                sb.AppendLine("Month,Electric,Water,GeneralFees");
                foreach (var mv in UtilitiesByMonth.OrderBy(x => x.Month))
                    sb.AppendLine($"{Year}-{mv.Month:00},{mv.Utilities1:0.##},{mv.Utilities2:0.##},{mv.GeneralFees:0.##}");

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
}