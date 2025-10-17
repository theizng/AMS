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
        private string? _diaChi;
        private int _totalRooms;
        private string? _notes;
        private DateTime _createdAt;
        private DateTime _updatedAt;

        public string PageTitle => _id == 0 ? "Thêm nhà" : "Sửa nhà";
        public string CreatedUpdatedInfo =>
            _id == 0 ? "" : $"Tạo: {_createdAt:yyyy-MM-dd HH:mm} | Cập nhật: {_updatedAt:yyyy-MM-dd HH:mm}";

        public int Id { get => _id; set { _id = value; OnPropertyChanged(); OnPropertyChanged(nameof(PageTitle)); } }
        public string? DiaChi { get => _diaChi; set { _diaChi = value; OnPropertyChanged(); } }
        public int TotalRooms { get => _totalRooms; set { _totalRooms = value; OnPropertyChanged(); } }
        public string? Notes { get => _notes; set { _notes = value; OnPropertyChanged(); } }
        public DateTime CreatedAt { get => _createdAt; set { _createdAt = value; OnPropertyChanged(); OnPropertyChanged(nameof(CreatedUpdatedInfo)); } }
        public DateTime UpdatedAt { get => _updatedAt; set { _updatedAt = value; OnPropertyChanged(); OnPropertyChanged(nameof(CreatedUpdatedInfo)); } }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public HouseEditViewModel(AMSDbContext db)
        {
            _db = db;
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

            var entity = await _db.Nhas.FirstOrDefaultAsync(h => h.Id == Id);
            if (entity != null)
            {
                DiaChi = entity.DiaChi;
                TotalRooms = entity.TotalRooms;
                Notes = entity.Notes;
                CreatedAt = entity.CreatedAt;
                UpdatedAt = entity.UpdatedAt;
            }
        }

        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(DiaChi))
            {
                await Application.Current.MainPage.DisplayAlertAsync("Thiếu thông tin", "Vui lòng nhập địa chỉ.", "OK");
                return;
            }

            try
            {
                if (Id == 0)
                {
                    var entity = new Nha
                    {
                        DiaChi = DiaChi?.Trim(),
                        TotalRooms = TotalRooms,
                        Notes = Notes,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _db.Nhas.Add(entity);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    var entity = await _db.Nhas.FirstOrDefaultAsync(h => h.Id == Id);
                    if (entity == null)
                    {
                        await Application.Current.MainPage.DisplayAlertAsync("Lỗi", "Không tìm thấy nhà.", "OK");
                        return;
                    }

                    entity.DiaChi = DiaChi?.Trim();
                    entity.TotalRooms = TotalRooms;
                    entity.Notes = Notes;
                    entity.UpdatedAt = DateTime.UtcNow;

                    await _db.SaveChangesAsync();
                }

                await Shell.Current.GoToAsync(".."); // back
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EditHouse] Save error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlertAsync("Lỗi", "Không thể lưu nhà.", "OK");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}