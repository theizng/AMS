using AMS.Models;
using AMS.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    public partial class ReportDebtViewModel : ObservableObject
    {
        private readonly IPaymentsRepository _payments;
        private readonly IRoomTenantQuery _roomTenantQuery;
        private readonly IEmailNotificationService _email;
        private readonly IContractsRepository _contracts;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private int month = DateTime.Today.Month;
        [ObservableProperty] private int year = DateTime.Today.Year;

        [ObservableProperty] private string selectedStatusFilter = "Tất cả";
        [ObservableProperty] private ObservableCollection<RoomStatusRow> rows = new();

        [ObservableProperty] private int totalRooms;
        [ObservableProperty] private int debtRooms;
        [ObservableProperty] private int paidRooms;
        [ObservableProperty] private int lateRooms;

        public IReadOnlyList<string> StatusFilterOptions { get; } = new[] {
            "Tất cả",
            "Chưa trả",
            "Đã trả một phần",
            "Đã trả đủ",
            "Trễ hạn",
            "Đã đóng"
        };

        public IAsyncRelayCommand LoadCommand { get; }
        public IAsyncRelayCommand<RoomStatusRow> RemindCommand { get; }
        public IAsyncRelayCommand ExportPdfCommand { get; }
        public IAsyncRelayCommand ExportExcelCommand { get; }

        public ReportDebtViewModel(IPaymentsRepository payments,
                                   IRoomTenantQuery roomTenantQuery,
                                   IEmailNotificationService email,
                                   IContractsRepository contracts)
        {
            _payments = payments;
            _roomTenantQuery = roomTenantQuery;
            _email = email;
            _contracts = contracts;

            LoadCommand = new AsyncRelayCommand(LoadAsync);
            RemindCommand = new AsyncRelayCommand<RoomStatusRow>(RemindAsync);
            ExportPdfCommand = new AsyncRelayCommand(ExportPdfAsync);
            ExportExcelCommand = new AsyncRelayCommand(ExportExcelAsync);
        }

        partial void OnSelectedStatusFilterChanged(string value) => _ = LoadAsync();

        private async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                Rows.Clear();
                var cycle = await _payments.GetCycleAsync(Year, Month);
                if (cycle == null)
                {
                    TotalRooms = DebtRooms = PaidRooms = LateRooms = 0;
                    return;
                }

                var charges = await _payments.GetRoomChargesForCycleAsync(cycle.CycleId);

                var filtered = charges.AsEnumerable();
                if (SelectedStatusFilter != "Tất cả")
                {
                    filtered = filtered.Where(c => MapDisplayStatus(c) == SelectedStatusFilter);
                }

                foreach (var rc in filtered.OrderBy(c => c.RoomCode))
                    Rows.Add(new RoomStatusRow(rc));

                TotalRooms = charges.Count;
                DebtRooms = charges.Count(c => c.AmountRemaining > 0);
                PaidRooms = charges.Count(c => c.Status == PaymentStatus.Paid);
                LateRooms = charges.Count(c => c.Status == PaymentStatus.Late);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RemindAsync(RoomStatusRow? row)
        {
            if (row == null) return;
            if (row.Source.AmountRemaining <= 0)
            {
                await Shell.Current.DisplayAlertAsync("Thông báo", "Phòng đã thanh toán đủ.", "OK");
                return;
            }
            if (row.IsDataIncomplete)
            {
                await Shell.Current.DisplayAlertAsync("Thiếu dữ liệu", row.MissingReasonsText ?? "Cần bổ sung dữ liệu trước khi nhắc.", "OK");
                return;
            }

            var info = await _roomTenantQuery.GetForRoomAsync(row.RoomCode);
            if (info.Emails.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync("Thiếu email", "Không có email người thuê.", "OK");
                return;
            }

            var subject = $"[QLT] Nhắc đóng tiền phòng {row.RoomCode} tháng {Month:00}/{Year}";
            var body =
$@"Xin chào,
Phòng {row.RoomCode} còn nợ: {row.Source.AmountRemaining:N0} đ.
Trạng thái hiện tại: {row.DisplayStatus}.
Vui lòng thanh toán sớm.

Trân trọng,
QLT";

            try
            {
                foreach (var mail in info.Emails.Where(e => !string.IsNullOrWhiteSpace(e)))
                    await _email.SendInvoiceAsync(mail, subject, body);
                await Shell.Current.DisplayAlertAsync("Đã gửi", $"Đã gửi nhắc nợ cho {row.RoomCode}.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi gửi email", ex.Message, "OK");
            }
        }

        // Generate PDF-like text report based on UI content
        private async Task ExportPdfAsync()
        {
            if (IsBusy) return;
            if (Rows.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync("Xuất PDF", "Không có dữ liệu để xuất.", "OK");
                return;
            }

            try
            {
                var folder = Path.Combine(FileSystem.AppDataDirectory, "reports");
                Directory.CreateDirectory(folder);

                var fileName = $"Debt_{Year}{Month:00}.pdf";
                var path = Path.Combine(folder, fileName);

                var sb = new StringBuilder();
                sb.AppendLine($"BÁO CÁO CÔNG NỢ THÁNG {Month:00}/{Year}");
                sb.AppendLine($"Lọc: {SelectedStatusFilter}");
                sb.AppendLine($"Tổng phòng: {TotalRooms}, Nợ: {DebtRooms}, Đã trả đủ: {PaidRooms}, Trễ hạn: {LateRooms}");
                sb.AppendLine(new string('-', 100));
                sb.AppendLine($"{"Phòng",-12} {"Trạng thái",-18} {"Còn nợ",-18} {"Tiền phòng",-14} {"Điện",-12} {"Nước",-12} {"Phí khác",-12} {"Xe",-10} {"Tổng",-14} {"Ghi chú"}");
                sb.AppendLine(new string('-', 100));

                foreach (var row in Rows.OrderBy(r => r.RoomCode))
                {
                    var rc = row.Source;
                    var note = row.IsDataIncomplete ? (row.MissingReasonsText ?? "Thiếu dữ liệu") : "";
                    sb.AppendLine($"{rc.RoomCode,-12} {row.DisplayStatus,-18} {rc.AmountRemaining, -18:N0} {rc.BaseRent,-14:N0} {rc.ElectricAmount,-12:N0} {rc.WaterAmount,-12:N0} {rc.CustomFeesTotal,-12:N0} {rc.BikePrice,-10:N0} {rc.TotalDue,-14:N0} {note}");
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

        // Generate Excel-friendly CSV based on UI content
        private async Task ExportExcelAsync()
        {
            if (IsBusy) return;
            if (Rows.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync("Xuất Excel", "Không có dữ liệu để xuất.", "OK");
                return;
            }

            try
            {
                var folder = Path.Combine(FileSystem.AppDataDirectory, "reports");
                Directory.CreateDirectory(folder);

                var fileName = $"Debt_{Year}{Month:00}.csv";
                var path = Path.Combine(folder, fileName);

                var sb = new StringBuilder();
                // Header includes filter and summary as first lines (prefixed with #)
                sb.AppendLine($"# Báo cáo công nợ tháng {Month:00}/{Year}");
                sb.AppendLine($"# Lọc: {SelectedStatusFilter}");
                sb.AppendLine($"# Tổng phòng: {TotalRooms}, Nợ: {DebtRooms}, Đã trả đủ: {PaidRooms}, Trễ hạn: {LateRooms}");
                sb.AppendLine("RoomCode,DisplayStatus,AmountRemaining,BaseRent,ElectricAmount,WaterAmount,CustomFeesTotal,BikePrice,TotalDue,Incomplete,MissingReasons");

                foreach (var row in Rows.OrderBy(r => r.RoomCode))
                {
                    var rc = row.Source;
                    var incomplete = row.IsDataIncomplete ? "Yes" : "No";
                    var reasons = (row.MissingReasonsText ?? "").Replace("\n", " | ");

                    string esc(string s) => s.Contains(',') ? $"\"{s.Replace("\"", "\"\"")}\"" : s;

                    var line = string.Join(",",
                        esc(rc.RoomCode),
                        esc(row.DisplayStatus),
                        rc.AmountRemaining.ToString("0.##"),
                        rc.BaseRent.ToString("0.##"),
                        rc.ElectricAmount.ToString("0.##"),
                        rc.WaterAmount.ToString("0.##"),
                        rc.CustomFeesTotal.ToString("0.##"),
                        rc.BikePrice.ToString("0.##"),
                        rc.TotalDue.ToString("0.##"),
                        esc(incomplete),
                        esc(reasons)
                    );
                    sb.AppendLine(line);
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

        private static string MapDisplayStatus(RoomCharge rc) => rc.Status switch
        {
            PaymentStatus.Paid => "Đã trả đủ",
            PaymentStatus.PartiallyPaid => "Đã trả một phần",
            PaymentStatus.Late => "Trễ hạn",
            PaymentStatus.Closed => "Đã đóng",
            _ => rc.AmountRemaining > 0 ? "Chưa trả" : "Đã trả đủ"
        };
    }

    public class RoomStatusRow : ObservableObject
    {
        public RoomCharge Source { get; }
        public string RoomCode => Source.RoomCode;

        public string DisplayStatus => Source.Status switch
        {
            PaymentStatus.Paid => "Đã trả đủ",
            PaymentStatus.PartiallyPaid => "Đã trả một phần",
            PaymentStatus.Late => "Trễ hạn",
            PaymentStatus.Closed => "Đã đóng",
            _ => Source.AmountRemaining > 0 ? "Chưa trả" : "Đã trả đủ"
        };

        public string StatusColor => DisplayStatus switch
        {
            "Đã trả đủ" => "#C8E6C9",
            "Đã trả một phần" => "#FFF9C4",
            "Trễ hạn" => "#FFE0B2",
            "Đã đóng" => "#B0BEC5",
            "Chưa trả" => IsDataIncomplete ? "#E0E0E0" : "#FFECB3",
            _ => "#E0E0E0"
        };

        public string StatusText => DisplayStatus;

        public bool CanRemind => Source.AmountRemaining > 0 && !IsDataIncomplete;

        public string AmountRemainingDisplay => $"Còn nợ: {Source.AmountRemaining:N0} đ";
        public string AmountColor => Source.AmountRemaining > 0 ? "#C62828" : "#2E7D32";

        public bool IsDataIncomplete
        {
            get
            {
                var elecOk = Source.ElectricReading != null &&
                             Source.ElectricReading.Current >= Source.ElectricReading.Previous &&
                             Source.ElectricReading.Current > 0;
                var waterOk = Source.WaterReading != null &&
                              Source.WaterReading.Current >= Source.WaterReading.Previous &&
                              Source.WaterReading.Current > 0;
                return !(elecOk && waterOk);
            }
        }

        public string? MissingReasonsText
        {
            get
            {
                if (!IsDataIncomplete) return null;
                var parts = new System.Collections.Generic.List<string>();
                if (Source.ElectricReading == null || Source.ElectricReading.Current <= Source.ElectricReading.Previous || Source.ElectricReading.Current == 0)
                    parts.Add("Cần cập nhật chỉ số điện");
                if (Source.WaterReading == null || Source.WaterReading.Current <= Source.WaterReading.Previous || Source.WaterReading.Current == 0)
                    parts.Add("Cần cập nhật chỉ số nước");
                return string.Join("\n", parts);
            }
        }

        public RoomStatusRow(RoomCharge rc) => Source = rc;
    }
}