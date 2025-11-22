using AMS.Models;
using AMS.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AMS.ViewModels
{
    public partial class ContractEditViewModel : ObservableObject, IQueryAttributable
    {
        private readonly IPdfCapabilityService _pdfCapability;
        private readonly IContractsRepository _repo;
        private readonly IRoomStatusService _roomStatusService;
        private readonly IRoomOccupancyAdminService _roomOccAdmin;
        private readonly IEmailNotificationService _emailNotify;
        private readonly IContractPdfService _pdfService;
        private readonly IAuthService _auth;

        private readonly IContractAddendumService _addendumService;
        private readonly IRoomOccupancyProvider _occupancyProvider;

        [ObservableProperty] private Contract editable = new();
        [ObservableProperty] private bool isBusy;

        [ObservableProperty] private bool isReadOnly;
        [ObservableProperty] private bool canCreateAddendum;

        [ObservableProperty] private DateTime startDate;
        [ObservableProperty] private DateTime endDate;
        [ObservableProperty] private int dueDay;
        [ObservableProperty] private decimal rentAmount;
        [ObservableProperty] private decimal securityDeposit;
        [ObservableProperty] private int depositReturnDays;
        [ObservableProperty] private int maxOccupants;
        [ObservableProperty] private int maxBikeAllowance;
        [ObservableProperty] private string paymentMethods = "";
        [ObservableProperty] private string lateFeePolicy = "";
        [ObservableProperty] private string propertyDescription = "";
        [ObservableProperty] private string? pdfUrl;

        [ObservableProperty] private bool isPersisted;
        [ObservableProperty] private bool isDraft;
        [ObservableProperty] private bool isActive;
        [ObservableProperty] private bool isTerminated;

        [ObservableProperty] private bool canActivate;
        [ObservableProperty] private bool canTerminate;
        [ObservableProperty] private bool canDelete;

        // Draft flow: show when desktop-capable + draft + persisted
        [ObservableProperty] private bool canGenerateContractPdfAndSend;

        // Addendum flow: show create/send when there is at least one addendum
        [ObservableProperty] private bool canGeneratePdf;          // Tạo PDF phụ lục (auto-send)
        [ObservableProperty] private bool canSendAddendumEmail;    // Gửi phụ lục PDF (re-send)

        // Internals to compute flags
        private string? _originalEditHash;
        private ContractSnapshot? _loadedSnapshot;
        private bool _hasContractPdf;
        private bool _hasAddendum;

        public IAsyncRelayCommand SaveCommand { get; }
        public IAsyncRelayCommand ActivateCommand { get; }
        public IAsyncRelayCommand TerminateCommand { get; } // disabled on edit page
        public IAsyncRelayCommand GeneratePdfCommand { get; } // addendum PDF (auto-send)
        public IAsyncRelayCommand SendAddendumEmailCommand { get; } // re-send addendum PDF
        public IAsyncRelayCommand CreateAddendumCommand { get; }
        public IAsyncRelayCommand ViewHistoryCommand { get; }
        public IAsyncRelayCommand GenerateAndSendContractPdfCommand { get; } // NEW

        public ContractEditViewModel(IContractsRepository repo,
                                     IRoomStatusService roomStatusService,
                                     IRoomOccupancyAdminService roomOccAdmin,
                                     IEmailNotificationService emailNotify,
                                     IContractPdfService contractPdfService,
                                     IContractAddendumService addendumService,
                                     IRoomOccupancyProvider occupancyProvider,
                                     IPdfCapabilityService pdfCapability,
                                     IAuthService auth)
        {
            _repo = repo;
            _roomStatusService = roomStatusService;
            _roomOccAdmin = roomOccAdmin;
            _emailNotify = emailNotify;
            _pdfService = contractPdfService;
            _addendumService = addendumService;
            _occupancyProvider = occupancyProvider;
            _pdfCapability = pdfCapability;
            _auth = auth;

            SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy);
            ActivateCommand = new AsyncRelayCommand(ActivateAsync, () => !IsBusy);
            TerminateCommand = new AsyncRelayCommand(TerminateAsync, () => !IsBusy);

            GenerateAndSendContractPdfCommand = new AsyncRelayCommand(GenerateAndSendContractPdfAsync, () => !IsBusy); // Contract PDF
            GeneratePdfCommand = new AsyncRelayCommand(GenerateAddendumPdfAndSendAsync, () => !IsBusy);               // Addendum PDF
            SendAddendumEmailCommand = new AsyncRelayCommand(SendAddendumEmailAsync, () => !IsBusy);

            CreateAddendumCommand = new AsyncRelayCommand(CreateAddendumAsync, () => !IsBusy);
            ViewHistoryCommand = new AsyncRelayCommand(ViewHistoryAsync, () => !IsBusy);
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            IsReadOnly = false;

            if (query.TryGetValue("Contract", out var obj) && obj is Contract c)
                Editable = c;

            if (query.TryGetValue("readonly", out var ro) && ro is bool b)
                IsReadOnly = b;
            else if (query.TryGetValue("readonly", out var ros) && bool.TryParse(ros?.ToString(), out var b2))
                IsReadOnly = b2;

            LoadFieldsFromEditable();
            _loadedSnapshot = BuildSnapshotFrom(Editable);
            _originalEditHash = ComputeEditHash(Editable);
            _ = RecalcStateFlagsAsync();
        }

        public void OnAppearing()
        {
            if (!IsReadOnly && Editable.Status == ContractStatus.Active && Editable.EndDate < DateTime.Today)
                Editable.Status = ContractStatus.Expired;

            _ = RecalcStateFlagsAsync();
        }

        private void LoadFieldsFromEditable()
        {
            StartDate = Editable.StartDate;
            EndDate = Editable.EndDate;
            DueDay = Editable.DueDay;
            RentAmount = Editable.RentAmount;
            SecurityDeposit = Editable.SecurityDeposit;
            DepositReturnDays = Editable.DepositReturnDays;
            MaxOccupants = Editable.MaxOccupants;
            MaxBikeAllowance = Editable.MaxBikeAllowance;
            PaymentMethods = Editable.PaymentMethods;
            LateFeePolicy = Editable.LateFeePolicy;
            PropertyDescription = Editable.PropertyDescription;
            PdfUrl = Editable.PdfUrl;
        }

        private void PushFieldsIntoEditable()
        {
            Editable.StartDate = StartDate;
            Editable.EndDate = EndDate;
            Editable.DueDay = DueDay;
            Editable.RentAmount = RentAmount;
            Editable.SecurityDeposit = SecurityDeposit;
            Editable.DepositReturnDays = DepositReturnDays;
            Editable.MaxOccupants = MaxOccupants;
            Editable.MaxBikeAllowance = MaxBikeAllowance;
            Editable.PaymentMethods = PaymentMethods?.Trim() ?? "";
            Editable.LateFeePolicy = LateFeePolicy?.Trim() ?? "";
            Editable.PropertyDescription = PropertyDescription?.Trim() ?? "";
            Editable.PdfUrl = PdfUrl?.Trim();
            Editable.UpdatedAt = DateTime.UtcNow;
        }

        private async Task RecalcStateFlagsAsync()
        {
            bool exists = false;
            if (!string.IsNullOrWhiteSpace(Editable.ContractId))
            {
                var found = await _repo.GetByIdAsync(Editable.ContractId);
                exists = found != null;
            }

            IsPersisted = exists;
            IsDraft = Editable.Status == ContractStatus.Draft;
            IsActive = Editable.Status == ContractStatus.Active;
            IsTerminated = Editable.Status == ContractStatus.Terminated;

            _hasContractPdf = CheckContractPdfExists(Editable.ContractId);
            var adds = IsPersisted ? await _addendumService.GetAddendumsAsync(Editable.ContractId) : Array.Empty<ContractAddendum>();
            _hasAddendum = adds.Count > 0;

            CanCreateAddendum = IsPersisted && IsActive && Editable.NeedsAddendum;
            CanActivate = IsPersisted && IsDraft && Editable.Tenants.Count > 0 && _hasContractPdf;
            CanTerminate = false;
            CanDelete = IsPersisted && IsTerminated;

            // Replaced IsSupported with CanGeneratePdf
            CanGenerateContractPdfAndSend = _pdfCapability.CanGeneratePdf && IsPersisted && IsDraft;
            CanGeneratePdf = _pdfCapability.CanGeneratePdf && IsPersisted && _hasAddendum && string.IsNullOrWhiteSpace(Editable.PdfUrl);
            CanSendAddendumEmail = IsPersisted && _hasAddendum && !string.IsNullOrWhiteSpace(Editable.PdfUrl);
        }

        private bool CheckContractPdfExists(string? contractId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(contractId)) return false;
                var folder = Path.Combine(FileSystem.AppDataDirectory, "contracts");
                var path = Path.Combine(folder, $"{contractId}.pdf");
                return File.Exists(path);
            }
            catch { return false; }
        }

        private async Task SaveAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                if (EndDate <= StartDate)
                {
                    await Shell.Current.DisplayAlertAsync("Lỗi", "Ngày kết thúc phải sau ngày bắt đầu.", "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(Editable.ContractId))
                    Editable.ContractId = Guid.NewGuid().ToString("N");
                if (string.IsNullOrWhiteSpace(Editable.ContractNumber))
                    Editable.ContractNumber = "HD-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");

                PushFieldsIntoEditable();

                var newHash = ComputeEditHash(Editable);
                var changedWhileActive = IsActive && _originalEditHash != null && _originalEditHash != newHash;

                if (changedWhileActive)
                {
                    var proceed = await Shell.Current.DisplayAlertAsync(
                        "Hợp đồng đang hiệu lực",
                        "Bạn đang sửa hợp đồng đang hiệu lực. Áp dụng thay đổi sẽ tạo PHỤ LỤC hợp đồng. Tiếp tục?",
                        "Tạo phụ lục",
                        "Hủy");

                    if (!proceed) return;

                    var landlord = BuildLandlordInfo();

                    var oldSnapshot = _loadedSnapshot ?? BuildSnapshotFrom(Editable);
                    var newSnapshot = BuildSnapshotFrom(Editable);

                    var add = await _addendumService.CreateAddendumWithSnapshotsAsync(
                        Editable, oldSnapshot, newSnapshot, "Điều chỉnh điều khoản hợp đồng", DateTime.Today, landlord);

                    // Reload parent updated by service and attach new addendum PDF link
                    var reloaded = await _repo.GetByIdAsync(Editable.ContractId);
                    if (reloaded != null)
                    {
                        Editable = reloaded;

                        PdfUrl = add.PdfUrl;
                        Editable.PdfUrl = add.PdfUrl;
                        await _repo.UpdateAsync(Editable);

                        LoadFieldsFromEditable();
                        _loadedSnapshot = BuildSnapshotFrom(Editable);
                        _originalEditHash = ComputeEditHash(Editable);
                    }

                    await Shell.Current.DisplayAlertAsync("Thành công", $"Đã tạo phụ lục {add.AddendumNumber} và cập nhật hợp đồng.", "OK");
                    await RecalcStateFlagsAsync();
                    return;
                }

                // Normal save (Draft or unchanged)
                var existing = await _repo.GetByIdAsync(Editable.ContractId);
                if (existing == null)
                {
                    await _repo.CreateAsync(Editable);
                    IsPersisted = true;
                }
                else
                {
                    // If fields changed (in Draft), clear addendum link as it's no longer relevant
                    if (_originalEditHash != null && _originalEditHash != newHash)
                    {
                        Editable.PdfUrl = null;
                        PdfUrl = null;
                    }
                    await _repo.UpdateAsync(Editable);
                }

                _loadedSnapshot = BuildSnapshotFrom(Editable);
                _originalEditHash = newHash;

                await Shell.Current.DisplayAlertAsync("Thành công", "Đã lưu hợp đồng.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
                await RecalcStateFlagsAsync();
            }
        }

        private async Task ActivateAsync()
        {
            if (IsBusy) return;
            if (!CanActivate)
            {
                await Shell.Current.DisplayAlertAsync("Không thể kích hoạt", "Cần tạo và gửi PDF hợp đồng trước khi kích hoạt.", "OK");
                return;
            }
            IsBusy = true;
            try
            {
                PushFieldsIntoEditable();
                if (Editable.Tenants.Count == 0)
                {
                    await Shell.Current.DisplayAlertAsync("Lỗi", "Hợp đồng không có người thuê.", "OK");
                    return;
                }
                Editable.Status = ContractStatus.Active;
                await _repo.UpdateAsync(Editable);
                await _roomStatusService.SetRoomOccupiedAsync(Editable.RoomCode);
                await _emailNotify.SendContractActivatedAsync(Editable);
                _loadedSnapshot = BuildSnapshotFrom(Editable);
                _originalEditHash = ComputeEditHash(Editable);
                await Shell.Current.DisplayAlertAsync("Thành công", "Hợp đồng đã kích hoạt.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
                await RecalcStateFlagsAsync();
            }
        }

        private async Task TerminateAsync()
        {
            await Shell.Current.DisplayAlertAsync("Không khả dụng", "Vui lòng chấm dứt hợp đồng từ danh sách hợp đồng.", "OK");
        }

        // CONTRACT PDF: create and send (Draft flow)
        private async Task GenerateAndSendContractPdfAsync()
        {
            if (!_pdfCapability.CanGeneratePdf)
            {
                await Shell.Current.DisplayAlertAsync("Không hỗ trợ", "Tính năng tạo PDF chỉ khả dụng trên phiên bản Desktop.", "OK");
                return;
            }
            if (!IsPersisted || !IsDraft)
            {
                await Shell.Current.DisplayAlertAsync("Không thể tạo", "Chỉ tạo và gửi PDF hợp đồng khi đang ở trạng thái nháp.", "OK");
                return;
            }

            try
            {
                var landlord = BuildLandlordInfo();
                var path = await _pdfService.GenerateContractPdfAsync(Editable, landlord);

                // Send the freshly generated contract PDF without altering Addendum link
                await _emailNotify.SendContractPdfFromPathAsync(Editable, path);

                await Shell.Current.DisplayAlertAsync("Thành công", "Đã tạo và gửi PDF hợp đồng cho người thuê.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi", ex.Message, "OK");
            }
            finally
            {
                await RecalcStateFlagsAsync();
            }
        }

        // ADDENDUM PDF: create latest addendum PDF and send immediately
        private async Task GenerateAddendumPdfAndSendAsync()
        {
            if (!_pdfCapability.CanGeneratePdf)
            {
                await Shell.Current.DisplayAlertAsync("Không hỗ trợ", "Tính năng tạo PDF chỉ khả dụng trên phiên bản Desktop.", "OK");
                return;
            }

            // Find the latest addendum
            var addendums = await _addendumService.GetAddendumsAsync(Editable.ContractId);
            var latestAddendum = addendums.OrderBy(a => a.CreatedAt).LastOrDefault();
            if (latestAddendum == null)
            {
                await Shell.Current.DisplayAlertAsync("Chưa có phụ lục", "Vui lòng tạo phụ lục trước khi tạo PDF.", "OK");
                return;
            }

            try
            {
                var landlord = BuildLandlordInfo();
                var path = await _pdfService.GenerateContractAddendumPdfAsync(Editable, latestAddendum, landlord);

                // Save link to contract for display/send (link section is for addendum)
                PdfUrl = path;
                Editable.PdfUrl = path;
                PushFieldsIntoEditable();
                await _repo.UpdateAsync(Editable);

                // Send right away
                await _emailNotify.SendContractAddendumAsync(Editable, latestAddendum);

                await Shell.Current.DisplayAlertAsync("Thành công", "Đã tạo và gửi PDF phụ lục.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi", ex.Message, "OK");
            }
            finally
            {
                await RecalcStateFlagsAsync();
            }
        }

        private async Task SendAddendumEmailAsync()
        {
            if (!CanSendAddendumEmail)
            {
                await Shell.Current.DisplayAlertAsync("Không thể gửi", "Chưa có file PDF phụ lục.", "OK");
                return;
            }

            var addendums = await _addendumService.GetAddendumsAsync(Editable.ContractId);
            var latestAddendum = addendums.OrderBy(a => a.CreatedAt).LastOrDefault();
            if (latestAddendum == null)
            {
                await Shell.Current.DisplayAlertAsync("Chưa có phụ lục", "Vui lòng tạo phụ lục trước.", "OK");
                return;
            }

            try
            {
                await _emailNotify.SendContractAddendumAsync(Editable, latestAddendum);
                await Shell.Current.DisplayAlertAsync("Thành công", "Đã gửi PDF phụ lục đến người thuê.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi", ex.Message, "OK");
            }
        }

        private async Task CreateAddendumAsync()
        {
            if (!CanCreateAddendum)
            {
                await Shell.Current.DisplayAlertAsync("Phụ lục", "Chỉ tạo phụ lục khi hợp đồng đang hiệu lực và cần phụ lục.", "OK");
                return;
            }

            try
            {
                var latest = await _repo.GetByIdAsync(Editable.ContractId) ?? Editable;

                var uiSnap = BuildUiChangesSnapshot();

                var occDtos = await _occupancyProvider.GetTenantsForRoomAsync(latest.RoomCode);
                var newTenantNames = string.Join(", ", occDtos.Select(o => o.Name));
                var oldTenantNames = string.Join(", ", latest.Tenants.Select(t => t.Name));

                var summary = $"Trước: {oldTenantNames}\nSau: {newTenantNames}";
                var proceed = await Shell.Current.DisplayAlertAsync("Xác nhận tạo phụ lục", summary, "Tạo", "Hủy");
                if (!proceed) return;

                var reason = await Shell.Current.DisplayPromptAsync("Phụ lục", "Lý do thay đổi (tùy chọn):", "OK", "Bỏ qua", maxLength: 200);
                if (reason == "Bỏ qua") reason = null;

                var landlord = BuildLandlordInfo();

                var add = await _addendumService.CreateAddendumFromCurrentContractAndRoomAsync(
                    latest, uiSnap, reason, DateTime.Today, landlord);

                var reloaded = await _repo.GetByIdAsync(Editable.ContractId);
                if (reloaded != null)
                {
                    Editable = reloaded;

                    // Attach addendum PDF link from service result if available
                    PdfUrl = add.PdfUrl;
                    Editable.PdfUrl = add.PdfUrl;
                    await _repo.UpdateAsync(Editable);

                    LoadFieldsFromEditable();
                    _originalEditHash = ComputeEditHash(Editable);
                }

                await Shell.Current.DisplayAlertAsync("Thành công", $"Đã tạo phụ lục {add.AddendumNumber}.", "OK");
                await RecalcStateFlagsAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi", ex.Message, "OK");
            }
        }

        private async Task ViewHistoryAsync()
        {
            await Shell.Current.DisplayAlertAsync("Lịch sử", "Vui lòng xem lịch sử từ danh sách hợp đồng.", "OK");
        }

        private ContractSnapshot BuildUiChangesSnapshot()
        {
            return new ContractSnapshot
            {
                ContractNumber = Editable.ContractNumber ?? "",
                RoomCode = Editable.RoomCode,
                HouseAddress = Editable.HouseAddress,
                StartDate = StartDate,
                EndDate = EndDate,
                DueDay = DueDay,
                RentAmount = RentAmount,
                SecurityDeposit = SecurityDeposit,
                DepositReturnDays = DepositReturnDays,
                MaxOccupants = MaxOccupants,
                MaxBikeAllowance = MaxBikeAllowance,
                PaymentMethods = PaymentMethods,
                LateFeePolicy = LateFeePolicy,
                PropertyDescription = PropertyDescription,
                Tenants = Editable.Tenants.ToList(),
                PdfUrl = PdfUrl
            };
        }

        private ContractSnapshot BuildSnapshotFrom(Contract c)
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

        private static Contract MakePreviewFrom(ContractSnapshot snap, string? labelSuffix)
        {
            return new Contract
            {
                ContractId = Guid.NewGuid().ToString("N"),
                ContractNumber = string.IsNullOrWhiteSpace(labelSuffix) ? snap.ContractNumber : $"{snap.ContractNumber}{labelSuffix}",
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

        private static string ComputeEditHash(Contract c)
        {
            var payload = string.Join("|", new[]
            {
                c.StartDate.ToString("O"),
                c.EndDate.ToString("O"),
                c.DueDay.ToString(),
                c.RentAmount.ToString("0.####"),
                c.SecurityDeposit.ToString("0.####"),
                c.DepositReturnDays.ToString(),
                c.MaxOccupants.ToString(),
                c.MaxBikeAllowance.ToString(),
                c.PaymentMethods ?? "",
                c.LateFeePolicy ?? "",
                c.PropertyDescription ?? ""
            });

            using var sha1 = SHA1.Create();
            var bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(bytes);
        }

        private LandlordInfo BuildLandlordInfo()
        {
            var admin = _auth.CurrentAdmin;
            var fullName = string.IsNullOrWhiteSpace(admin?.FullName) ? "Chủ nhà" : admin!.FullName;
            var phone = admin?.PhoneNumber ?? "";
            var address = Editable.HouseAddress;
            var idCard = admin?.IdCardNumber ?? "";
            return new LandlordInfo(fullName, address, idCard, phone);
        }
    }
}