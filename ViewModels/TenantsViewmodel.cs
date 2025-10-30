using AMS.Data;
using AMS.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace AMS.ViewModels
{
    public class TenantsViewModel : INotifyPropertyChanged
    {
        private readonly AMSDbContext _db;

        private ObservableCollection<Tenant> _tenants = new();
        private string _searchText = string.Empty;
        private string _selectedStatus = "Tất cả";
        private bool _isRefreshing;

        public ObservableCollection<Tenant> Tenants
        {
            get => _tenants;
            set { _tenants = value; OnPropertyChanged(); }
        }

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); }
        }

        public IReadOnlyList<string> StatusOptions { get; } = new[] { "Tất cả", "Hoạt động", "Ngừng" };

        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (_selectedStatus != value)
                {
                    _selectedStatus = value;
                    OnPropertyChanged();
                    _ = LoadTenantsAsync();
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
        public ICommand ClearFilterCommand { get; }
        public ICommand AddTenantCommand { get; }
        public ICommand EditTenantCommand { get; }
        public ICommand DeactivateCommand { get; }
        public ICommand ActivateCommand { get; }
        public ICommand SoftDeleteCommand { get; }

        public TenantsViewModel(AMSDbContext db)
        {
            _db = db;

            RefreshCommand = new Command(async () => await LoadTenantsAsync());
            SearchCommand = new Command(async () => await LoadTenantsAsync());
            ClearFilterCommand = new Command(async () =>
            {
                SearchText = string.Empty;
                SelectedStatus = "Tất cả";
                await LoadTenantsAsync();
            });

            AddTenantCommand = new Command(async () => await Shell.Current.GoToAsync("edittenant"));
            EditTenantCommand = new Command<Tenant>(async (tenant) =>
            {
                if (tenant == null) return;
                await Shell.Current.GoToAsync($"edittenant?tenantId={tenant.IdTenant}");
            });

            DeactivateCommand = new Command<Tenant>(async (tenant) => await SetActiveAsync(tenant, false));
            ActivateCommand = new Command<Tenant>(async (tenant) => await SetActiveAsync(tenant, true));
            SoftDeleteCommand = new Command<Tenant>(async (tenant) =>
            {
                // Soft delete = set Inactive; we keep history
                await SetActiveAsync(tenant, false, askConfirm: true);
            });

            _ = LoadTenantsAsync();
        }

        private async Task LoadTenantsAsync()
        {
            try
            {
                IsRefreshing = true;

                // IMPORTANT: prevent stale tracked entities
                _db.ChangeTracker.Clear();

                IQueryable<Tenant> query = _db.Tenants.AsNoTracking();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var keyword = SearchText.Trim();
                    query = query.Where(t =>
                        (t.FullName != null && EF.Functions.Like(t.FullName, $"%{keyword}%")) ||
                        (t.PhoneNumber != null && EF.Functions.Like(t.PhoneNumber, $"%{keyword}%")) ||
                        (t.Email != null && EF.Functions.Like(t.Email, $"%{keyword}%")) ||
                        (t.IdCardNumber != null && EF.Functions.Like(t.IdCardNumber, $"%{keyword}%"))
                    );
                }

                if (SelectedStatus == "Hoạt động")
                    query = query.Where(t => t.IsActive);
                else if (SelectedStatus == "Ngừng")
                    query = query.Where(t => !t.IsActive);

                var items = await query
                    .OrderByDescending(t => t.UpdatedAt)
                    .ThenByDescending(t => t.IdTenant)
                    .ToListAsync();

                // Force ItemsSource rebind so CollectionView updates immediately
                Tenants = new ObservableCollection<Tenant>(items);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Tenants] Load error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlertAsync("Lỗi", "Không thể tải danh sách người thuê.", "OK");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private async Task SetActiveAsync(Tenant? tenant, bool active, bool askConfirm = false)
        {
            if (tenant == null) return;

            if (!active)
            {
                // Prevent deactivating if tenant is currently assigned to any room (active occupancy)
                bool inAnyActiveRoom = await _db.RoomOccupancies
                    .AsNoTracking()
                    .AnyAsync(o => o.TenantId == tenant.IdTenant && o.MoveOutDate == null);

                if (inAnyActiveRoom)
                {
                    await Application.Current.MainPage.DisplayAlertAsync("Không thể ngừng",
                        "Người thuê đang ở một phòng. Hãy trả phòng trước.", "OK");
                    return;
                }
            }

            if (askConfirm)
            {
                bool confirm = await Application.Current.MainPage.DisplayAlertAsync(
                    active ? "Kích hoạt" : "Ngừng hoạt động",
                    active
                        ? $"Kích hoạt lại người thuê: {tenant.FullName}?"
                        : $"Ngừng hoạt động người thuê: {tenant.FullName}?",
                    active ? "Kích hoạt" : "Ngừng",
                    "Hủy");
                if (!confirm) return;
            }

            try
            {
                var entity = await _db.Tenants.FirstOrDefaultAsync(t => t.IdTenant == tenant.IdTenant);
                if (entity == null)
                {
                    await Application.Current.MainPage.DisplayAlertAsync("Lỗi", "Không tìm thấy người thuê.", "OK");
                    return;
                }

                entity.IsActive = active;
                entity.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                // Update current item for instant UI feedback
                tenant.IsActive = active;
                tenant.UpdatedAt = entity.UpdatedAt;

                // Optional: fully reload list to reflect sorting by UpdatedAt
                await LoadTenantsAsync();

                await Application.Current.MainPage.DisplayAlertAsync("Thành công",
                    active ? "Đã kích hoạt." : "Đã ngừng hoạt động.", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Tenants] SetActive error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlertAsync("Lỗi", "Không thể cập nhật trạng thái.", "OK");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}