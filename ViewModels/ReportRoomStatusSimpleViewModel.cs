using AMS.Models;
using AMS.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
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
            finally
            {
                IsBusy = false;
            }
        }

        private Task ExportPdfAsync() => Task.CompletedTask;
        private Task ExportExcelAsync() => Task.CompletedTask;
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