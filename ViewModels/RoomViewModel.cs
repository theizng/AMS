using AMS.Data;
using AMS.Models;
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
                if (_selectedStatusText != value)
                {
                    _selectedStatusText = value;
                    OnPropertyChanged();
                    // If you prefer live filtering, uncomment:
                    // _ = LoadRoomsAsync();
                }
            }
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set { _isRefreshing = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ApplyFilterCommand { get; }
        public ICommand ClearFilterCommand { get; }
        public ICommand AddRoomCommand { get; }
        public ICommand EditRoomCommand { get; }
        public ICommand DeleteRoomCommand { get; }
        public ICommand ChangeStatusCommand { get; }

        public RoomsViewModel(AMSDbContext db)
        {
            _db = db;

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
                    await Application.Current.MainPage.DisplayAlertAsync("Thiếu thông tin", "Thiếu HouseId.", "OK");
                    return;
                }
                await Shell.Current.GoToAsync($"editroom?houseId={_houseId}");
            });

            EditRoomCommand = new Command<Room>(async (room) =>
            {
                if (room == null) return;
                await Shell.Current.GoToAsync($"editroom?roomId={room.IdRoom}&houseId={_houseId}");
            });

            DeleteRoomCommand = new Command<Room>(async (room) => await DeleteRoomAsync(room));

            ChangeStatusCommand = new Command<Room>(async (room) =>
            {
                if (room == null) return;

                var choice = await Application.Current.MainPage.DisplayActionSheet(
                    $"Đổi trạng thái phòng {room.RoomCode}",
                    "Hủy", null,
                    nameof(Room.Status.Available),
                    nameof(Room.Status.Occupied),
                    nameof(Room.Status.Maintaining),
                    nameof(Room.Status.Inactive));

                if (string.IsNullOrEmpty(choice) || choice == "Hủy") return;

                try
                {
                    var newStatus = Enum.Parse<Room.Status>(choice);
                    if (room.RoomStatus != newStatus)
                    {
                        room.RoomStatus = newStatus;
                        room.UpdatedAt = DateTime.UtcNow;
                        await _db.SaveChangesAsync();
                        await LoadRoomsAsync();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Rooms] Change status error: {ex.Message}");
                    await Application.Current.MainPage.DisplayAlertAsync("Lỗi", "Không thể đổi trạng thái phòng.", "OK");
                }
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

                IQueryable<Room> query = _db.Rooms;

                // If houseId provided, filter by house; otherwise show all rooms
                if (_houseId > 0)
                {
                    query = query.Where(r => r.HouseID == _houseId);
                }
                else
                {
                    // Helpful when showing all rooms: include House for display
                    query = query.Include(r => r.House);
                }

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
                {
                    query = query.Where(r => r.RoomStatus == status.Value);
                }

                var items = await query
                    .OrderBy(r => r.RoomStatus)
                    .ThenBy(r => r.RoomCode)
                    .ToListAsync();

                Rooms = new ObservableCollection<Room>(items);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Rooms] Load error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlertAsync("Lỗi", "Không thể tải danh sách phòng.", "OK");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private async Task DeleteRoomAsync(Room room)
        {
            if (room == null) return;

            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "Xóa phòng",
                $"Bạn chắc chắn muốn xóa phòng:\n\"{room.RoomCode}\"?",
                "Xóa",
                "Hủy"
            );

            if (!confirm) return;

            try
            {
                _db.Rooms.Remove(room);
                await _db.SaveChangesAsync();

                Rooms.Remove(room);

                await Application.Current.MainPage.DisplayAlert("Thành công", "Đã xóa phòng.", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Rooms] Delete error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Lỗi", "Không thể xóa phòng.", "OK");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}