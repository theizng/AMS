using AMS.Data;
using AMS.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace AMS.ViewModels
{
    public class HousesViewModel : INotifyPropertyChanged
    {
        private readonly AMSDbContext _dbContext;

        private ObservableCollection<House> _houses = new();
        private string _searchText = string.Empty;
        private bool _isRefreshing;

        public ObservableCollection<House> Houses
        {
            get => _houses;
            set { _houses = value; OnPropertyChanged(); }
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

        // Commands
        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand AddHouseCommand { get; }
        public ICommand EditHouseCommand { get; }
        public ICommand DeleteHouseCommand { get; }
        public ICommand ViewRoomsCommand { get; }

        public HousesViewModel(AMSDbContext dbContext)
        {
            _dbContext = dbContext;

            RefreshCommand = new Command(async () => await LoadHousesAsync());
            SearchCommand = new Command(async () => await LoadHousesAsync());
            AddHouseCommand = new Command(async () => await Shell.Current.GoToAsync("edithouse"));
            EditHouseCommand = new Command<House>(async (house) =>
            {
                if (house == null) return;
                await Shell.Current.GoToAsync($"edithouse?houseId={house.IdHouse}");
            });
            DeleteHouseCommand = new Command<House>(async (house) => await DeleteHouseAsync(house));
            ViewRoomsCommand = new Command<House>(async (house) =>
            {
                if (house == null) return;
                await Shell.Current.GoToAsync($"rooms?houseId={house.IdHouse}");
            });

            _ = LoadHousesAsync();
        }

        private async Task LoadHousesAsync()
        {
            try
            {
                IsRefreshing = true;

                var query = _dbContext.Houses.AsQueryable();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var keyword = SearchText.Trim();
                    query = query.Where(h =>
                        (h.Address != null && EF.Functions.Like(h.Address, $"%{keyword}%")) ||
                        (h.Notes != null && EF.Functions.Like(h.Notes, $"%{keyword}%"))
                    );
                }

                var items = await query
                    .OrderByDescending(h => h.UpdatedAt)
                    .ThenByDescending(h => h.IdHouse)
                    .ToListAsync();

                Houses = new ObservableCollection<House>(items);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Houses] Load error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlertAsync("Lỗi", "Không thể tải danh sách nhà.", "OK");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private async Task DeleteHouseAsync(House house)
        {
            if (house == null) return;

            bool confirm = await Application.Current.MainPage.DisplayAlertAsync(
                "Xóa nhà",
                $"Bạn chắc chắn muốn xóa nhà tại:\n\"{house.Address}\"?\nLưu ý: có thể ảnh hưởng đến các phòng liên quan.",
                "Xóa",
                "Hủy"
            );

            if (!confirm) return;

            try
            {
                _dbContext.Houses.Remove(house);
                await _dbContext.SaveChangesAsync();

                Houses.Remove(house);

                await Application.Current.MainPage.DisplayAlertAsync("Thành công", "Đã xóa nhà.", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Houses] Delete error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlertAsync("Lỗi", "Không thể xóa nhà.", "OK");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}