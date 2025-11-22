using AMS.Models;
using AMS.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
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

        // Filter now uses normalized payment states (hide MissingData / ReadyToSend / SentFirst)
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

        private Task ExportPdfAsync() => Task.CompletedTask;
        private Task ExportExcelAsync() => Task.CompletedTask;

        // Display mapping used for filtering
        private static string MapDisplayStatus(RoomCharge rc) => rc.Status switch
        {
            PaymentStatus.Paid => "Đã trả đủ",
            PaymentStatus.PartiallyPaid => "Đã trả một phần",
            PaymentStatus.Late => "Trễ hạn",
            PaymentStatus.Closed => "Đã đóng",
            _ => rc.AmountRemaining > 0 ? "Chưa trả" : "Đã trả đủ" // MissingData / ReadyToSend / SentFirst / UnPaid consolidated
        };
    }

    public class RoomStatusRow : ObservableObject
    {
        public RoomCharge Source { get; }
        public string RoomCode => Source.RoomCode;

        // Normalized display (hide internal states)
        public string DisplayStatus => Source.Status switch
        {
            PaymentStatus.Paid => "Đã trả đủ",
            PaymentStatus.PartiallyPaid => "Đã trả một phần",
            PaymentStatus.Late => "Trễ hạn",
            PaymentStatus.Closed => "Đã đóng",
            _ => Source.AmountRemaining > 0 ? "Chưa trả" : "Đã trả đủ"
        };

        // Badge color based on normalized status
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

        // Data completeness check (similar heuristic)
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