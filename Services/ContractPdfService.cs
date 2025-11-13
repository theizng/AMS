using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AMS.Models;
using AMS.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AMS.Services
{
    public class ContractPdfService : IContractPdfService
    {
        public async Task<string> GenerateContractPdfAsync(Contract contract, LandlordInfo landlord, CancellationToken ct = default)
        {
            var folder = Path.Combine(FileSystem.AppDataDirectory, "contracts");
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, $"{contract.ContractId}.pdf");

            var today = DateTime.Today;
            var tenantList = contract.Tenants.Select((t, i) =>
                $"{i + 1}. Ông/Bà: {t.Name}   SĐT: {t.Phone}   Email: {t.Email}");

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A4);

                    page.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM")
                            .Bold().FontSize(14);
                        col.Item().AlignCenter().Text("Độc lập – Tự do – Hạnh phúc").Italic().FontSize(11);
                        col.Item().PaddingTop(15).AlignCenter().Text("HỢP ĐỒNG THUÊ PHÒNG TRỌ").Bold().FontSize(16);
                    });

                    page.Content().Column(col =>
                    {
                        col.Item().Text($"Hôm nay ngày {today:dd} tháng {today:MM} năm {today:yyyy}; tại địa chỉ: {contract.HouseAddress}")
                            .FontSize(11);

                        col.Item().PaddingTop(10).Text("Chúng tôi gồm:").Bold().FontSize(12);

                        col.Item().Text(text =>
                        {
                            text.DefaultTextStyle(TextStyle.Default.FontSize(11));
                            text.Span("1. Bên cho thuê (Bên A):").Bold();
                            text.Line("");
                            text.Line($"Ông/Bà: {landlord.FullName} {(!string.IsNullOrWhiteSpace(landlord.RepresentativeTitle) ? $"({landlord.RepresentativeTitle})" : "")}");
                            text.Line($"CMND/CCCD: {landlord.IdCardNumber}");
                            text.Line($"Địa chỉ: {landlord.Address}");
                            text.Line($"Số điện thoại: {landlord.Phone}");
                        });

                        col.Item().PaddingTop(8).Text(text =>
                        {
                            text.DefaultTextStyle(TextStyle.Default.FontSize(11));
                            text.Span("2. Bên thuê phòng trọ (Bên B):").Bold();
                            text.Line("");
                            foreach (var t in tenantList) text.Line(t);
                        });

                        col.Item().PaddingTop(12).Text("Sau khi bàn bạc trên tinh thần tự nguyện, hai bên thống nhất các điều khoản sau:")
                            .Bold().FontSize(12);

                        col.Item().Text(text =>
                        {
                            text.DefaultTextStyle(TextStyle.Default.FontSize(11));
                            text.Line("");
                            text.Span("Điều 1. Thông tin phòng thuê").Bold();
                            text.Line("");
                            text.Line($"Phòng: {contract.RoomCode} tại địa chỉ: {contract.HouseAddress}");
                            text.Line($"Thời hạn thuê: từ {contract.StartDate:dd/MM/yyyy} đến {contract.EndDate:dd/MM/yyyy}");
                            text.Line($"Giá thuê: {contract.RentAmount:N0} đ/tháng. Ngày thu tiền: {contract.DueDay}");
                            text.Line($"Đặt cọc: {contract.SecurityDeposit:N0} đ; Trả lại trong vòng {contract.DepositReturnDays} ngày khi kết thúc hợp đồng.");
                            text.Line($"Số người tối đa: {contract.MaxOccupants}; Xe máy tối đa: {contract.MaxBikeAllowance}");
                        });

                        col.Item().PaddingTop(8).Text(text =>
                        {
                            text.DefaultTextStyle(TextStyle.Default.FontSize(11));
                            text.Line("");
                            text.Span("Điều 2. Phương thức thanh toán và trễ hạn").Bold();
                            text.Line("");
                            text.Line($"Phương thức thanh toán: {contract.PaymentMethods}");
                            text.Line($"Chính sách trễ hạn: {contract.LateFeePolicy}");
                        });

                        col.Item().PaddingTop(8).Text(text =>
                        {
                            text.DefaultTextStyle(TextStyle.Default.FontSize(11));
                            text.Span("Điều 3. Trách nhiệm và sử dụng").Bold();
                            text.Line("");
                            text.Line("Bên B cam kết sử dụng phòng đúng mục đích, không gây hư hỏng, không chuyển nhượng trái phép.");
                        });

                        col.Item().PaddingTop(8).Text(text =>
                        {
                            text.DefaultTextStyle(TextStyle.Default.FontSize(11));
                            text.Span("Điều 4. Chấm dứt hợp đồng").Bold();
                            text.Line("");
                            text.Line("Hợp đồng tự động chấm dứt khi hết hạn hoặc theo thỏa thuận bằng phụ lục/hủy trước hạn.");
                        });

                        col.Item().PaddingTop(12).Text("ĐẠI DIỆN CÁC BÊN KÝ TÊN").Bold().FontSize(12);

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(cLeft =>
                            {
                                cLeft.Item().PaddingTop(10).Text("BÊN A").Bold();
                                cLeft.Item().PaddingTop(40).Text("(Ký, ghi rõ họ tên)");
                            });
                            row.RelativeItem().Column(cRight =>
                            {
                                cRight.Item().PaddingTop(10).Text("BÊN B").Bold();
                                cRight.Item().PaddingTop(40).Text("(Ký, ghi rõ họ tên)");
                            });
                        });
                    });

                    page.Footer().AlignCenter().Text($"Hợp đồng số: {contract.ContractNumber} / ID: {contract.ContractId}")
                        .FontSize(9).Italic();
                });
            });

            var pdfData = doc.GeneratePdf();
            await File.WriteAllBytesAsync(path, pdfData, ct);
            return path;
        }
        public async Task<string> GenerateContractAddendumPdfAsync(Contract parent, ContractAddendum addendum, LandlordInfo landlord, CancellationToken ct = default)
        {
            var folder = Path.Combine(FileSystem.AppDataDirectory, "contracts");
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, $"{parent.ContractId}-{addendum.AddendumId}.pdf");

            // Diff (very simple: list old and new)
            var oldNames = addendum.OldTenants.Select(t => $"{t.Name} ({t.Phone})");
            var newNames = addendum.NewTenants.Select(t => $"{t.Name} ({t.Phone})");

            var effective = addendum.EffectiveDate ?? DateTime.Today;

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A4);

                    page.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM").Bold().FontSize(14);
                        col.Item().AlignCenter().Text("Độc lập – Tự do – Hạnh phúc").Italic().FontSize(11);
                        col.Item().PaddingTop(15).AlignCenter().Text("PHỤ LỤC HỢP ĐỒNG THUÊ PHÒNG TRỌ").Bold().FontSize(16);
                    });

                    page.Content().Column(col =>
                    {
                        col.Item().Text($"Phụ lục cho Hợp đồng: {parent.ContractNumber ?? parent.ContractId} – Phòng: {parent.RoomCode}")
                            .FontSize(11);
                        col.Item().Text($"Địa chỉ: {parent.HouseAddress}").FontSize(11);
                        col.Item().Text($"Ngày hiệu lực phụ lục: {effective:dd/MM/yyyy}").FontSize(11);
                        if (!string.IsNullOrWhiteSpace(addendum.Reason))
                            col.Item().Text($"Nội dung thay đổi: {addendum.Reason}").FontSize(11);

                        col.Item().PaddingTop(10).Text("Danh sách người thuê trước khi thay đổi:").Bold().FontSize(12);
                        col.Item().Text(string.Join("\n", oldNames)).FontSize(11);

                        col.Item().PaddingTop(8).Text("Danh sách người thuê sau khi thay đổi:").Bold().FontSize(12);
                        col.Item().Text(string.Join("\n", newNames)).FontSize(11);

                        col.Item().PaddingTop(12).Text("Hai bên thống nhất các điều khoản điều chỉnh liên quan đến người thuê và các trách nhiệm đi kèm. Các điều khoản khác của Hợp đồng không thay đổi.")
                            .FontSize(11);

                        col.Item().PaddingTop(12).Text("ĐẠI DIỆN CÁC BÊN KÝ TÊN").Bold().FontSize(12);

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(cLeft =>
                            {
                                cLeft.Item().PaddingTop(10).Text("BÊN A").Bold();
                                cLeft.Item().PaddingTop(40).Text("(Ký, ghi rõ họ tên)");
                            });
                            row.RelativeItem().Column(cRight =>
                            {
                                cRight.Item().PaddingTop(10).Text("BÊN B").Bold();
                                cRight.Item().PaddingTop(40).Text("(Ký, ghi rõ họ tên)");
                            });
                        });
                    });

                    page.Footer().AlignCenter().Text($"Phụ lục: {addendum.AddendumNumber ?? addendum.AddendumId} | HĐ: {parent.ContractNumber ?? parent.ContractId}")
                        .FontSize(9).Italic();
                });
            });

            var pdfData = doc.GeneratePdf();
            await File.WriteAllBytesAsync(path, pdfData, ct);
            return path;
        }
    }
}