using AMS.Data;
using AMS.Models;
using AMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace AMS.ViewModels
{
    public class RoomsViewModel : INotifyPropertyChanged
    {
        private readonly AMSDbContext _db;
        private readonly IContractRoomGuard _contractRoomGuard; // injected

        private int _houseId;
        private ObservableCollection<Room> _rooms = new();
        private string _searchText = string.Empty;
        private string _selectedStatusText = "Tất cả";
        private bool _isRefreshing;

        public ObservableCollection<Room> Rooms
        {
            get => _rooms;
            set { _rooms = value; OnPropertyChanged(); }
        }

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); }
        }

        public IReadOnlyList<string> StatusOptions { get; } =
            new[] { "Tất cả", nameof(Room.Status.Available), nameof(Room.Status.Occupied), nameof(Room.Status.Maintaining), nameof(Room.Status.Inactive) };

        public string SelectedStatusText
        {
            get => _selectedStatusText;
            set
            {
                var newVal = string.IsNullOrWhiteSpace(value) ? "Tất cả" : value;
                if (_selectedStatusText != newVal)
                {
                    _selectedStatusText = newVal;
                    OnPropertyChanged();
                    _ = LoadRoomsAsync();
                }
            }
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set { _isRefreshing = value; OnPropertyChanged(); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ApplyFilterCommand { get; }
        public ICommand ClearFilterCommand { get; }
        public ICommand AddRoomCommand { get; }
        public ICommand EditRoomCommand { get; }
        public ICommand DeleteRoomCommand { get; }
        public ICommand ViewDetailCommand { get; }

        // Constructor now injects guard
        public RoomsViewModel(AMSDbContext db, IContractRoomGuard contractRoomGuard)
        {
            _db = db;
            _contractRoomGuard = contractRoomGuard;

            RefreshCommand = new Command(async () => await LoadRoomsAsync());
            SearchCommand = new Command(async () => await LoadRoomsAsync());
            ApplyFilterCommand = new Command(async () => await LoadRoomsAsync());
            ClearFilterCommand = new Command(async () =>
            {
                SearchText = string.Empty;
                SelectedStatusText = "Tất cả";
                await LoadRoomsAsync();
            });

            AddRoomCommand = new Command(async () =>
            {
                if (_houseId <= 0)
                {
                    await Shell.Current.DisplayAlert("Thiếu thông tin", "Thiếu HouseId.", "OK");
                    return;
                }
                await Shell.Current.GoToAsync($"editroom?houseId={_houseId}");
            });

            EditRoomCommand = new Command<Room>(async room =>
            {
                if (room == null) return;

                // Guard is guaranteed not null now
                var editable = await _contractRoomGuard.CanEditRoomAsync(room.RoomCode);
                if (!editable)
                {
                    await Shell.Current.DisplayAlert("Không thể sửa", "Phòng đang thuộc hợp đồng có hiệu lực. Sửa thông qua phụ lục hợp đồng.", "OK");
                    return;
                }

                await Shell.Current.GoToAsync($"editroom?roomId={room.IdRoom}&houseId={_houseId}");
            });

            DeleteRoomCommand = new Command<Room>(async (room) => await DeleteRoomAsync(room));

            ViewDetailCommand = new Command<Room>(async (room) =>
            {
                if (room == null) return;
                await Shell.Current.GoToAsync($"/detailroom?roomId={room.IdRoom}");
            });
        }

        public void SetHouseId(int houseId)
        {
            _houseId = houseId;
            _ = LoadRoomsAsync();
        }

        private Room.Status? SelectedStatusFilter =>
            SelectedStatusText == "Tất cả" ? (Room.Status?)null : Enum.Parse<Room.Status>(SelectedStatusText);

        private async Task LoadRoomsAsync()
        {
            try
            {
                IsRefreshing = true;
                _db.ChangeTracker.Clear();

                IQueryable<Room> query = _db.Rooms.AsNoTracking().Include(r => r.House);

                if (_houseId > 0)
                    query = query.Where(r => r.HouseID == _houseId);

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var keyword = SearchText.Trim();
                    query = query.Where(r =>
                        (r.RoomCode != null && EF.Functions.Like(r.RoomCode, $"%{keyword}%")) ||
                        (r.Notes != null && EF.Functions.Like(r.Notes, $"%{keyword}%"))
                    );
                }

                var status = SelectedStatusFilter;
                if (status.HasValue)
                    query = query.Where(r => r.RoomStatus == status.Value);

                var items = await query
                    .OrderBy(r => r.RoomStatus)
                    .ThenBy(r => r.RoomCode)
                    .ToListAsync();

                var ids = items.Select(r => r.IdRoom).ToList();
                var counts = await _db.RoomOccupancies
                    .AsNoTracking()
                    .Where(o => ids.Contains(o.RoomId) && o.MoveOutDate == null)
                    .GroupBy(o => o.RoomId)
                    .Select(g => new { RoomId = g.Key, Count = g.Count() })
                    .ToListAsync();

                var map = counts.ToDictionary(x => x.RoomId, x => x.Count);
                foreach (var r in items)
                    r.ActiveOccupants = map.TryGetValue(r.IdRoom, out var c) ? c : 0;

                Rooms = new ObservableCollection<Room>(items);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Rooms] Load error: {ex.Message}");
                await Shell.Current.DisplayAlert("Lỗi", "Không thể tải danh sách phòng.", "OK");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private async Task DeleteRoomAsync(Room room)
        {
            if (room == null) return;

            bool confirm = await Shell.Current.DisplayAlert(
                "Xóa phòng",
                $"Bạn chắc chắn muốn xóa phòng:\n\"{room.RoomCode}\"?",
                "Xóa",
                "Hủy"
            );
            if (!confirm) return;

            try
            {
                var tracked = await _db.Rooms.FindAsync(room.IdRoom) ?? new Room { IdRoom = room.IdRoom };
                _db.Attach(tracked);
                _db.Remove(tracked);
                await _db.SaveChangesAsync();

                var idx = Rooms.IndexOf(room);
                if (idx >= 0) Rooms.RemoveAt(idx);

                await Shell.Current.DisplayAlert("Thành công", "Đã xóa phòng.", "OK");
            }
            catch (InvalidOperationException)
            {
                _db.ChangeTracker.Clear();
                var stub = new Room { IdRoom = room.IdRoom };
                _db.Entry(stub).State = EntityState.Deleted;
                await _db.SaveChangesAsync();

                var idx = Rooms.IndexOf(room);
                if (idx >= 0) Rooms.RemoveAt(idx);

                await Shell.Current.DisplayAlert("Thành công", "Đã xóa phòng.", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Rooms] Delete error: {ex.Message}");
                await Shell.Current.DisplayAlert("Lỗi", "Không thể xóa phòng.", "OK");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}