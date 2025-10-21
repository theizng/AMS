using AMS.Data;
using AMS.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace AMS.ViewModels
{
    public class RoomDetailViewModel : INotifyPropertyChanged
    {
        private readonly AMSDbContext _db;

        private int _roomId;
        private Room? _room;
        private Tenant? _currentTenant;
        private ObservableCollection<Tenant> _tenantHistory = new();

        public Room? Room
        {
            get => _room;
            set { _room = value; OnPropertyChanged(); OnPropertyChanged(nameof(Title)); OnPropertyChanged(nameof(HouseAddress)); OnPropertyChanged(nameof(StatusText)); OnPropertyChanged(nameof(AreaText)); OnPropertyChanged(nameof(PriceText)); }
        }

        public Tenant? CurrentTenant
        {
            get => _currentTenant;
            set { _currentTenant = value; OnPropertyChanged(); (MoveOutCommand as Command)?.ChangeCanExecute(); }
        }

        public ObservableCollection<Tenant> TenantHistory
        {
            get => _tenantHistory;
            set { _tenantHistory = value; OnPropertyChanged(); }
        }

        public string Title => Room == null ? "Chi tiết phòng" : $"Phòng {Room.RoomCode}";
        public string HouseAddress => Room?.House?.Address ?? "";
        public string StatusText => Room?.RoomStatus.ToString() ?? "";
        public string AreaText => Room == null ? "" : $"{Room.Area:N0} m²";
        public string PriceText => Room == null ? "" : $"{Room.Price:N0} đ";

        public ICommand RefreshCommand { get; }
        public ICommand EditRoomCommand { get; }
        public ICommand MoveOutCommand { get; }
        public ICommand AddTenantCommand { get; }
        public ICommand CallPhoneCommand { get; }
        public ICommand OpenContractCommand { get; }

        public RoomDetailViewModel(AMSDbContext db)
        {
            _db = db;

            RefreshCommand = new Command(async () => await LoadAsync());
            EditRoomCommand = new Command(async () =>
            {
                if (_roomId <= 0) return;
                await Shell.Current.GoToAsync($"editroom?roomId={_roomId}&houseId={Room?.HouseID ?? 0}");
            });

            AddTenantCommand = new Command(async () =>
            {
                // Navigate to tenants flow; adjust route to your existing TenantsPage
                await Shell.Current.GoToAsync($"tenants?roomId={_roomId}");
            });

            MoveOutCommand = new Command(async () => await MoveOutAsync(), () => CurrentTenant != null);

            CallPhoneCommand = new Command(async () =>
            {
                var phone = CurrentTenant?.PhoneNumber;
                if (!string.IsNullOrWhiteSpace(phone))
                {
                    try { await Microsoft.Maui.ApplicationModel.Launcher.OpenAsync($"tel:{phone}"); }
                    catch { await Application.Current.MainPage.DisplayAlert("Lỗi", "Không thể thực hiện cuộc gọi.", "OK"); }
                }
            });

            OpenContractCommand = new Command(async () =>
            {
                var url = CurrentTenant?.ContractUrl;
                if (!string.IsNullOrWhiteSpace(url))
                {
                    try { await Microsoft.Maui.ApplicationModel.Launcher.OpenAsync(url); }
                    catch { await Application.Current.MainPage.DisplayAlert("Lỗi", "Không mở được hợp đồng.", "OK"); }
                }
            });
        }

        public void SetRoomId(int roomId)
        {
            _roomId = roomId;
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                // Load room + house
                var room = await _db.Rooms
                    .Include(r => r.House)
                    .FirstOrDefaultAsync(r => r.IdRoom == _roomId);

                // Mock fallback if not found (optional for first run)
                if (room == null)
                {
                    room = new Room
                    {
                        IdRoom = _roomId,
                        HouseID = 1,
                        RoomCode = $"R-{_roomId}",
                        RoomStatus = Room.Status.Available,
                        Area = 25,
                        Price = 3000000,
                        Notes = "Phòng mẫu (mock).",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        House = new House { IdHouse = 1, Address = "Địa chỉ mẫu (mock)" }
                    };
                }

                Room = room;

                // Current tenant (IsActive + matches RoomId)
                var current = await _db.Tenants
                    .Where(t => t.RoomId == _roomId && t.IsActive)
                    .OrderByDescending(t => t.MoveInDate)
                    .FirstOrDefaultAsync();

                // Mock current tenant (optional)
                // current ??= new Tenant { FullName = "Nguyễn Văn A (mock)", PhoneNumber = "0901234567", IdCardNumber = "0123456789", MonthlyRent = Room?.Price ?? 0, DepositAmount = 1000000, MoveInDate = DateTime.UtcNow.AddMonths(-2), ContractUrl = "https://example.com/contract.pdf", EmergencyContacts = new List<string> { "Mẹ: 0900000001", "Anh trai: 0900000002" }, IsActive = false };

                CurrentTenant = current;

                // History = tenants of this room who are not active
                var history = await _db.Tenants
                    .Where(t => t.RoomId == _roomId && !t.IsActive)
                    .OrderByDescending(t => t.MoveOutDate ?? t.MoveInDate)
                    .ToListAsync();

                TenantHistory = new ObservableCollection<Tenant>(history);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RoomDetail] Load error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Lỗi", "Không thể tải chi tiết phòng.", "OK");
            }
        }

        private async Task MoveOutAsync()
        {
            if (CurrentTenant == null || Room == null) return;

            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "Trả phòng", $"Xác nhận trả phòng cho {CurrentTenant.FullName}?", "Đồng ý", "Hủy");

            if (!confirm) return;

            try
            {
                var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.IdTenant == CurrentTenant.IdTenant);
                var room = await _db.Rooms.FirstOrDefaultAsync(r => r.IdRoom == Room.IdRoom);

                if (tenant != null)
                {
                    tenant.IsActive = false;
                    tenant.MoveOutDate = DateTime.UtcNow;
                    tenant.UpdatedAt = DateTime.UtcNow;
                }

                if (room != null)
                {
                    room.RoomStatus = Models.Room.Status.Available; // business rule: free room on move-out
                    room.UpdatedAt = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();
                await LoadAsync();
                await Application.Current.MainPage.DisplayAlert("Thành công", "Đã trả phòng.", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RoomDetail] MoveOut error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Lỗi", "Không thể trả phòng.", "OK");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}