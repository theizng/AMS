using AMS.Data;
using AMS.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

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
        private int _maxOccupants = 1;
        private int _freeBikeAllowance = 1;
        private decimal? _bikeExtraFee;
        private string? _bikeExtraFeeText;
        private DateTime _createdAt;
        private DateTime _updatedAt;
        private Room? _currentRoom;

        // NEW: bind to Entry Text="{Binding MaxBikes}" in XAML
        private int _maxBikes = 1;

        public string PageTitle => _idRoom == 0 ? "Thêm phòng" : "Sửa phòng";
        public string CreatedUpdatedInfo =>
            _idRoom == 0 ? "" : $"Tạo: {_createdAt:yyyy-MM-dd HH:mm} | Cập nhật: {_updatedAt:yyyy-MM-dd HH:mm}";

        public int IdRoom { get => _idRoom; set { _idRoom = value; OnPropertyChanged(); OnPropertyChanged(nameof(PageTitle)); } }
        public int HouseID { get => _houseId; set { _houseId = value; OnPropertyChanged(); } }
        public string? RoomCode { get => _roomCode; set { _roomCode = value; OnPropertyChanged(); } }

        public Room.Status RoomStatus
        {
            get => _roomStatus;
            set
            {
                if (_roomStatus != value)
                {
                    _roomStatus = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedStatus));
                }
            }
        }

        public decimal Area { get => _area; set { _area = value; OnPropertyChanged(); } }
        public decimal Price { get => _price; set { _price = value; OnPropertyChanged(); } }
        public string? Notes { get => _notes; set { _notes = value; OnPropertyChanged(); } }

        public int MaxOccupants { get => _maxOccupants; set { _maxOccupants = value; OnPropertyChanged(); } }
        public int FreeBikeAllowance { get => _freeBikeAllowance; set { _freeBikeAllowance = value; OnPropertyChanged(); } }

        public string? BikeExtraFeeText
        {
            get => _bikeExtraFeeText;
            set { _bikeExtraFeeText = value; OnPropertyChanged(); }
        }

        public DateTime CreatedAt { get => _createdAt; set { _createdAt = value; OnPropertyChanged(); OnPropertyChanged(nameof(CreatedUpdatedInfo)); } }
        public DateTime UpdatedAt { get => _updatedAt; set { _updatedAt = value; OnPropertyChanged(); OnPropertyChanged(nameof(CreatedUpdatedInfo)); } }

        public IReadOnlyList<Room.Status> StatusOptions { get; } =
            Enum.GetValues(typeof(Room.Status)).Cast<Room.Status>().ToList();

        public Room.Status SelectedStatus
        {
            get => RoomStatus;
            set { if (RoomStatus != value) RoomStatus = value; }
        }

        // NEW: binds to "Số lượng xe tối đa"
        public int MaxBikes
        {
            get => _maxBikes;
            set { _maxBikes = value; OnPropertyChanged(); }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ChangeStatusCommand { get; }

        public RoomEditViewModel(AMSDbContext db)
        {
            _db = db;
            SaveCommand = new Command(async () => await SaveAsync());
            CancelCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
            ChangeStatusCommand = new Command(async () => await ChangeStatusAsync());
        }

        public void SetHouseId(int houseId) => HouseID = houseId;
        public void SetRoomId(int roomId) { IdRoom = roomId; _ = LoadAsync(); }

        private async Task LoadAsync()
        {
            if (IdRoom <= 0) return;

            _currentRoom = await _db.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.IdRoom == IdRoom);
            if (_currentRoom != null)
            {
                HouseID = _currentRoom.HouseID;
                RoomCode = _currentRoom.RoomCode;
                RoomStatus = _currentRoom.RoomStatus;
                Area = _currentRoom.Area;
                Price = _currentRoom.Price;
                Notes = _currentRoom.Notes;
                MaxOccupants = _currentRoom.MaxOccupants;
                FreeBikeAllowance = _currentRoom.FreeBikeAllowance;
                _bikeExtraFee = _currentRoom.BikeExtraFee;
                BikeExtraFeeText = _bikeExtraFee?.ToString(CultureInfo.InvariantCulture);
                CreatedAt = _currentRoom.CreatedAt;
                UpdatedAt = _currentRoom.UpdatedAt;

                // NEW
                MaxBikes = _currentRoom.MaxBikeAllowance;
            }
            else
            {
                // Defaults for create
                MaxBikes = 1;
            }
        }

        private async Task ChangeStatusAsync()
        {
            var currentCode = RoomCode ?? "N/A";
            var choice = await Shell.Current.DisplayActionSheet(
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
                    RoomStatus = newStatus;
                    UpdatedAt = DateTime.UtcNow;

                    if (IdRoom != 0)
                    {
                        var tracked = await _db.Rooms.FindAsync(IdRoom);
                        if (tracked is not null)
                        {
                            tracked.RoomStatus = newStatus;
                            tracked.UpdatedAt = DateTime.UtcNow;
                            await _db.SaveChangesAsync();
                        }
                    }

                    await Shell.Current.DisplayAlertAsync("Thành công", $"Đã cập nhật trạng thái: {newStatus}.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EditRoom] Change status error: {ex.Message}");
                await Shell.Current.DisplayAlertAsync("Lỗi", "Không thể đổi trạng thái phòng.", "OK");
            }
        }

        private async Task<bool> ValidateAsync()
        {
            if (HouseID <= 0)
                return await Fail("Thiếu thông tin", "Thiếu HouseId.");

            if (string.IsNullOrWhiteSpace(RoomCode))
                return await Fail("Thiếu thông tin", "Vui lòng nhập mã phòng.");

            // Normalize RoomCode
            var normalized = RoomCode.Trim().ToUpperInvariant();
            if (!Regex.IsMatch(normalized, @"^[A-Z0-9_-]+$"))
                return await Fail("Giá trị không hợp lệ", "Mã phòng chỉ được chứa chữ, số, gạch dưới (_) hoặc gạch ngang (-), không dấu cách.");
            RoomCode = normalized;

            if (Area <= 0)
                return await Fail("Giá trị không hợp lệ", "Diện tích phải lớn hơn 0.");

            if (Price < 0)
                return await Fail("Giá trị không hợp lệ", "Giá thuê không được âm.");

            if (MaxOccupants < 1)
                return await Fail("Giá trị không hợp lệ", "Số người tối đa phải >= 1.");

            if (FreeBikeAllowance < 0)
                return await Fail("Giá trị không hợp lệ", "Số xe miễn phí phải >= 0.");

            // NEW: MaxBikes must be >= 0 (0 => không giới hạn nếu bạn muốn)
            if (MaxBikes < 0)
                return await Fail("Giá trị không hợp lệ", "Số lượng xe tối đa phải >= 0.");

            // Optional: clamp FreeBikeAllowance to MaxBikes if MaxBikes > 0
            if (MaxBikes > 0 && FreeBikeAllowance > MaxBikes)
                FreeBikeAllowance = MaxBikes;

            // Parse optional BikeExtraFeeText
            if (string.IsNullOrWhiteSpace(BikeExtraFeeText))
            {
                _bikeExtraFee = null;
            }
            else
            {
                if (!decimal.TryParse(BikeExtraFeeText, NumberStyles.Number, CultureInfo.InvariantCulture, out var fee))
                    return await Fail("Giá trị không hợp lệ", "Phí xe thêm không hợp lệ.");
                if (fee < 0)
                    return await Fail("Giá trị không hợp lệ", "Phí xe thêm không được âm.");
                _bikeExtraFee = fee;
            }

            // Uniqueness per house
            bool exists = await _db.Rooms.AnyAsync(r =>
                r.HouseID == HouseID &&
                r.RoomCode == normalized &&
                r.IdRoom != IdRoom);

            if (exists)
                return await Fail("Trùng mã", "Mã phòng đã tồn tại trong nhà này.");

            return true;
        }

        private static Task<bool> Fail(string title, string message)
            => Shell.Current.DisplayAlertAsync(title, message, "OK").ContinueWith(_ => false);

        private async Task SaveAsync()
        {
            if (!await ValidateAsync()) return;

            try
            {
                var now = DateTime.UtcNow;
                var roomCode = RoomCode!; // normalized already

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
                        MaxOccupants = MaxOccupants,
                        FreeBikeAllowance = FreeBikeAllowance,
                        BikeExtraFee = _bikeExtraFee,
                        CreatedAt = now,
                        UpdatedAt = now,

                        // NEW
                        MaxBikeAllowance = MaxBikes
                    };
                    _db.Rooms.Add(entity);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    var tracked = await _db.Rooms.FindAsync(IdRoom);
                    if (tracked == null)
                    {
                        await Shell.Current.DisplayAlertAsync("Lỗi", "Không tìm thấy phòng.", "OK");
                        return;
                    }

                    tracked.RoomCode = roomCode;
                    tracked.RoomStatus = RoomStatus;
                    tracked.Area = Area;
                    tracked.Price = Price;
                    tracked.Notes = Notes;
                    tracked.MaxOccupants = MaxOccupants;
                    tracked.FreeBikeAllowance = FreeBikeAllowance;
                    tracked.BikeExtraFee = _bikeExtraFee;
                    tracked.UpdatedAt = now;

                    // NEW
                    tracked.MaxBikeAllowance = MaxBikes;

                    await _db.SaveChangesAsync();
                }

                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EditRoom] Save error: {ex.Message}");
                await Shell.Current.DisplayAlertAsync("Lỗi", "Không thể lưu phòng.", "OK");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}