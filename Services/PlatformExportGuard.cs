using System.Threading.Tasks;
using AMS.Services.Interfaces;
using Microsoft.Maui.Devices;

namespace AMS.Services
{
    public interface IPlatformExportGuard
    {
        Task<bool> EnsurePdfAsync();
        Task<bool> EnsureExcelAsync();
    }

    public class PlatformExportGuard : IPlatformExportGuard
    {
        private readonly IPdfCapabilityService _pdfCapability;
        public PlatformExportGuard(IPdfCapabilityService pdfCapability) => _pdfCapability = pdfCapability;

        public async Task<bool> EnsurePdfAsync()
        {
            if (_pdfCapability.CanGeneratePdf) return true;
            await Shell.Current.DisplayAlert("Không hỗ trợ",
                "Chức năng tạo PDF chỉ khả dụng trên máy tính (Windows, macOS).", "OK");
            return false;
        }

        // Adjust if Excel export is also desktop-only; if mobile is OK, return true directly.
        public async Task<bool> EnsureExcelAsync()
        {
            // Example: block on Android only
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                await Shell.Current.DisplayAlert("Không hỗ trợ",
                    "Xuất Excel chưa hỗ trợ trên Android.", "OK");
                return false;
            }
            return true;
        }
    }
}