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

        [ObservableProperty] private string selectedStatusFilter = "Tất cả";

        [ObservableProperty] private ObservableCollection<RoomStatusRow> rows = new();

        // Summary counters
        [ObservableProperty] private int totalRooms;
        [ObservableProperty] private int debtRooms;
        [ObservableProperty] private int paidRooms;
        [ObservableProperty] private int lateRooms;

        public IReadOnlyList<string> StatusFilterOptions { get; } = new[] {
            "Tất cả",
            "Thiếu dữ liệu",
            "Sẵn sàng gửi",
            "Đã gửi lần 1",
            "Đã trả một phần",
            "Đã trả đủ",
            "Trễ hạn"
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
                    filtered = filtered.Where(c => StatusToText(c.Status) == SelectedStatusFilter);
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

            // Fetch tenant emails via IRoomTenantQuery
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
Trạng thái hiện tại: {row.StatusText}.
Vui lòng thanh toán sớm.

Trân trọng,
QLT";

            try
            {
                foreach (var mail in info.Emails.Where(e => !string.IsNullOrWhiteSpace(e)))
                    await _email.SendInvoiceAsync(mail, subject, body); // reuse simple email method

                await Shell.Current.DisplayAlertAsync("Đã gửi", $"Đã gửi nhắc nợ cho {row.RoomCode}.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi gửi email", ex.Message, "OK");
            }
        }

        private Task ExportPdfAsync() => Task.CompletedTask;
        private Task ExportExcelAsync() => Task.CompletedTask;

        private static string StatusToText(PaymentStatus status) => status switch
        {
            PaymentStatus.MissingData => "Thiếu dữ liệu",
            PaymentStatus.ReadyToSend => "Sẵn sàng gửi",
            PaymentStatus.SentFirst => "Đã gửi lần 1",
            PaymentStatus.PartiallyPaid => "Đã trả một phần",
            PaymentStatus.Paid => "Đã trả đủ",
            PaymentStatus.Late => "Trễ hạn",
            PaymentStatus.Closed => "Đã đóng",
            _ => status.ToString()
        };
    }

    public class RoomStatusRow : ObservableObject
    {
        public RoomCharge Source { get; }
        public string RoomCode => Source.RoomCode;
        public string StatusText => StatusToText(Source.Status);
        public bool CanRemind => Source.AmountRemaining > 0;
        public string AmountRemainingDisplay => $"Còn nợ: {Source.AmountRemaining:N0} đ";

        public string StatusColor => Source.Status switch
        {
            PaymentStatus.Paid => "#C8E6C9",
            PaymentStatus.PartiallyPaid => "#FFF9C4",
            PaymentStatus.Late => "#FFE0B2",
            PaymentStatus.MissingData => "#E0E0E0",
            PaymentStatus.ReadyToSend => "#BBDEFB",
            PaymentStatus.SentFirst => "#BBDEFB",
            _ => "#E0E0E0"
        };

        public string AmountColor => Source.AmountRemaining > 0 ? "#C62828" : "#2E7D32";

        public RoomStatusRow(RoomCharge rc) => Source = rc;

        private static string StatusToText(PaymentStatus status) => status switch
        {
            PaymentStatus.MissingData => "Thiếu dữ liệu",
            PaymentStatus.ReadyToSend => "Sẵn sàng gửi",
            PaymentStatus.SentFirst => "Đã gửi lần 1",
            PaymentStatus.PartiallyPaid => "Đã trả một phần",
            PaymentStatus.Paid => "Đã trả đủ",
            PaymentStatus.Late => "Trễ hạn",
            PaymentStatus.Closed => "Đã đóng",
            _ => status.ToString()
        };
    }
}