using AMS.Models;
using AMS.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMS.ViewModels
{
    public partial class ReportRoomStatusSimpleViewModel : ObservableObject
    {
        private readonly IRoomsRepository _roomsRepo;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string selectedStatusFilter = "Tất cả";
        [ObservableProperty] private ObservableCollection<RoomStatusDisplayRow> rooms = new();

        public IReadOnlyList<string> StatusFilterOptions { get; } = new[]
        {
            "Tất cả",
            "Available",
            "Occupied",
            "Maintaining",
            "Inactive"
        };

        public IAsyncRelayCommand LoadCommand { get; }
        public IAsyncRelayCommand ExportPdfCommand { get; }
        public IAsyncRelayCommand ExportExcelCommand { get; }

        public ReportRoomStatusSimpleViewModel(IRoomsRepository roomsRepo)
        {
            _roomsRepo = roomsRepo;
            LoadCommand = new AsyncRelayCommand(LoadAsync);
            ExportPdfCommand = new AsyncRelayCommand(ExportPdfAsync);
            ExportExcelCommand = new AsyncRelayCommand(ExportExcelAsync);
        }

        partial void OnSelectedStatusFilterChanged(string value) => _ = LoadAsync();

        private async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                Rooms.Clear();
                var list = await _roomsRepo.GetAllAsync(includeInactive: true);

                var filtered = list.AsEnumerable();
                if (SelectedStatusFilter != "Tất cả")
                {
                    filtered = filtered.Where(r => r.RoomStatus.ToString().Equals(SelectedStatusFilter, StringComparison.OrdinalIgnoreCase));
                }

                foreach (var r in filtered.OrderBy(r => r.RoomCode))
                    Rooms.Add(new RoomStatusDisplayRow(r));
            }
            finally { IsBusy = false; }
        }

        private async Task ExportPdfAsync()
        {
            if (IsBusy) return;
            if (Rooms.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync("Xuất PDF", "Không có dữ liệu để xuất.", "OK");
                return;
            }
            try
            {
                var folder = Path.Combine(FileSystem.AppDataDirectory, "reports");
                Directory.CreateDirectory(folder);
                var fileName = $"RoomStatus_{DateTime.Today:yyyyMM}.pdf";
                var path = Path.Combine(folder, fileName);

                var sb = new StringBuilder();
                sb.AppendLine("BÁO CÁO TRẠNG THÁI PHÒNG");
                sb.AppendLine($"Lọc: {SelectedStatusFilter}");
                sb.AppendLine(new string('-', 50));
                sb.AppendLine($"{"Phòng",-14} {"Trạng thái",-16}");
                sb.AppendLine(new string('-', 50));
                foreach (var r in Rooms.OrderBy(x => x.RoomCode))
                    sb.AppendLine($"{r.RoomCode,-14} {r.StatusText,-16}");

                // Summary counts
                var total = Rooms.Count;
                var byGroup = Rooms.GroupBy(x => x.StatusText)
                                   .Select(g => $"{g.Key}: {g.Count()}").ToList();
                sb.AppendLine(new string('-', 50));
                sb.AppendLine($"Tổng: {total}");
                foreach (var line in byGroup) sb.AppendLine(line);

                await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
                await Shell.Current.DisplayAlertAsync("Đã xuất PDF", $"Đã lưu: {fileName}\nThư mục: {folder}", "OK");
                try { await Launcher.OpenAsync(new OpenFileRequest(fileName, new ReadOnlyFile(path))); } catch { }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi xuất PDF", ex.Message, "OK");
            }
        }

        private async Task ExportExcelAsync()
        {
            if (IsBusy) return;
            if (Rooms.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync("Xuất Excel", "Không có dữ liệu để xuất.", "OK");
                return;
            }
            try
            {
                var folder = Path.Combine(FileSystem.AppDataDirectory, "reports");
                Directory.CreateDirectory(folder);
                var fileName = $"RoomStatus_{DateTime.Today:yyyyMM}.csv";
                var path = Path.Combine(folder, fileName);

                var sb = new StringBuilder();
                sb.AppendLine("# Báo cáo trạng thái phòng");
                sb.AppendLine($"# Lọc: {SelectedStatusFilter}");
                sb.AppendLine("RoomCode,Status");

                foreach (var r in Rooms.OrderBy(x => x.RoomCode))
                    sb.AppendLine($"{r.RoomCode},{r.StatusText}");

                // Summary as comment lines
                var total = Rooms.Count;
                var byGroup = Rooms.GroupBy(x => x.StatusText)
                                   .Select(g => $"{g.Key}:{g.Count()}");
                sb.AppendLine("# Tổng: " + total);
                foreach (var line in byGroup) sb.AppendLine("# " + line);

                await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
                await Shell.Current.DisplayAlertAsync("Đã xuất Excel", $"Đã lưu: {fileName}\nThư mục: {folder}", "OK");
                try { await Launcher.OpenAsync(new OpenFileRequest(fileName, new ReadOnlyFile(path))); } catch { }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi xuất Excel", ex.Message, "OK");
            }
        }
    }

    public class RoomStatusDisplayRow
    {
        private readonly Room _room;
        public string RoomCode => _room.RoomCode;
        public string StatusText => _room.RoomStatus switch
        {
            Room.Status.Available => "Available",
            Room.Status.Occupied => "Occupied",
            Room.Status.Maintaining => "Maintaining",
            Room.Status.Inactive => "Inactive",
            _ => _room.RoomStatus.ToString()
        };
        public string StatusColor => _room.RoomStatus switch
        {
            Room.Status.Available => "#C8E6C9",
            Room.Status.Occupied => "#BBDEFB",
            Room.Status.Maintaining => "#FFE0B2",
            Room.Status.Inactive => "#E0E0E0",
            _ => "#E0E0E0"
        };
        public RoomStatusDisplayRow(Room r) => _room = r;
    }
}