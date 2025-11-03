using AMS.Models;
using AMS.Services;
using AMS.Services.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace AMS.ViewModels
{
    public class MaintenancesViewModel : INotifyPropertyChanged
    {
        private readonly IMaintenanceSheetReader _fileReader;
        private readonly IOnlineMaintenanceReader _onlineReader;

        private ObservableCollection<MaintenanceRequest> _items = new();
        private string _searchText = "";
        private string _selectedStatus = "Tất cả";
        private bool _isRefreshing;
        private string? _sheetPath;
        private string? _sheetUrl;

        public ObservableCollection<MaintenanceRequest> Items
        {
            get => _items;
            set { _items = value; OnPropertyChanged(); }
        }

        public string SearchText { get => _searchText; set { _searchText = value; OnPropertyChanged(); } }
        public IReadOnlyList<string> StatusOptions { get; } = new[] { "Tất cả", "New", "InProgress", "Done", "Cancelled" };
        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (_selectedStatus != value)
                {
                    _selectedStatus = value;
                    OnPropertyChanged();
                    _ = LoadAsync();
                }
            }
        }

        public bool IsRefreshing { get => _isRefreshing; set { _isRefreshing = value; OnPropertyChanged(); } }

        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ClearFilterCommand { get; }
        public ICommand ImportSheetCommand { get; }
        public ICommand ImportUrlCommand { get; } // NEW

        public string? SheetUrl
        {
            get => _sheetUrl;
            set { _sheetUrl = value; OnPropertyChanged(); }
        }

        public MaintenancesViewModel(IMaintenanceSheetReader fileReader, IOnlineMaintenanceReader onlineReader)
        {
            _fileReader = fileReader;
            _onlineReader = onlineReader;

            RefreshCommand = new Command(async () => await LoadAsync());
            SearchCommand = new Command(async () => await LoadAsync());
            ClearFilterCommand = new Command(async () =>
            {
                SearchText = "";
                SelectedStatus = "Tất cả";
                await LoadAsync();
            });
            ImportSheetCommand = new Command(async () => await PickAndLoadAsync());
            ImportUrlCommand = new Command(async () => await PromptAndSaveUrlAsync()); // NEW

            _sheetPath = Preferences.Get("maintenance:sheet:path", null);
            _sheetUrl = Preferences.Get("maintenance:sheet:url", null);

            _ = LoadAsync();
        }

        private async Task PromptAndSaveUrlAsync()
        {
            var input = await Shell.Current.DisplayPromptAsync(
                "Nhập URL Google Sheet",
                "Dán liên kết Google Sheet (đặt chia sẻ 'Anyone with the link' hoặc 'Publish to web')",
                "Lưu", "Hủy",
                placeholder: "https://docs.google.com/spreadsheets/d/....",
                maxLength: 500);
            if (string.IsNullOrWhiteSpace(input)) return;

            if (!GoogleSheetUrlHelper.TryBuildExportXlsxUrl(input, out var normalized))
            {
                await Shell.Current.DisplayAlert("URL không hợp lệ", "Hãy dán liên kết chuẩn của Google Sheets.", "OK");
                return;
            }

            SheetUrl = normalized; // save normalized export xlsx URL
            Preferences.Set("maintenance:sheet:url", SheetUrl);

            // Prefer URL over local file if both exist
            await LoadAsync();
        }

        private async Task PickAndLoadAsync()
        {
            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Chọn file Excel (.xlsx)",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.Android, new [] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" } },
                        { DevicePlatform.iOS, new [] { "com.microsoft.excel.xlsx" } },
                        { DevicePlatform.WinUI, new [] { ".xlsx" } }
                    })
                });

                if (result == null) return;

                _sheetPath = result.FullPath;
                Preferences.Set("maintenance:sheet:path", _sheetPath);

                // If you pick a file, we can clear URL to avoid confusion
                SheetUrl = null;
                Preferences.Remove("maintenance:sheet:url");

                await LoadAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Maintenance] Pick error: {ex.Message}");
                await Shell.Current.DisplayAlert("Lỗi", "Không thể chọn file.", "OK");
            }
        }

        private async Task LoadAsync()
        {
            try
            {
                IsRefreshing = true;

                IReadOnlyList<MaintenanceRequest> all;

                // Prefer URL if set
                var url = Preferences.Get("maintenance:sheet:url", _sheetUrl ?? "");
                if (!string.IsNullOrWhiteSpace(url))
                {
                    try
                    {
                        all = await _onlineReader.ReadFromUrlAsync(url);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Maintenance] URL load failed: {ex.Message}");
                        await Shell.Current.DisplayAlert("Lỗi tải từ URL", $"{ex.Message}", "OK");
                        all = Array.Empty<MaintenanceRequest>();
                    }
                }
                else
                {
                    var path = Preferences.Get("maintenance:sheet:path", _sheetPath ?? "");
                    if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    {
                        all = await _fileReader.ReadAsync(path);
                    }
                    else
                    {
                        Items = new ObservableCollection<MaintenanceRequest>();
                        return;
                    }
                }

                // Filter by search and status
                IEnumerable<MaintenanceRequest> filtered = all;

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var k = SearchText.Trim().ToLowerInvariant();
                    filtered = filtered.Where(m =>
                        (m.HouseAddress?.ToLowerInvariant().Contains(k) ?? false) ||
                        (m.RoomCode?.ToLowerInvariant().Contains(k) ?? false) ||
                        (m.Category?.ToLowerInvariant().Contains(k) ?? false) ||
                        (m.Description?.ToLowerInvariant().Contains(k) ?? false) ||
                        (m.TenantName?.ToLowerInvariant().Contains(k) ?? false) ||
                        (m.TenantPhone?.ToLowerInvariant().Contains(k) ?? false));
                }

                if (SelectedStatus != "Tất cả" && Enum.TryParse<MaintenanceStatus>(SelectedStatus, out var st))
                    filtered = filtered.Where(m => m.Status == st);

                var items = filtered
                    .OrderByDescending(m => m.CreatedDate)
                    .ThenByDescending(m => m.Priority)
                    .ThenBy(m => m.HouseAddress)
                    .ThenBy(m => m.RoomCode)
                    .ToList();

                Items = new ObservableCollection<MaintenanceRequest>(items);
                if (!filtered.Any())
                {
                    // Optional toast/alert to help diagnose header mismatches
                    await Shell.Current.DisplayAlertAsync("Thông báo", "Không đọc được dòng nào từ sheet. Kiểm tra hàng tiêu đề (header).", "OK");
                }
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}