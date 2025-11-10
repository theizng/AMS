using AMS.Models;
using AMS.Services.Interfaces;
using System.IO;
using System.Threading.Tasks;

namespace AMS.Services
{
    // Placeholder: implement with actual PDF library later
    public class ContractPdfService : IContractPdfService
    {
        public Task<string> GeneratePdfAsync(Contract contract)
        {
            // For now just create a .txt to simulate a PDF path
            var folder = Path.Combine(FileSystem.AppDataDirectory, "contracts");
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, $"{contract.ContractId}.pdf");

            File.WriteAllText(path,
$@"HỢP ĐỒNG THUÊ PHÒNG
Số hợp đồng: {contract.ContractNumber}
Phòng: {contract.RoomCode}
Địa chỉ: {contract.HouseAddress}
Thời hạn: {contract.StartDate:yyyy-MM-dd} -> {contract.EndDate:yyyy-MM-dd}
Giá thuê: {contract.RentAmount:N0} đ / tháng
Người thuê:
{string.Join("\n", contract.Tenants.ConvertAll(t => $"- {t.Name} ({t.Phone}, {t.Email})"))}
... (nội dung bổ sung) ...");

            return Task.FromResult(path);
        }
    }
}