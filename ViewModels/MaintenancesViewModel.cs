using AMS.Models;
using AMS.Services;
using AMS.Services.Interfaces;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AMS.ViewModels
{
    public class MaintenancesViewModel : INotifyPropertyChanged
    {
        private readonly IMaintenanceSheetReader _fileReader;
        private readonly IOnlineMaintenanceReader _onlineReader;
        private readonly IMaintenanceSheetWriter _writer;
        private readonly IRoomsProvider _roomsProvider;

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
        public ICommand CreateRequestCommand { get; }
        public ICommand DeleteRequestCommand { get; }
        public ICommand SyncRoomsCommand { get; }   // NEW

        public string? SheetUrl
        {
            get => _sheetUrl;
            set { _sheetUrl = value; OnPropertyChanged(); }
        }

        public MaintenancesViewModel(
            IMaintenanceSheetReader fileReader,
            IOnlineMaintenanceReader onlineReader,
            IMaintenanceSheetWriter writer,
            IRoomsProvider roomsProvider
            )
        {
            _fileReader = fileReader;
            _onlineReader = onlineReader;
            _writer = writer;
            _roomsProvider = roomsProvider;
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

            CreateRequestCommand = new Command(async () => await CreateAsync());
            DeleteRequestCommand = new Command<MaintenanceRequest>(async item => await DeleteAsync(item));

            // NEW: sync rooms button
            SyncRoomsCommand = new Command(async () => await SyncRoomsAsync());

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
                await Shell.Current.DisplayAlertAsync("URL không hợp lệ", "Hãy dán liên kết chuẩn của Google Sheets.", "OK");
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
                Debug.WriteLine($"[Maintenance] Pick error: {ex.Message}");
                await Shell.Current.DisplayAlertAsync("Lỗi", "Không thể chọn file.", "OK");
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
                        Debug.WriteLine($"[Maintenance] URL load failed: {ex.Message}");
                        await Shell.Current.DisplayAlertAsync("Lỗi tải từ URL", $"{ex.Message}", "OK");
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

                if (SelectedStatus != "Tất cả" && TryParseStatusVi(SelectedStatus, out var st))
                    filtered = filtered.Where(m => m.Status == st);

                var items = filtered
                    .OrderByDescending(m => m.CreatedDate)
                    .ThenByDescending(m => GetPriorityScore(m.Priority))
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

        private static int GetPriorityScore(string priority)
        {
            var p = (priority ?? "").Trim().ToLowerInvariant();
            return p switch
            {
                "high" or "cao" => 3,
                "medium" or "trung bình" or "trung binh" => 2,
                "low" or "thấp" or "thap" => 1,
                _ => 0
            };
        }

        private async Task UpdateAsync(MaintenanceRequest? item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.RequestId))
            {
                await Shell.Current.DisplayAlertAsync("Thiếu RequestId", "Dòng này chưa có RequestId.", "OK");
                return;
            }

            var status = await Shell.Current.DisplayActionSheet("Trạng thái", "Hủy", null,
                "Chưa xử lý", "Đang xử lý", "Đã xử lý");
            if (string.IsNullOrEmpty(status) || status == "Hủy") return;

            var priorityVi = await Shell.Current.DisplayActionSheet("Ưu tiên", "Bỏ qua", null, "Thấp", "Trung bình", "Cao");

            var costStr = await Shell.Current.DisplayPromptAsync("Chi phí",
                "Nhập chi phí (để trống nếu không đổi)",
                "Lưu", "Bỏ qua", keyboard: Keyboard.Numeric);

            var values = new Dictionary<string, object> { ["Trạng thái"] = status };
            if (!string.IsNullOrEmpty(priorityVi) && priorityVi != "Bỏ qua")
                values["Mức độ ưu tiên"] = priorityVi;

            if (!string.IsNullOrWhiteSpace(costStr))
            {
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
                Debug.WriteLine(ex);
                await Shell.Current.DisplayAlertAsync("Lỗi", "Không cập nhật được Google Sheet.", "OK");
            }
        }

        private async Task CreateAsync()
        {
            var room = await Shell.Current.DisplayPromptAsync("Mã phòng", "Nhập Mã phòng (chọn từ Rooms)", "Tiếp", "Hủy");
            if (string.IsNullOrWhiteSpace(room)) return;

            var category = await Shell.Current.DisplayPromptAsync("Phân loại", "Nhập phân loại (hoặc để trống)", "Tiếp", "Bỏ qua");
            var desc = await Shell.Current.DisplayPromptAsync("Miêu tả", "Nhập mô tả sự cố", "Tạo", "Hủy");
            if (string.IsNullOrWhiteSpace(desc)) return;

            var priority = await Shell.Current.DisplayActionSheet("Ưu tiên", "Bỏ qua", null, "Thấp", "Trung bình", "Cao");

            var values = new Dictionary<string, object>
            {
                ["Mã phòng"] = room.Trim(),
                ["Phân loại"] = category ?? "",
                ["Miêu tả"] = desc.Trim()
            };
            if (!string.IsNullOrEmpty(priority) && priority != "Bỏ qua") values["Mức độ ưu tiên"] = priority;

            try
            {
                var id = await _writer.CreateAsync(values);
                await LoadAsync();
                await Shell.Current.DisplayAlertAsync("Thành công", $"Đã tạo yêu cầu: {id}", "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await Shell.Current.DisplayAlertAsync("Lỗi", "Không tạo được yêu cầu.", "OK");
            }
        }

        private async Task DeleteAsync(MaintenanceRequest? item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.RequestId)) return;
            var confirm = await Shell.Current.DisplayAlertAsync("Xóa yêu cầu", $"Xóa ID {item.RequestId}?", "Xóa", "Hủy");
            if (!confirm) return;

            try
            {
                await _writer.DeleteAsync(item.RequestId!);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await Shell.Current.DisplayAlertAsync("Lỗi", "Không xóa được yêu cầu.", "OK");
            }
        }

        // ========== Rooms sync ==========

        private async Task SyncRoomsAsync()
        {
            try
            {
                IsRefreshing = true;

                var rooms = await GetRoomsFromDbAsync();
                if (rooms == null || rooms.Count == 0)
                {
                    await Shell.Current.DisplayAlertAsync("Không có dữ liệu", "Chưa có phòng để đồng bộ.", "OK");
                    return;
                }

                await _writer.SyncRoomsAsync(rooms);
                await Shell.Current.DisplayAlertAsync("Thành công", $"Đã đồng bộ {rooms.Count} phòng.", "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await Shell.Current.DisplayAlertAsync("Lỗi", "Đồng bộ phòng thất bại.", "OK");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private async Task<IReadOnlyList<RoomInfo>> GetRoomsFromDbAsync()
        {
            // Include inactive so the sheet knows ALL RoomCodes; the script hides inactive via Active=false.
            return await _roomsProvider.GetRoomsAsync(includeInactive: true);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}