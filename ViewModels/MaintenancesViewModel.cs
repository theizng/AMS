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
        private readonly IMaintenanceSheetWriter _writer;

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

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); }
        }

        // Vietnamese status options
        public IReadOnlyList<string> StatusOptions { get; } =
            new[] { "Tất cả", "Chưa xử lý", "Đang xử lý", "Đã xử lý", "Đã hủy" };

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

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set { _isRefreshing = value; OnPropertyChanged(); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ClearFilterCommand { get; }
        public ICommand ImportSheetCommand { get; }
        public ICommand ImportUrlCommand { get; }
        public ICommand UpdateRequestCommand { get; }
        public ICommand PhoneTapCommand { get; }

        public string? SheetUrl
        {
            get => _sheetUrl;
            set { _sheetUrl = value; OnPropertyChanged(); }
        }

        public MaintenancesViewModel(
            IMaintenanceSheetReader fileReader,
            IOnlineMaintenanceReader onlineReader,
            IMaintenanceSheetWriter writer)
        {
            _fileReader = fileReader;
            _onlineReader = onlineReader;
            _writer = writer;

            RefreshCommand = new Command(async () => await LoadAsync());
            SearchCommand = new Command(async () => await LoadAsync());
            ClearFilterCommand = new Command(async () =>
            {
                SearchText = "";
                SelectedStatus = "Tất cả";
                await LoadAsync();
            });
            ImportSheetCommand = new Command(async () => await PickAndLoadAsync());
            ImportUrlCommand = new Command(async () => await PromptAndSaveUrlAsync());
            UpdateRequestCommand = new Command<MaintenanceRequest>(async item => await UpdateAsync(item));
            PhoneTapCommand = new Command<string?>(phone =>
            {
                try { if (!string.IsNullOrWhiteSpace(phone)) PhoneDialer.Open(phone); }
                catch { }
            });

            _sheetPath = Preferences.Get("maintenance:sheet:path", null);
            _sheetUrl = Preferences.Get("maintenance:sheet:url", null);

            _ = LoadAsync();
        }

        private async Task PromptAndSaveUrlAsync()
        {
            var input = await Shell.Current.DisplayPromptAsync(
                "Nhập URL Google Sheet",
                "Dán liên kết Google Sheet (chia sẻ 'Bất kỳ ai có liên kết')",
                "Lưu", "Hủy",
                placeholder: "https://docs.google.com/spreadsheets/d/....",
                maxLength: 500);

            if (string.IsNullOrWhiteSpace(input)) return;

            if (!GoogleSheetUrlHelper.TryBuildExportXlsxUrl(input, out var normalized))
            {
                await Shell.Current.DisplayAlert("URL không hợp lệ", "Hãy dán liên kết chuẩn của Google Sheets.", "OK");
                return;
            }

            SheetUrl = normalized;
            Preferences.Set("maintenance:sheet:url", SheetUrl);
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

                var url = Preferences.Get("maintenance:sheet:url", _sheetUrl ?? "");
                if (!string.IsNullOrWhiteSpace(url))
                {
                    try { all = await _onlineReader.ReadFromUrlAsync(url); }
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
                        all = await _fileReader.ReadAsync(path);
                    else
                    {
                        Items = new ObservableCollection<MaintenanceRequest>();
                        return;
                    }
                }

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

                if (SelectedStatus != "Tất cả")
                {
                    if (TryParseStatusVi(SelectedStatus, out var st))
                        filtered = filtered.Where(m => m.Status == st);
                }

                var items = filtered
                    .OrderByDescending(m => m.CreatedDate)
                    .ThenByDescending(m => m.Priority) // High/Medium/Low textual order; OK for now
                    .ThenBy(m => m.HouseAddress)
                    .ThenBy(m => m.RoomCode)
                    .ToList();

                Items = new ObservableCollection<MaintenanceRequest>(items);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private static bool TryParseStatusVi(string vi, out MaintenanceStatus status)
        {
            status = MaintenanceStatus.New;
            switch (vi.Trim())
            {
                case "Chưa xử lý": status = MaintenanceStatus.New; return true;
                case "Đang xử lý": status = MaintenanceStatus.InProgress; return true;
                case "Đã xử lý": status = MaintenanceStatus.Done; return true;
                case "Đã hủy": status = MaintenanceStatus.Cancelled; return true;
                default: return false;
            }
        }

        private async Task UpdateAsync(MaintenanceRequest? item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.RequestId))
            {
                await Shell.Current.DisplayAlert("Thiếu RequestId", "Dòng này chưa có RequestId.", "OK");
                return;
            }

            var status = await Shell.Current.DisplayActionSheet("Trạng thái", "Hủy", null,
                "Chưa xử lý", "Đang xử lý", "Đã xử lý");
            if (string.IsNullOrEmpty(status) || status == "Hủy") return;

            var priorityVi = await Shell.Current.DisplayActionSheet("Ưu tiên", "Bỏ qua", null, "Thấp", "Trung bình", "Cao");

            var costStr = await Shell.Current.DisplayPromptAsync("Chi phí",
                "Nhập chi phí (để trống nếu không đổi)",
                "Lưu", "Bỏ qua", keyboard: Keyboard.Numeric);

            // Build values to send to sheet (Vietnamese headers/values)
            var values = new Dictionary<string, object> { ["Trạng thái"] = status };
            if (!string.IsNullOrEmpty(priorityVi) && priorityVi != "Bỏ qua")
                values["Mức độ ưu tiên"] = priorityVi;

            if (!string.IsNullOrWhiteSpace(costStr))
            {
                // Normalize thousands/decimal separators to a plain number
                var digits = new string(costStr.Where(ch => char.IsDigit(ch) || ch == '.' || ch == ',').ToArray());
                if (decimal.TryParse(digits.Replace(",", ""), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var cost))
                    values["Chi phí (nếu có)"] = cost;
            }

            try
            {
                await _writer.UpdateAsync(item.RequestId!, values);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                await Shell.Current.DisplayAlertAsync("Lỗi", "Không cập nhật được Google Sheet.", "OK");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}