using AMS.Data;
using AMS.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;

namespace AMS.ViewModels
{
    public class TenantEditViewModel : INotifyPropertyChanged
    {
        private readonly AMSDbContext _db;

        private int _idTenant;
        private string _fullName = "";
        private string? _email;
        private string? _phoneNumber;
        private string? _idCardNumber;
        private DateTime _dateOfBirth = DateTime.UtcNow.AddYears(-18);
        private string? _permanentAddress;
        private string? _notes;
        private string? _profilePictureUrl;
        private bool _isActive = true;

        // Emergency contacts as a simple multiline text editor
        private string _emergencyContactsText = "";

        public string PageTitle => _idTenant == 0 ? "Thêm người thuê" : "Sửa người thuê";

        public int IdTenant { get => _idTenant; set { _idTenant = value; OnPropertyChanged(); OnPropertyChanged(nameof(PageTitle)); } }
        public string FullName { get => _fullName; set { _fullName = value; OnPropertyChanged(); } }
        public string? Email { get => _email; set { _email = value; OnPropertyChanged(); } }
        public string? PhoneNumber { get => _phoneNumber; set { _phoneNumber = value; OnPropertyChanged(); } }
        public string? IdCardNumber { get => _idCardNumber; set { _idCardNumber = value; OnPropertyChanged(); } }
        public DateTime DateOfBirth { get => _dateOfBirth; set { _dateOfBirth = value; OnPropertyChanged(); } }
        public string? PermanentAddress { get => _permanentAddress; set { _permanentAddress = value; OnPropertyChanged(); } }
        public string? Notes { get => _notes; set { _notes = value; OnPropertyChanged(); } }
        public string? ProfilePictureUrl { get => _profilePictureUrl; set { _profilePictureUrl = value; OnPropertyChanged(); } }
        public bool IsActive { get => _isActive; set { _isActive = value; OnPropertyChanged(); } }

        public string EmergencyContactsText
        {
            get => _emergencyContactsText;
            set { _emergencyContactsText = value; OnPropertyChanged(); }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public TenantEditViewModel(AMSDbContext db)
        {
            _db = db;
            SaveCommand = new Command(async () => await SaveAsync());
            CancelCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        }

        public void SetTenantId(int tenantId)
        {
            IdTenant = tenantId;
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            if (IdTenant <= 0) return;

            var entity = await _db.Tenants.FirstOrDefaultAsync(t => t.IdTenant == IdTenant);
            if (entity != null)
            {
                FullName = entity.FullName;
                Email = entity.Email;
                PhoneNumber = entity.PhoneNumber;
                IdCardNumber = entity.IdCardNumber;
                DateOfBirth = entity.DateOfBirth;
                PermanentAddress = entity.PermanentAddress;
                Notes = entity.Notes;
                ProfilePictureUrl = entity.ProfilePictureUrl;
                IsActive = entity.IsActive;

                // Convert JSON -> multiline text
                try
                {
                    if (!string.IsNullOrEmpty(entity.EmergencyContactsJson))
                    {
                        var list = JsonSerializer.Deserialize<List<string>>(entity.EmergencyContactsJson) ?? new();
                        EmergencyContactsText = string.Join(Environment.NewLine, list);
                    }
                }
                catch { EmergencyContactsText = ""; }
            }
        }

        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(FullName))
            {
                await Application.Current.MainPage.DisplayAlertAsync("Thiếu thông tin", "Vui lòng nhập họ tên.", "OK");
                return;
            }
            if (string.IsNullOrWhiteSpace(PhoneNumber))
            {
                await Application.Current.MainPage.DisplayAlertAsync("Thiếu thông tin", "Vui lòng nhập SĐT.", "OK");
                return;
            }
            if (string.IsNullOrWhiteSpace(IdCardNumber))
            {
                await Application.Current.MainPage.DisplayAlertAsync("Thiếu thông tin", "Vui lòng nhập CCCD/CMND.", "OK");
                return;
            }
            if (string.IsNullOrWhiteSpace(Email))
            {
                await Application.Current.MainPage.DisplayAlertAsync("Thiếu thông tin", "Vui lòng nhập Email.", "OK");
                return;
            }
            if (string.IsNullOrWhiteSpace(PermanentAddress))
            {
                await Application.Current.MainPage.DisplayAlertAsync("Thiếu thông tin", "Vui lòng nhập quê quán người thuê.", "OK");
                return;
            }

            try
            {
                // Convert multiline text -> JSON array
                string ecJson = "[]";
                if (!string.IsNullOrWhiteSpace(EmergencyContactsText))
                {
                    var list = EmergencyContactsText
                        .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => s.Length > 0)
                        .ToList();
                    ecJson = JsonSerializer.Serialize(list);
                }

                if (IdTenant == 0)
                {
                    var entity = new Tenant
                    {
                        FullName = FullName.Trim(),
                        Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                        PhoneNumber = PhoneNumber.Trim(),
                        IdCardNumber = IdCardNumber.Trim(),
                        DateOfBirth = DateOfBirth,
                        PermanentAddress = string.IsNullOrWhiteSpace(PermanentAddress) ? null : PermanentAddress.Trim(),
                        Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
                        ProfilePictureUrl = string.IsNullOrWhiteSpace(ProfilePictureUrl) ? null : ProfilePictureUrl.Trim(),
                        EmergencyContactsJson = ecJson,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        CreatedBy = "system",
                        UpdatedBy = "system",
                        IsActive = true
                    };

                    _db.Tenants.Add(entity);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    var entity = await _db.Tenants.FirstOrDefaultAsync(t => t.IdTenant == IdTenant);
                    if (entity == null)
                    {
                        await Application.Current.MainPage.DisplayAlertAsync("Lỗi", "Không tìm thấy người thuê.", "OK");
                        return;
                    }

                    entity.FullName = FullName.Trim();
                    entity.Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim();
                    entity.PhoneNumber = PhoneNumber.Trim();
                    entity.IdCardNumber = IdCardNumber.Trim();
                    entity.DateOfBirth = DateOfBirth;
                    entity.PermanentAddress = string.IsNullOrWhiteSpace(PermanentAddress) ? null : PermanentAddress.Trim();
                    entity.Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim();
                    entity.ProfilePictureUrl = string.IsNullOrWhiteSpace(ProfilePictureUrl) ? null : ProfilePictureUrl.Trim();
                    entity.EmergencyContactsJson = ecJson;
                    entity.UpdatedAt = DateTime.UtcNow;
                    entity.UpdatedBy = "system";

                    await _db.SaveChangesAsync();
                }

                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TenantEdit] Save error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlertAsync("Lỗi", "Không thể lưu người thuê.", "OK");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}