using AMS.Data;
using AMS.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace AMS.ViewModels
{
    public class RoomEditViewModel : INotifyPropertyChanged
    {
        private readonly AMSDbContext _db;

        private int _idRoom;
        private int _houseId;
        private string? _roomCode;
        private Room.Status _roomStatus = Room.Status.Available;
        private decimal _area;
        private decimal _price;
        private string? _notes;
        private DateTime _createdAt;
        private DateTime _updatedAt;
        private Room? _currentRoom;  // Cache for updates

        public string PageTitle => _idRoom == 0 ? "Thêm phòng" : "Sửa phòng";
        public string CreatedUpdatedInfo =>
            _idRoom == 0 ? "" : $"Tạo: {_createdAt:yyyy-MM-dd HH:mm} | Cập nhật: {_updatedAt:yyyy-MM-dd HH:mm}";

        public int IdRoom { get => _idRoom; set { _idRoom = value; OnPropertyChanged(); OnPropertyChanged(nameof(PageTitle)); } }
        public int HouseID { get => _houseId; set { _houseId = value; OnPropertyChanged(); } }
        public string? RoomCode { get => _roomCode; set { _roomCode = value; OnPropertyChanged(); } }
        public Room.Status RoomStatus { get => _roomStatus; set { _roomStatus = value; OnPropertyChanged(); } }
        public decimal Area { get => _area; set { _area = value; OnPropertyChanged(); } }
        public decimal Price { get => _price; set { _price = value; OnPropertyChanged(); } }
        public string? Notes { get => _notes; set { _notes = value; OnPropertyChanged(); } }
        public DateTime CreatedAt { get => _createdAt; set { _createdAt = value; OnPropertyChanged(); OnPropertyChanged(nameof(CreatedUpdatedInfo)); } }
        public DateTime UpdatedAt { get => _updatedAt; set { _updatedAt = value; OnPropertyChanged(); OnPropertyChanged(nameof(CreatedUpdatedInfo)); } }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ChangeStatusCommand { get; }  // FIXED: No <Room> param

        public RoomEditViewModel(AMSDbContext db)
        {
            _db = db;
            SaveCommand = new Command(async () => await SaveAsync());
            CancelCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
            ChangeStatusCommand = new Command(async () => await ChangeStatusAsync());  // FIXED: No param, uses VM state
        }

        public void SetHouseId(int houseId)
        {
            HouseID = houseId;
        }

        public void SetRoomId(int roomId)
        {
            IdRoom = roomId;
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            if (IdRoom <= 0) return;

            _currentRoom = await _db.Rooms.FirstOrDefaultAsync(r => r.IdRoom == IdRoom);  // FIXED: Cache tracked
            if (_currentRoom != null)
            {
                HouseID = _currentRoom.HouseID;
                RoomCode = _currentRoom.RoomCode;
                RoomStatus = _currentRoom.RoomStatus;
                Area = _currentRoom.Area;
                Price = _currentRoom.Price;
                Notes = _currentRoom.Notes;
                CreatedAt = _currentRoom.CreatedAt;
                UpdatedAt = _currentRoom.UpdatedAt;
            }
        }

        // NEW: Dedicated status change (uses current VM state)
        private async Task ChangeStatusAsync()
        {
            var currentCode = RoomCode ?? "N/A";
            var choice = await Application.Current.MainPage.DisplayActionSheet(
                $"Đổi trạng thái phòng {currentCode}",
                "Hủy", null,
                nameof(Room.Status.Available),
                nameof(Room.Status.Occupied),
                nameof(Room.Status.Maintaining),
                nameof(Room.Status.Inactive));

            if (string.IsNullOrEmpty(choice) || choice == "Hủy") return;

            try
            {
                var newStatus = Enum.Parse<Room.Status>(choice);
                if (RoomStatus != newStatus)
                {
                    RoomStatus = newStatus;  // Update VM
                    UpdatedAt = DateTime.UtcNow;
                    if (_currentRoom != null)  // Save if editing existing
                    {
                        _currentRoom.RoomStatus = newStatus;
                        _currentRoom.UpdatedAt = DateTime.UtcNow;
                        await _db.SaveChangesAsync();
                    }
                    await Application.Current.MainPage.DisplayAlertAsync("Thành công", $"Đã cập nhật trạng thái: {newStatus}.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EditRoom] Change status error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlertAsync("Lỗi", "Không thể đổi trạng thái phòng.", "OK");
            }
        }

        private async Task SaveAsync()
        {
            if (HouseID <= 0)
            {
                await Application.Current.MainPage.DisplayAlertAsync("Thiếu thông tin", "Thiếu HouseId.", "OK");
                return;
            }
            if (string.IsNullOrWhiteSpace(RoomCode))
            {
                await Application.Current.MainPage.DisplayAlertAsync("Thiếu thông tin", "Vui lòng nhập mã phòng.", "OK");
                return;
            }
            if (Area <= 0)  // FIXED: Removed invalid 'is String'
            {
                await Application.Current.MainPage.DisplayAlertAsync("Giá trị không hợp lệ", "Diện tích phải lớn hơn 0.", "OK");
                return;
            }
            if (Price < 0)  // FIXED: Removed invalid 'is String'
            {
                await Application.Current.MainPage.DisplayAlertAsync("Giá trị không hợp lệ", "Giá thuê không được âm.", "OK");
                return;
            }

            try
            {
                var roomCode = RoomCode!.Trim().ToUpperInvariant();

                bool exists = await _db.Rooms.AnyAsync(r =>
                    r.HouseID == HouseID &&
                    r.RoomCode == roomCode &&
                    r.IdRoom != IdRoom);

                if (exists)
                {
                    await Application.Current.MainPage.DisplayAlertAsync("Trùng mã", "Mã phòng đã tồn tại trong nhà này.", "OK");
                    return;
                }

                if (IdRoom == 0)
                {
                    var entity = new Room
                    {
                        HouseID = HouseID,
                        RoomCode = roomCode,
                        RoomStatus = RoomStatus,
                        Area = Area,
                        Price = Price,
                        Notes = Notes,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _db.Rooms.Add(entity);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    if (_currentRoom == null)
                    {
                        await Application.Current.MainPage.DisplayAlertAsync("Lỗi", "Không tìm thấy phòng.", "OK");
                        return;
                    }

                    // FIXED: Update cached entity
                    _currentRoom.RoomCode = roomCode;
                    _currentRoom.RoomStatus = RoomStatus;
                    _currentRoom.Area = Area;
                    _currentRoom.Price = Price;
                    _currentRoom.Notes = Notes;
                    _currentRoom.UpdatedAt = DateTime.UtcNow;

                    await _db.SaveChangesAsync();
                }

                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EditRoom] Save error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlertAsync("Lỗi", "Không thể lưu phòng.", "OK");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}