using AMS.Data;
using AMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Storage;
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
        private string? _emergencyContactName;
        private string? _emergencyContactPhone;

        public Room? Room { get => _room; private set { _room = value; OnPropertyChanged(); } }

        public ObservableCollection<RoomOccupancy> ActiveOccupancies { get; } = new();
        public ObservableCollection<Bike> Bikes { get; } = new();

        public string? EmergencyContactName { get => _emergencyContactName; private set { _emergencyContactName = value; OnPropertyChanged(); } }
        public string? EmergencyContactPhone { get => _emergencyContactPhone; private set { _emergencyContactPhone = value; OnPropertyChanged(); } }

        public ICommand RefreshCommand { get; }
        public ICommand AddTenantCommand { get; }
        public ICommand RemoveTenantCommand { get; }
        public ICommand ChooseEmergencyContactCommand { get; }
        public ICommand AddBikeCommand { get; }
        public ICommand RemoveBikeCommand { get; }
        public ICommand CallPhoneCommand { get; }

        public RoomDetailViewModel(AMSDbContext db)
        {
            _db = db;

            RefreshCommand = new Command(async () => await LoadAsync());
            AddTenantCommand = new Command(async () => await AddTenantAsync());
            RemoveTenantCommand = new Command<RoomOccupancy>(async (occ) => await RemoveTenantAsync(occ));
            ChooseEmergencyContactCommand = new Command(async () => await ChooseEmergencyContactAsync());
            AddBikeCommand = new Command(async () => await AddBikeAsync());
            RemoveBikeCommand = new Command<Bike>(async (b) => await RemoveBikeAsync(b));
            CallPhoneCommand = new Command<string>(async (phone) =>
            {
                if (string.IsNullOrWhiteSpace(phone)) return;
                try { await Launcher.OpenAsync($"tel:{phone}"); } catch { }
            });
        }

        public void SetRoomId(int roomId)
        {
            _roomId = roomId;
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            if (_roomId <= 0) return;

            try
            {
                // Load room + current occupancies + tenants
                var room = await _db.Rooms
                    .Include(r => r.RoomOccupancies!)
                        .ThenInclude(ro => ro.Tenant)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.IdRoom == _roomId);

                Room = room;

                // Active occupancies
                ActiveOccupancies.Clear();
                if (room?.RoomOccupancies != null)
                {
                    foreach (var occ in room.RoomOccupancies
                                            .Where(ro => ro.MoveOutDate == null && ro.Tenant != null)
                                            .OrderBy(ro => ro.MoveInDate))
                    {
                        ActiveOccupancies.Add(occ);
                    }
                }

                // Bikes with owner tenant included (for binding OwnerTenant.FullName)
                Bikes.Clear();
                var bikes = await _db.Bikes
                    .AsNoTracking()
                    .Include(b => b.OwnerTenant)
                    .Where(b => b.RoomId == _roomId && b.IsActive)
                    .OrderBy(b => b.CreatedAt)
                    .ToListAsync();
                foreach (var b in bikes) Bikes.Add(b);

                // Emergency contact: from preference (or from Room if you later add nullable FK)
                var key = $"room:{_roomId}:emergencyContactOccId";
                if (Preferences.ContainsKey(key))
                {
                    var occId = Preferences.Get(key, 0);
                    var occ = ActiveOccupancies.FirstOrDefault(o => o.IdRoomOccupancy == occId) ?? ActiveOccupancies.FirstOrDefault();
                    UpdateEmergencyFromOccupancy(occ);
                }
                else
                {
                    UpdateEmergencyFromOccupancy(ActiveOccupancies.FirstOrDefault());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RoomDetail] Load error: {ex.Message}");
                await Shell.Current.DisplayAlertAsync("Lỗi", "Không thể tải chi tiết phòng.", "OK");
            }
        }

        private void UpdateEmergencyFromOccupancy(RoomOccupancy? occ)
        {
            EmergencyContactName = occ?.Tenant?.FullName;
            EmergencyContactPhone = occ?.Tenant?.PhoneNumber;
        }

        private async Task AddTenantAsync()
        {
            if (Room == null)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi", "Không tìm thấy phòng.", "OK");
                return;
            }
            if (ActiveOccupancies.Count >= Room.MaxOccupants)
            {
                await Shell.Current.DisplayAlertAsync("Vượt quá số người tối đa",
                    $"Phòng này chỉ cho phép tối đa {Room.MaxOccupants} người.", "OK");
                return;
            }

            try
            {
                var candidates = await _db.Tenants
                    .AsNoTracking()
                    .Where(t => !_db.RoomOccupancies.Any(ro => ro.TenantId == t.IdTenant && ro.MoveOutDate == null))
                    .OrderBy(t => t.FullName)
                    .Select(t => new { t.IdTenant, t.FullName, t.PhoneNumber })
                    .ToListAsync();

                if (candidates.Count == 0)
                {
                    await Shell.Current.DisplayAlertAsync("Thông báo", "Không còn người thuê trống để gán.", "OK");
                    return;
                }

                var labels = candidates.Select(c => $"{c.FullName} ({c.PhoneNumber})").ToArray();
                var choice = await Shell.Current.DisplayActionSheet("Chọn người thuê", "Hủy", null, labels);
                if (string.IsNullOrEmpty(choice) || choice == "Hủy") return;

                var idx = Array.IndexOf(labels, choice);
                if (idx < 0) return;
                var picked = candidates[idx];

                var occ = new RoomOccupancy
                {
                    RoomId = _roomId,
                    TenantId = picked.IdTenant,
                    MoveInDate = DateTime.Today,
                    MoveOutDate = null
                };
                _db.RoomOccupancies.Add(occ);
                await _db.SaveChangesAsync();

                await LoadAsync();
                await Shell.Current.DisplayAlertAsync("Thành công", "Đã thêm người thuê vào phòng.", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RoomDetail] AddTenant error: {ex.Message}");
                await Shell.Current.DisplayAlertAsync("Lỗi", "Không thể thêm người thuê.", "OK");
            }
        }

        // Replace your current RemoveTenantAsync with this version
        private async Task RemoveTenantAsync(RoomOccupancy? occ)
        {
            if (occ == null) return;
            if (occ.MoveOutDate != null)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi", "Bản ghi thuê đã kết thúc.", "OK");
                return;
            }

            bool confirm = await Shell.Current.DisplayAlertAsync("Xác nhận",
                $"Gỡ {occ.Tenant?.FullName} khỏi phòng?\nLưu ý: Tất cả xe máy của người thuê này trong phòng sẽ bị xóa.",
                "Gỡ", "Hủy");
            if (!confirm) return;

            try
            {
                await using var tx = await _db.Database.BeginTransactionAsync();

                var tracked = await _db.RoomOccupancies
                    .FirstOrDefaultAsync(ro => ro.IdRoomOccupancy == occ.IdRoomOccupancy);

                if (tracked == null)
                {
                    await Shell.Current.DisplayAlertAsync("Lỗi", "Không tìm thấy bản ghi thuê.", "OK");
                    return;
                }

                var tenantId = tracked.TenantId;
                var roomId = tracked.RoomId;

                // 1) Delete all bikes of this tenant in this room
                // If you're on EF Core 7+, ExecuteDeleteAsync is available:
                await _db.Bikes
                    .Where(b => b.RoomId == roomId && b.OwnerId == tenantId)
                    .ExecuteDeleteAsync();

                // If your EF version doesn't support ExecuteDeleteAsync, use:
                // var bikes = await _db.Bikes.Where(b => b.RoomId == roomId && b.OwnerId == tenantId).ToListAsync();
                // _db.Bikes.RemoveRange(bikes);
                // await _db.SaveChangesAsync();

                // 2) Set BikeCount to 0 for this (now-ended) occupancy and mark MoveOut
                tracked.BikeCount = 0;
                tracked.MoveOutDate = DateTime.Today;

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                // Clear emergency contact preference if it pointed to this occupancy
                var key = $"room:{_roomId}:emergencyContactOccId";
                if (Preferences.ContainsKey(key) && Preferences.Get(key, 0) == occ.IdRoomOccupancy)
                    Preferences.Remove(key);

                // Refresh UI
                await LoadAsync();
                await Shell.Current.DisplayAlertAsync("Thành công", "Đã gỡ người thuê và xóa xe máy liên quan.", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RoomDetail] RemoveTenant error: {ex.Message}");
                await Shell.Current.DisplayAlertAsync("Lỗi", "Không thể gỡ người thuê.", "OK");
            }
        }

        private async Task ChooseEmergencyContactAsync()
        {
            if (ActiveOccupancies.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync("Thông báo", "Chưa có người thuê để chọn.", "OK");
                return;
            }

            var labels = ActiveOccupancies.Select(o => $"{o.Tenant!.FullName} ({o.Tenant!.PhoneNumber})").ToArray();
            var choice = await Shell.Current.DisplayActionSheet("Chọn người liên hệ", "Hủy", null, labels);
            if (string.IsNullOrEmpty(choice) || choice == "Hủy") return;

            var idx = Array.IndexOf(labels, choice);
            if (idx < 0) return;

            var picked = ActiveOccupancies[idx];
            UpdateEmergencyFromOccupancy(picked);

            // Persist preference (or later set Room.EmergencyContactRoomOccupancyId)
            Preferences.Set($"room:{_roomId}:emergencyContactOccId", picked.IdRoomOccupancy);
        }

   
        private async Task AddBikeAsync()
        {
            if (_roomId <= 0) return;

            // Enforce MaxBikeAllowance (0 => no limit)
            var max = Room?.MaxBikeAllowance ?? 0;
            if (max > 0)
            {
                var current = await _db.Bikes
                    .AsNoTracking()
                    .Where(b => b.RoomId == _roomId && b.IsActive)
                    .CountAsync();

                if (current >= max)
                {
                    await Shell.Current.DisplayAlertAsync("Vượt quá số xe tối đa",
                        $"Phòng này chỉ cho phép tối đa {max} xe.", "OK");
                    return;
                }
            }

            if (ActiveOccupancies.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync("Thông báo", "Cần có người thuê hiện tại để gán chủ xe.", "OK");
                return;
            }

            var plate = await Shell.Current.DisplayPromptAsync("Thêm xe", "Biển số:", "Lưu", "Hủy", maxLength: 32);
            if (string.IsNullOrWhiteSpace(plate)) return;
            var normalizedPlate = plate.Trim().ToUpperInvariant();

            var labels = ActiveOccupancies
                .Select(o => $"{o.Tenant!.FullName} ({o.Tenant!.PhoneNumber})")
                .ToArray();
            var choice = await Shell.Current.DisplayActionSheet("Chọn chủ sở hữu", "Hủy", null, labels);
            if (string.IsNullOrEmpty(choice) || choice == "Hủy") return;

            var idx = Array.IndexOf(labels, choice);
            if (idx < 0) return;
            var ownerOcc = ActiveOccupancies[idx];
            var ownerTenantId = ownerOcc.TenantId;

            try
            {
                var bike = new Bike
                {
                    RoomId = _roomId,
                    Plate = normalizedPlate,
                    OwnerId = ownerTenantId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Add(bike);
                await _db.SaveChangesAsync();

                // Recalc BikeCount on the active occupancy for that owner
                var occ = await _db.RoomOccupancies
                    .FirstOrDefaultAsync(ro => ro.RoomId == _roomId
                                            && ro.TenantId == ownerTenantId
                                            && ro.MoveOutDate == null);
                if (occ != null)
                {
                    occ.BikeCount = await _db.Bikes
                        .Where(b => b.RoomId == _roomId && b.OwnerId == ownerTenantId && b.IsActive)
                        .CountAsync();
                    await _db.SaveChangesAsync();
                }

                await LoadAsync();
            }
            catch (DbUpdateException)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi", "Biển số đã tồn tại trong phòng này.", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RoomDetail] AddBike error: {ex.Message}");
                await Shell.Current.DisplayAlertAsync("Lỗi", "Không thể thêm xe.", "OK");
            }
        }
        private async Task RemoveBikeAsync(Bike? bike)
        {
            if (bike == null) return;
            bool confirm = await Shell.Current.DisplayAlertAsync("Xóa xe", $"Xóa xe {bike.Plate}?", "Xóa", "Hủy");
            if (!confirm) return;

            try
            {
                var tracked = await _db.Bikes.FirstOrDefaultAsync(b => b.Id == bike.Id);
                if (tracked != null)
                {
                    var roomId = tracked.RoomId;
                    var ownerId = tracked.OwnerId;

                    _db.Remove(tracked);
                    await _db.SaveChangesAsync();

                    // Recalc BikeCount for the occupancy if we know owner
                    if (ownerId.HasValue)
                    {
                        var occ = await _db.RoomOccupancies
                            .FirstOrDefaultAsync(ro => ro.RoomId == roomId
                                                    && ro.TenantId == ownerId.Value
                                                    && ro.MoveOutDate == null);
                        if (occ != null)
                        {
                            occ.BikeCount = await _db.Bikes
                                .Where(b => b.RoomId == roomId && b.OwnerId == ownerId.Value && b.IsActive)
                                .CountAsync();
                            await _db.SaveChangesAsync();
                        }
                    }
                }

                await LoadAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RoomDetail] RemoveBike error: {ex.Message}");
                await Shell.Current.DisplayAlertAsync("Lỗi", "Không thể xóa xe.", "OK");
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}