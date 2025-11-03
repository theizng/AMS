// ViewModels/PaymentsViewModel.cs
using AMS.Data;
using AMS.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace AMS.ViewModels
{
    public class PaymentsViewModel : INotifyPropertyChanged
    {
        private readonly AMSDbContext _db;

        private ObservableCollection<Invoice> _invoices = new();
        private string _searchText = string.Empty;
        private bool _isRefreshing;
        private DateTime _month = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

        public ObservableCollection<Invoice> Invoices
        {
            get => _invoices;
            set { _invoices = value; OnPropertyChanged(); }
        }

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); }
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set { _isRefreshing = value; OnPropertyChanged(); }
        }

        public DateTime Month
        {
            get => _month;
            set { _month = new DateTime(value.Year, value.Month, 1); OnPropertyChanged(); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand GenerateCommand { get; }
        public ICommand MarkPaidCommand { get; }
        public ICommand MarkUnpaidCommand { get; }
        public ICommand RecordPartialCommand { get; }

        public PaymentsViewModel(AMSDbContext db)
        {
            _db = db;

            RefreshCommand = new Command(async () => await LoadAsync());
            SearchCommand = new Command(async () => await LoadAsync());
            GenerateCommand = new Command(async () => await GenerateBillsAsync(Month));
            MarkPaidCommand = new Command<Invoice>(async i => await MarkPaidAsync(i));
            MarkUnpaidCommand = new Command<Invoice>(async i => await MarkUnpaidAsync(i));
            RecordPartialCommand = new Command<Invoice>(async i => await RecordPartialAsync(i));

            _ = LoadAsync();
        }

        public async Task LoadAsync()
        {
            try
            {
                IsRefreshing = true;
                var month = new DateTime(Month.Year, Month.Month, 1);

                var q = _db.Invoices
                    .AsNoTracking()
                    .Include(i => i.Room)
                    .Where(i => i.BillingMonth == month);

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var k = SearchText.Trim();
                    q = q.Where(i =>
                        (i.Room != null && EF.Functions.Like(i.Room.RoomCode, $"%{k}%")) ||
                        EF.Functions.Like(i.Notes ?? "", $"%{k}%"));
                }

                var items = await q
                    .OrderBy(i => i.Room!.RoomCode)
                    .ToListAsync();

                Invoices = new ObservableCollection<Invoice>(items);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Payments] Load error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Lỗi", "Không thể tải danh sách hóa đơn.", "OK");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        public async Task GenerateBillsAsync(DateTime billingMonth)
        {
            try
            {
                var month = new DateTime(billingMonth.Year, billingMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);

                // Lấy các phòng đang có người ở (RoomOccupancy.Active) hoặc tất cả phòng (đơn giản: theo Price)
                var occupiedRoomIds = await _db.RoomOccupancies
                    .Where(ro => ro.MoveOutDate == null)
                    .Select(ro => ro.RoomId)
                    .Distinct()
                    .ToListAsync();

                var rooms = await _db.Rooms
                    .Where(r => occupiedRoomIds.Contains(r.IdRoom))
                    .ToListAsync();

                // Upsert hóa đơn cho từng phòng
                foreach (var room in rooms)
                {
                    var exists = await _db.Invoices
                        .AnyAsync(i => i.RoomId == room.IdRoom && i.BillingMonth == month);
                    if (exists) continue;

                    var baseRent = room.Price; // dùng Price của Room làm tiền thuê cơ bản
                    var total = baseRent;      // bước 1: chưa tính utilities/extras

                    var invoice = new Invoice
                    {
                        RoomId = room.IdRoom,
                        BillingMonth = month,
                        BaseRent = baseRent,
                        Utilities = 0,
                        Extras = 0,
                        TotalAmount = total,
                        PaidAmount = 0,
                        Status = InvoiceStatus.Unpaid,
                        UniqueCode = $"INV-{room.RoomCode}-{month:yyyyMM}"
                    };

                    _db.Invoices.Add(invoice);
                }

                await _db.SaveChangesAsync();
                await LoadAsync();
                await Application.Current.MainPage.DisplayAlert("Thành công", "Đã tạo hóa đơn cho tháng.", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Payments] Generate error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Lỗi", "Không thể tạo hóa đơn.", "OK");
            }
        }

        private async Task MarkPaidAsync(Invoice? i)
        {
            if (i == null) return;
            var entity = await _db.Invoices.FirstAsync(x => x.Id == i.Id);
            entity.PaidAmount = entity.TotalAmount;
            entity.Status = InvoiceStatus.Paid;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await LoadAsync();
        }

        private async Task MarkUnpaidAsync(Invoice? i)
        {
            if (i == null) return;
            var entity = await _db.Invoices.FirstAsync(x => x.Id == i.Id);
            entity.PaidAmount = 0;
            entity.Status = InvoiceStatus.Unpaid;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await LoadAsync();
        }

        private async Task RecordPartialAsync(Invoice? i)
        {
            if (i == null) return;
            var input = await Application.Current.MainPage.DisplayPromptAsync("Thanh toán 1 phần", "Nhập số tiền đã thu (VND):", keyboard: Keyboard.Numeric);
            if (string.IsNullOrWhiteSpace(input)) return;

            if (!decimal.TryParse(input, out var amount) || amount < 0)
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi", "Số tiền không hợp lệ.", "OK");
                return;
            }

            var entity = await _db.Invoices.FirstAsync(x => x.Id == i.Id);
            entity.PaidAmount = Math.Min(entity.TotalAmount, amount);
            entity.Status = entity.PaidAmount <= 0 ? InvoiceStatus.Unpaid :
                            entity.PaidAmount < entity.TotalAmount ? InvoiceStatus.Partial :
                            InvoiceStatus.Paid;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await LoadAsync();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
