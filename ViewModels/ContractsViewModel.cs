using AMS.Models;
using AMS.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AMS.ViewModels
{
    public partial class ContractsViewModel : ObservableObject
    {
        private readonly IContractAddendumService _addendumService;
        private readonly IContractPdfService _pdfService;
        private readonly IEmailNotificationService _emailNotify;

        private readonly IContractsRepository _repo;
        private readonly IRoomsRepository _roomsRepository;
        private readonly IRoomOccupancyProvider _occupancyProvider;

        [ObservableProperty] private ObservableCollection<Contract> items = new();
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string searchText = "";
        [ObservableProperty] private string selectedStatusFilter = "Tất cả";

        public IReadOnlyList<string> StatusFilterOptions { get; } =
            new[] { "Tất cả", "Bản nháp", "Hiệu lực", "Hết hạn", "Chấm dứt", "Cần phụ lục" };

        public IAsyncRelayCommand<Contract> AddendumCommand { get; }
        public IAsyncRelayCommand<Contract> HistoryCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }
        public IAsyncRelayCommand CreateCommand { get; }
        public IAsyncRelayCommand<Contract> EditCommand { get; }
        public IAsyncRelayCommand<Contract> DeleteCommand { get; }
        public IAsyncRelayCommand<Contract> GeneratePdfCommand { get; }
        public IAsyncRelayCommand<Contract> SendEmailCommand { get; }
        public IRelayCommand ClearFilterCommand { get; }

        public ContractsViewModel(IContractsRepository repo,
            IRoomsRepository roomsRepository,
            IRoomOccupancyProvider occupancyProvider,
            IContractAddendumService addendumService,
            IContractPdfService contractPdfService,
            IEmailNotificationService email)
        {
            _addendumService = addendumService;
            _emailNotify = email;
            _pdfService = contractPdfService;
            _repo = repo;
            _roomsRepository = roomsRepository;
            _occupancyProvider = occupancyProvider;

            AddendumCommand = new AsyncRelayCommand<Contract>(HandleAddendumAsync);
            HistoryCommand = new AsyncRelayCommand<Contract>(ShowHistoryAsync);
            RefreshCommand = new AsyncRelayCommand(LoadAsync);
            CreateCommand = new AsyncRelayCommand(CreateFromRoomAsync);
            EditCommand = new AsyncRelayCommand<Contract>(EditAsync);
            DeleteCommand = new AsyncRelayCommand<Contract>(DeleteAsync);
            GeneratePdfCommand = new AsyncRelayCommand<Contract>(GeneratePdfAsync);
            SendEmailCommand = new AsyncRelayCommand<Contract>(SendEmailAsync);
            ClearFilterCommand = new RelayCommand(() =>
            {
                SearchText = "";
                SelectedStatusFilter = "Tất cả";
                _ = LoadAsync();
            });
        }

        partial void OnSelectedStatusFilterChanged(string value) => _ = LoadAsync();

        public async Task LoadAsync()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                var all = await _repo.GetAllAsync();
                IEnumerable<Contract> filtered = all;

                if (SelectedStatusFilter != "Tất cả")
                {
                    if (SelectedStatusFilter == "Cần phụ lục")
                        filtered = filtered.Where(c => c.NeedsAddendum);
                    else
                    {
                        var st = SelectedStatusFilter switch
                        {
                            "Bản nháp" => ContractStatus.Draft,
                            "Hiệu lực" => ContractStatus.Active,
                            "Hết hạn" => ContractStatus.Expired,
                            "Chấm dứt" => ContractStatus.Terminated,
                            _ => (ContractStatus?)null
                        };
                        if (st.HasValue) filtered = filtered.Where(c => c.Status == st.Value);
                    }
                }

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var k = SearchText.Trim().ToLowerInvariant();
                    filtered = filtered.Where(c =>
                        (c.ContractNumber?.ToLowerInvariant().Contains(k) ?? false) ||
                        (c.RoomCode?.ToLowerInvariant().Contains(k) ?? false) ||
                        (c.HouseAddress?.ToLowerInvariant().Contains(k) ?? false) ||
                        (c.PropertyDescription?.ToLowerInvariant().Contains(k) ?? false) ||
                        c.Tenants.Any(t =>
                            (t.Name?.ToLowerInvariant().Contains(k) ?? false) ||
                            (t.Email?.ToLowerInvariant().Contains(k) ?? false) ||
                            (t.Phone?.ToLowerInvariant().Contains(k) ?? false))
                    );
                }

                Items = new ObservableCollection<Contract>(filtered
                    .OrderByDescending(c => c.Status == ContractStatus.Active)
                    .ThenBy(c => c.EndDate)
                    .ToList());
            }
            finally
            {
                IsBusy = false;
            }
        }

        private Task EditAsync(Contract? c)
        {
            if (c == null) return Task.CompletedTask;
            return Shell.Current.GoToAsync("editcontract", new Dictionary<string, object>
            {
                ["Contract"] = c,
                ["readonly"] = false
            });
        }

        private async Task CreateFromRoomAsync()
        {
            var availableRooms = await _roomsRepository.GetAvailableRoomsForContractAsync(DateTime.Today);
            var candidates = availableRooms
                .Where(r => r.RoomOccupancies != null && r.RoomOccupancies.Any(o => o.MoveOutDate == null))
                .ToList();

            if (candidates.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync("Không có phòng", "Không có phòng trống để tạo hợp đồng.", "OK");
                return;
            }

            var labels = candidates.Select(r =>
                string.IsNullOrWhiteSpace(r.House?.Address)
                    ? r.RoomCode
                    : $"{r.RoomCode} — {r.House?.Address}").ToArray();

            var chosen = await Shell.Current.DisplayActionSheet("Chọn phòng", "Hủy", null, labels);
            if (string.IsNullOrEmpty(chosen) || chosen == "Hủy") return;

            var picked = candidates[Array.IndexOf(labels, chosen)];

            var occ = await _occupancyProvider.GetTenantsForRoomAsync(picked.RoomCode);
            var tenants = occ.Select(o => new ContractTenant { Name = o.Name, Email = o.Email, Phone = o.Phone }).ToList();

            var draft = new Contract
            {
                Status = ContractStatus.Draft,
                RoomCode = picked.RoomCode,
                HouseAddress = picked.House?.Address ?? "",
                Tenants = tenants,
                RentAmount = picked.Price,
                MaxOccupants = picked.MaxOccupants,
                MaxBikeAllowance = picked.MaxBikeAllowance,
                PropertyDescription = $"{picked.House?.Address} • Phòng {picked.RoomCode}",
                SecurityDeposit = 0m,
                DueDay = 5
            };

            await Shell.Current.GoToAsync("editcontract", new Dictionary<string, object>
            {
                ["Contract"] = draft,
                ["readonly"] = false
            });
        }

        private async Task DeleteAsync(Contract? c)
        {
            if (c == null) return;
            if ((c.Status != ContractStatus.Terminated) && (c.Status != ContractStatus.Draft))
            {
                await Shell.Current.DisplayAlertAsync("Không thể xóa", "Chỉ được xóa khi hợp đồng đã chấm dứt.", "OK");
                return;
            }
            var ok = await Shell.Current.DisplayAlertAsync("Xóa hợp đồng",
                $"Xóa hợp đồng {c.ContractNumber ?? c.ContractId}?", "Xóa", "Hủy");
            if (!ok) return;
            await _repo.DeleteAsync(c.ContractId);
            await LoadAsync();
        }

        private async Task GeneratePdfAsync(Contract? c)
        {
            if (c == null) return;
            await Shell.Current.DisplayAlertAsync("PDF", "Tạo PDF (chưa triển khai).", "OK");
        }

        private async Task SendEmailAsync(Contract? c)
        {
            if (c == null) return;
            await Shell.Current.DisplayAlertAsync("Email", "Gửi email (chưa triển khai).", "OK");
        }

        private async Task HandleAddendumAsync(Contract? c)
        {
            if (c == null) return;
            if (c.Status != ContractStatus.Active || !c.NeedsAddendum)
            {
                await Shell.Current.DisplayAlertAsync("Phụ lục", "Hợp đồng không cần phụ lục.", "OK");
                return;
            }

            await Shell.Current.GoToAsync("editcontract", new Dictionary<string, object>
            {
                ["Contract"] = c,
                ["readonly"] = false
            });
        }

        private async Task ShowHistoryAsync(Contract? c)
        {
            if (c == null) return;

            var adds = await _addendumService.GetAddendumsAsync(c.ContractId);
            var items = new List<string> { "Hợp đồng gốc" };
            items.AddRange(adds.Select(a => a.AddendumNumber ?? a.AddendumId));

            var choice = await Shell.Current.DisplayActionSheet("Lịch sử hợp đồng", "Đóng", null, items.ToArray());
            if (string.IsNullOrEmpty(choice) || choice == "Đóng") return;

            Contract preview;
            if (choice == "Hợp đồng gốc")
            {
                var first = adds.OrderBy(a => a.CreatedAt).FirstOrDefault();
                var originSnap = first?.OldSnapshot ?? ToSnapshot(c);
                preview = MakePreviewFrom(originSnap, " (Gốc)");
            }
            else
            {
                var selected = adds.First(a => (a.AddendumNumber ?? a.AddendumId) == choice);
                preview = MakePreviewFrom(selected.NewSnapshot, $" / {selected.AddendumNumber}");
            }

            await Shell.Current.GoToAsync("editcontract", new Dictionary<string, object>
            {
                ["Contract"] = preview,
                ["readonly"] = true
            });
        }

        private static ContractSnapshot ToSnapshot(Contract c)
        {
            return new ContractSnapshot
            {
                ContractNumber = c.ContractNumber ?? "",
                RoomCode = c.RoomCode,
                HouseAddress = c.HouseAddress,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                DueDay = c.DueDay,
                RentAmount = c.RentAmount,
                SecurityDeposit = c.SecurityDeposit,
                DepositReturnDays = c.DepositReturnDays,
                MaxOccupants = c.MaxOccupants,
                MaxBikeAllowance = c.MaxBikeAllowance,
                PaymentMethods = c.PaymentMethods,
                LateFeePolicy = c.LateFeePolicy,
                PropertyDescription = c.PropertyDescription,
                Tenants = c.Tenants?.ToList() ?? new(),
                PdfUrl = c.PdfUrl
            };
        }

        private static Contract MakePreviewFrom(ContractSnapshot snap, string? suffix)
        {
            return new Contract
            {
                ContractId = Guid.NewGuid().ToString("N"),
                ContractNumber = string.IsNullOrWhiteSpace(suffix) ? snap.ContractNumber : $"{snap.ContractNumber}{suffix}",
                RoomCode = snap.RoomCode,
                HouseAddress = snap.HouseAddress,
                StartDate = snap.StartDate,
                EndDate = snap.EndDate,
                RentAmount = snap.RentAmount,
                DueDay = snap.DueDay,
                PaymentMethods = snap.PaymentMethods,
                LateFeePolicy = snap.LateFeePolicy,
                SecurityDeposit = snap.SecurityDeposit,
                DepositReturnDays = snap.DepositReturnDays,
                MaxOccupants = snap.MaxOccupants,
                MaxBikeAllowance = snap.MaxBikeAllowance,
                Tenants = snap.Tenants?.ToList() ?? new(),
                Status = ContractStatus.Active,
                PdfUrl = snap.PdfUrl
            };
        }
    }
}