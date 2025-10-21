using AMS.Data;
using AMS.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace AMS.ViewModels
{
    public class HouseEditViewModel : INotifyPropertyChanged
    {
        private readonly AMSDbContext _db;

        private int _id;
        private string? _Address;
        private int _totalRooms;
        private string? _notes;
        private DateTime _createdAt;
        private DateTime _updatedAt;
        private House? _currentHouse;  // Cache tracked entity for updates

        public string PageTitle => _id == 0 ? "Thêm nhà" : "Sửa nhà";
        public string CreatedUpdatedInfo =>
            _id == 0 ? "" : $"Tạo: {_createdAt:yyyy-MM-dd HH:mm} | Cập nhật: {_updatedAt:yyyy-MM-dd HH:mm}";

        public int Id { get => _id; set { _id = value; OnPropertyChanged(); OnPropertyChanged(nameof(PageTitle)); } }
        public string? Address { get => _Address; set { _Address = value; OnPropertyChanged(); } }
        public int TotalRooms { get => _totalRooms; set { _totalRooms = value; OnPropertyChanged(); } }
        public string? Notes { get => _notes; set { _notes = value; OnPropertyChanged(); } }
        public DateTime CreatedAt { get => _createdAt; set { _createdAt = value; OnPropertyChanged(); OnPropertyChanged(nameof(CreatedUpdatedInfo)); } }
        public DateTime UpdatedAt { get => _updatedAt; set { _updatedAt = value; OnPropertyChanged(); OnPropertyChanged(nameof(CreatedUpdatedInfo)); } }  // FIXED: Proper call + ;

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public HouseEditViewModel(AMSDbContext db)
        {
            _db = db;
            _currentHouse = null;
            SaveCommand = new Command(async () => await SaveAsync());
            CancelCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        }

        public void SetHouseId(int id)
        {
            Id = id;
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            if (Id <= 0) return;

            try
            {
                _currentHouse = await _db.Houses.FirstOrDefaultAsync(h => h.IdHouse == Id);
                if (_currentHouse != null)
                {
                    Address = _currentHouse.Address;
                    TotalRooms = _currentHouse.TotalRooms;
                    Notes = _currentHouse.Notes;
                    CreatedAt = _currentHouse.CreatedAt;
                    UpdatedAt = _currentHouse.UpdatedAt;
                    System.Diagnostics.Debug.WriteLine($"[HouseEdit] Loaded house {Id}: {_currentHouse.Address}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HouseEdit] Load error: {ex.Message}");
            }
        }

        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Address))
            {
                await Application.Current.MainPage.DisplayAlertAsync("Thiếu thông tin", "Vui lòng nhập địa chỉ.", "OK");
                return;
            }

            try
            {
                var trimmedAddress = Address?.Trim();

                // NEW: Optional uniqueness check (prevents dups)
                bool addressExists = Id > 0 ? await _db.Houses.AnyAsync(h => h.Address == trimmedAddress && h.IdHouse != Id) : await _db.Houses.AnyAsync(h => h.Address == trimmedAddress);
                if (addressExists)
                {
                    await Application.Current.MainPage.DisplayAlertAsync("Trùng địa chỉ", "Địa chỉ này đã tồn tại.", "OK");
                    return;
                }

                if (Id == 0)  // Add new
                {
                    var entity = new House
                    {
                        Address = trimmedAddress,
                        TotalRooms = TotalRooms,
                        Notes = Notes,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _db.Houses.Add(entity);
                    await _db.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine($"[HouseEdit] Added new house: {entity.Address} (ID: {entity.IdHouse})");
                }
                else  // Update existing
                {
                    if (_currentHouse == null)
                    {
                        await Application.Current.MainPage.DisplayAlertAsync("Lỗi", "Không tìm thấy nhà để cập nhật.", "OK");
                        return;
                    }

                    // Update cached tracked entity
                    _currentHouse.Address = trimmedAddress;
                    _currentHouse.TotalRooms = TotalRooms;
                    _currentHouse.Notes = Notes;
                    _currentHouse.UpdatedAt = DateTime.UtcNow;

                    System.Diagnostics.Debug.WriteLine($"[HouseEdit] Updating house {Id}: New Address={_currentHouse.Address}, TotalRooms={_currentHouse.TotalRooms}");

                    await _db.SaveChangesAsync();

                    // Verify post-save
                    var verified = await _db.Houses.FirstOrDefaultAsync(h => h.IdHouse == Id);
                    System.Diagnostics.Debug.WriteLine($"[HouseEdit] Post-save verify: Address={verified?.Address}, UpdatedAt={verified?.UpdatedAt}");
                }

                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HouseEdit] Save error: {ex.Message}\nStack: {ex.StackTrace}");
                await Application.Current.MainPage.DisplayAlertAsync("Lỗi", $"Không thể lưu nhà: {ex.Message}", "OK");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}