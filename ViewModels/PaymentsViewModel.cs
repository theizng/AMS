using AMS.Helpers;
using AMS.Models;
using AMS.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microcharts;
using Microsoft.Maui.Storage;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AMS.ViewModels
{
    public partial class PaymentsViewModel : ObservableObject
    {
        private readonly IPaymentsRepository _repo;
        private readonly IPaymentSettingsProvider _settings;
        private readonly IRoomTenantQuery _roomQuery;
        private readonly IEmailNotificationService _email;
        private readonly IContractsRepository _repoContract;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private ObservableCollection<PaymentCycleLight> cycles = new();
        [ObservableProperty] private PaymentCycleLight? selectedCycle;
        [ObservableProperty] private ObservableCollection<OverviewRow> rows = new();

        [ObservableProperty] private decimal totalDue;
        [ObservableProperty] private decimal totalPaid;
        [ObservableProperty] private decimal totalRemaining;
        // ReadyCount now = count of rows that are sendable (data complete, unpaid, have PDF)
        [ObservableProperty] private int readyCount;
        [ObservableProperty] private string statusMessage = "";

        // Microcharts KPI
        [ObservableProperty] private int paidCount;
        [ObservableProperty] private int unpaidCount;
        [ObservableProperty] private Chart? paidUnpaidPie;

        public IAsyncRelayCommand LoadCyclesCommand { get; }
        public IAsyncRelayCommand CreateCycleCommand { get; }
        public IAsyncRelayCommand ReseedCommand { get; }
        public IAsyncRelayCommand RecomputeCommand { get; }
        public IAsyncRelayCommand<OverviewRow> MakeReadyCommand { get; }
        public IAsyncRelayCommand<OverviewRow> MarkLateCommand { get; }
        public IAsyncRelayCommand<OverviewRow> MarkPaidCommand { get; }
        public IAsyncRelayCommand<OverviewRow> MarkUnpaidCommand { get; }
        public IAsyncRelayCommand<OverviewRow> AddPartialPaymentCommand { get; }
        public IAsyncRelayCommand<OverviewRow> ChooseStatusCommand { get; }
        public IAsyncRelayCommand<OverviewRow> SendInvoiceEmailCommand { get; }
        public IAsyncRelayCommand SendAllWithInvoiceCommand { get; }

        public IAsyncRelayCommand OpenMeterPageCommand { get; }
        public IAsyncRelayCommand OpenInvoicesPageCommand { get; }

        public PaymentsViewModel(IPaymentsRepository repo,
                                 IPaymentSettingsProvider settings,
                                 IRoomTenantQuery roomQuery,
                                 IEmailNotificationService email,
                                 IContractsRepository repoContract)
        {
            _repoContract = repoContract;
            _repo = repo;
            _settings = settings;
            _roomQuery = roomQuery;
            _email = email;

            LoadCyclesCommand = new AsyncRelayCommand(LoadCyclesAsync);
            CreateCycleCommand = new AsyncRelayCommand(CreateCycleAsync);
            ReseedCommand = new AsyncRelayCommand(ReseedAsync);
            RecomputeCommand = new AsyncRelayCommand(RecomputeAsync);
            MakeReadyCommand = new AsyncRelayCommand<OverviewRow>(MakeReadyAsync);
            MarkLateCommand = new AsyncRelayCommand<OverviewRow>(MarkLateAsync);
            MarkPaidCommand = new AsyncRelayCommand<OverviewRow>(MarkPaidAsync);
            MarkUnpaidCommand = new AsyncRelayCommand<OverviewRow>(MarkUnpaidAsync);
            AddPartialPaymentCommand = new AsyncRelayCommand<OverviewRow>(AddPartialPaymentAsync);
            ChooseStatusCommand = new AsyncRelayCommand<OverviewRow>(ChooseStatusAsync);
            SendInvoiceEmailCommand = new AsyncRelayCommand<OverviewRow>(SendInvoiceEmailAsync);
            SendAllWithInvoiceCommand = new AsyncRelayCommand(SendAllWithInvoiceAsync);

            OpenMeterPageCommand = new AsyncRelayCommand(() => Shell.Current.GoToAsync("//payment_meterentry"));
            OpenInvoicesPageCommand = new AsyncRelayCommand(() => Shell.Current.GoToAsync("//payment_invoices"));
        }

        public async Task LoadCyclesAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                Cycles.Clear();
                var all = await _repo.GetRecentCyclesAsync(24);
                foreach (var c in all.OrderByDescending(c => new DateTime(c.Year, c.Month, 1)))
                    Cycles.Add(new PaymentCycleLight(c));

                if (SelectedCycle == null && Cycles.Count > 0)
                {
                    SelectedCycle = Cycles.First();
                    await LoadSelectedCycleAsync();
                }
                StatusMessage = Cycles.Count == 0
                    ? "Chưa có chu kỳ nào. Bấm 'Tạo chu kỳ mới'."
                    : "";
            }
            catch (Exception ex)
            {
                StatusMessage = "Lỗi tải chu kỳ: " + ex.Message;
            }
            finally { IsBusy = false; }
            OnPropertyChanged(nameof(StatusMessage));
        }

        partial void OnSelectedCycleChanged(PaymentCycleLight? oldValue, PaymentCycleLight? newValue)
        {
            if (newValue != null)
                _ = LoadSelectedCycleAsync();
        }

        private async Task LoadSelectedCycleAsync()
        {
            if (SelectedCycle == null) return;
            Rows.Clear();
            var cycleFull = await _repo.GetCycleAsync(SelectedCycle.Year, SelectedCycle.Month);
            if (cycleFull?.RoomCharges != null)
            {
                foreach (var rc in cycleFull.RoomCharges.OrderBy(r => r.RoomCode))
                    Rows.Add(new OverviewRow(rc, SelectedCycle.Year, SelectedCycle.Month));
            }
            Recalc();
        }

        private async Task CreateCycleAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var today = DateTime.Today;
                var existsCurrent = Cycles.Any(c => c.Year == today.Year && c.Month == today.Month);
                var y = existsCurrent && today.Month == 12 ? today.Year + 1 : today.Year;
                var m = existsCurrent ? (today.Month == 12 ? 1 : today.Month + 1) : today.Month;

                await _repo.CreateCycleAsync(y, m);
                await LoadCyclesAsync();

                StatusMessage = $"Đã tạo chu kỳ {m:00}/{y}.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Lỗi tạo chu kỳ: " + ex.Message;
            }
            finally { IsBusy = false; }
            OnPropertyChanged(nameof(StatusMessage));
        }

        private async Task ReseedAsync()
        {
            if (SelectedCycle == null) return;
            await _repo.ReseedRoomChargesAsync(SelectedCycle.CycleId);
            await LoadSelectedCycleAsync();
            StatusMessage = "Đã đồng bộ phòng vào chu kỳ.";
            OnPropertyChanged(nameof(StatusMessage));
        }

        private async Task RecomputeAsync()
        {
            if (SelectedCycle == null) return;
            var sets = _settings.Get();
            foreach (var r in Rows)
            {
                var rc = r.Source;
                rc.ElectricAmount = rc.ElectricReading?.Amount ?? 0;
                rc.WaterAmount = rc.WaterReading?.Amount ?? 0;
                rc.CustomFeesTotal = (rc.Fees?.Sum(f => f.Amount) ?? 0);
                rc.Status = EvaluateStatus(rc, sets);
                await _repo.UpdateRoomChargeAsync(rc);
                r.Refresh();
            }
            Recalc();
            StatusMessage = "Đã tính lại trạng thái.";
            OnPropertyChanged(nameof(StatusMessage));
        }

        private async Task MakeReadyAsync(OverviewRow? row)
        {
            if (row == null) return;
            var sets = _settings.Get();
            var rc = row.Source;

            rc.ElectricAmount = rc.ElectricReading?.Amount ?? 0;
            rc.WaterAmount = rc.WaterReading?.Amount ?? 0;
            rc.CustomFeesTotal = (rc.Fees?.Sum(f => f.Amount) ?? 0);

            var st = EvaluateStatus(rc, sets);
            if (st == PaymentStatus.ReadyToSend || st == PaymentStatus.PartiallyPaid)
                rc.Status = st;
            else
                await Shell.Current.DisplayAlertAsync("Chưa đủ", $"Phòng {rc.RoomCode} chưa đủ điều kiện.", "OK");

            await _repo.UpdateRoomChargeAsync(rc);
            row.Refresh();
            Recalc();
        }

        private async Task MarkLateAsync(OverviewRow? row)
        {
            if (row == null) return;
            row.Source.Status = PaymentStatus.Late;
            await _repo.UpdateRoomChargeAsync(row.Source);
            row.Refresh();
            Recalc();
        }

        private async Task MarkPaidAsync(OverviewRow? row)
        {
            if (row == null || SelectedCycle == null) return;
            var rc = row.Source;
            var remaining = rc.AmountRemaining;
            if (remaining <= 0)
            {
                await Shell.Current.DisplayAlertAsync("Thông báo", "Phòng này đã thanh toán đủ.", "OK");
                return;
            }

            await _repo.AddPaymentRecordAsync(new PaymentRecord
            {
                RoomChargeId = rc.RoomChargeId,
                Amount = remaining,
                PaidAt = DateTime.UtcNow,
                IsPartial = false,
                Note = "MarkPaid from overview"
            });
            await LoadSelectedCycleAsync();
        }
        private async Task ChooseStatusAsync(OverviewRow? row)
        {
            if (row == null) return;

            var choice = await Shell.Current.DisplayActionSheet(
                "Cập nhật trạng thái",
                "Hủy", null,
                "Đã trả", "Trả một phần", "Chưa trả", "Đánh dấu trễ");

            switch (choice)
            {
                case "Đã trả":
                    await MarkPaidAsync(row);
                    break;
                case "Trả một phần":
                    await AddPartialPaymentAsync(row);
                    break;
                case "Chưa trả":
                    await MarkUnpaidAsync(row);
                    break;
                case "Đánh dấu trễ":
                    await MarkLateAsync(row);
                    break;
                default:
                    break;
            }
        }
        private async Task MarkUnpaidAsync(OverviewRow? row)
        {
            if (row == null) return;
            var rc = row.Source;
            rc.AmountPaid = 0;
            rc.Status = PaymentStatus.UnPaid; // treat as unpaid & sendable when data complete
            rc.PaidAt = null;

            await _repo.UpdateRoomChargeAsync(rc);
            row.Refresh();
            Recalc();
        }

        private async Task AddPartialPaymentAsync(OverviewRow? row)
        {
            if (row == null) return;
            var rc = row.Source;

            var input = await Shell.Current.DisplayPromptAsync(
                "Thanh toán một phần",
                $"Nhập số tiền (Còn lại: {(rc.AmountRemaining):N0} đ):",
                "Ghi nhận", "Hủy",
                keyboard: Microsoft.Maui.Keyboard.Numeric);
            if (string.IsNullOrWhiteSpace(input)) return;

            if (!decimal.TryParse(input.Replace(".", "").Replace(",", ""), out var amount) || amount <= 0)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi", "Số tiền không hợp lệ.", "OK");
                return;
            }

            await _repo.AddPaymentRecordAsync(new PaymentRecord
            {
                RoomChargeId = rc.RoomChargeId,
                Amount = amount,
                PaidAt = DateTime.UtcNow,
                IsPartial = true,
                Note = "Partial from overview"
            });
            await LoadSelectedCycleAsync();
        }

        private async Task SendInvoiceEmailAsync(OverviewRow? row)
        {
            if (row == null || SelectedCycle == null) return;

            if (!TryFindInvoicePath(SelectedCycle.Year, SelectedCycle.Month, row.RoomCode, out var path))
            {
                await Shell.Current.DisplayAlertAsync("Chưa có hóa đơn", "Hãy vào trang Hóa đơn để tạo PDF trước.", "OK");
                return;
            }

            var s = _settings.Get();
            var info = await _roomQuery.GetForRoomAsync(row.RoomCode);
            if (info.Emails.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync("Thiếu email", "Phòng này chưa có email người thuê.", "OK");
                return;
            }

            await _email.SendInvoicePdfAsync(info, row.RoomCode, SelectedCycle.Year, SelectedCycle.Month, s, path);
            await Shell.Current.DisplayAlertAsync("Đã gửi", $"Đã gửi hóa đơn cho {row.RoomCode}.", "OK");
        }

        private async Task SendAllWithInvoiceAsync()
        {
            if (SelectedCycle == null) return;

            var sent = 0; var skipped = 0;
            foreach (var row in Rows)
            {
                if (!TryFindInvoicePath(SelectedCycle.Year, SelectedCycle.Month, row.RoomCode, out var path))
                {
                    skipped++;
                    continue;
                }
                try
                {
                    var s = _settings.Get();
                    var info = await _roomQuery.GetForRoomAsync(row.RoomCode);
                    if (info.Emails.Count == 0) { skipped++; continue; }

                    await _email.SendInvoicePdfAsync(info, row.RoomCode, SelectedCycle.Year, SelectedCycle.Month, s, path);
                    sent++;
                }
                catch
                {
                    skipped++;
                }
            }

            await Shell.Current.DisplayAlertAsync("Kết quả", $"Đã gửi: {sent}, Bỏ qua: {skipped} (chưa có PDF hoặc thiếu email).", "OK");
        }

        private static bool TryFindInvoicePath(int year, int month, string roomCode, out string path)
        {
            var folder = Path.Combine(FileSystem.AppDataDirectory, "invoices");
            path = string.Empty;
            if (!Directory.Exists(folder)) return false;
            var prefix = $"{year}{month:00}-{roomCode}";
            var file = Directory.GetFiles(folder, "*.pdf").FirstOrDefault(f => Path.GetFileName(f).StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            if (file == null) return false;
            path = file;
            return true;
        }

        // Minimal cleanup: separate data completeness from payment progress; ReadyToSend acts as unpaid & complete
        private bool IsDataComplete(RoomCharge rc, PaymentSettings s)
        {
            var needElec = (rc.ElectricReading?.Rate ?? s.DefaultElectricRate) > 0;
            var elecOk = !needElec || (rc.ElectricReading?.Confirmed ?? false)
                         && rc.ElectricReading!.Current >= rc.ElectricReading.Previous;

            var needWater = (rc.WaterReading?.Rate ?? s.DefaultWaterRate) > 0;
            var waterOk = !needWater || (rc.WaterReading?.Confirmed ?? false)
                          && rc.WaterReading!.Current >= rc.WaterReading.Previous;

            var totalOk = rc.BaseRent +
                          (rc.Fees?.Sum(f => f.Amount) ?? 0) +
                          (rc.ElectricReading?.Amount ?? 0) +
                          (rc.WaterReading?.Amount ?? 0) >= 0;

            return elecOk && waterOk && totalOk;
        }

        private PaymentStatus EvaluateStatus(RoomCharge rc, PaymentSettings sets)
        {
            if (!IsDataComplete(rc, sets))
                return PaymentStatus.MissingData;

            if (rc.AmountPaid >= rc.TotalDue && rc.TotalDue > 0)
                return PaymentStatus.Paid;

            if (rc.AmountPaid > 0 && rc.AmountPaid < rc.TotalDue)
                return PaymentStatus.PartiallyPaid;

            if (rc.Status == PaymentStatus.Late)
                return PaymentStatus.Late;

            // Treat sendable unpaid as ReadyToSend (legacy meaning)
            return PaymentStatus.ReadyToSend;
        }

        private void Recalc()
        {
            TotalDue = Rows.Sum(r => r.Source.TotalDue);
            TotalPaid = Rows.Sum(r => r.Source.AmountPaid);
            TotalRemaining = TotalDue - TotalPaid;

            var sets = _settings.Get();
            // ReadyCount now counts rows that are unpaid & complete & have a PDF
            ReadyCount = Rows.Count(r =>
            {
                var rc = r.Source;
                var complete = IsDataComplete(rc, sets);
                var unpaid = rc.AmountRemaining > 0 && rc.Status != PaymentStatus.Paid;
                var hasPdf = TryFindInvoicePath(r.Year, r.Month, r.RoomCode, out _);
                return complete && unpaid && hasPdf;
            });

            PaidCount = Rows.Count(r => r.Source.Status == PaymentStatus.Paid);
            UnpaidCount = Rows.Count(r => r.Source.Status != PaymentStatus.Paid);

            PaidUnpaidPie = new DonutChart
            {
                Entries = ChartHelper.BuildPaidUnpaidEntries(PaidCount, UnpaidCount).ToList(),
                HoleRadius = 0.5f,
                LabelTextSize = 28
            };
        }
    }

    public class PaymentCycleLight
    {
        public string CycleId { get; }
        public int Year { get; }
        public int Month { get; }
        public string Display => $"{Month:00}/{Year}";

        public PaymentCycleLight(PaymentCycle cycle)
        {
            CycleId = cycle.CycleId;
            Year = cycle.Year;
            Month = cycle.Month;
        }
    }

    public partial class OverviewRow : ObservableObject
    {
        public RoomCharge Source { get; }
        public string RoomCode => Source.RoomCode;
        public int Year { get; }
        public int Month { get; }

        [ObservableProperty] private string summary = "";
        [ObservableProperty] private string statusText = "";

        public OverviewRow(RoomCharge rc, int year, int month)
        {
            Source = rc;
            Year = year;
            Month = month;
            Refresh();
        }

        public void Refresh()
        {
            Summary = $"Tổng: {Source.TotalDue:N0} đ | Đã trả: {Source.AmountPaid:N0} đ | Còn: {Source.AmountRemaining:N0} đ";
            StatusText = Source.Status switch
            {
                PaymentStatus.MissingData => "Thiếu dữ liệu",
                PaymentStatus.UnPaid => "Chưa trả",          // renamed for clarity
                PaymentStatus.SentFirst => "Đã gửi lần 1",
                PaymentStatus.PartiallyPaid => "Đã trả một phần",
                PaymentStatus.Paid => "Đã trả đủ",
                PaymentStatus.Late => "Trễ hạn",
                PaymentStatus.Closed => "Đã đóng",
                _ => Source.Status.ToString()
            };
        }
    }
}