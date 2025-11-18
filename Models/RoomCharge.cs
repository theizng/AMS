using System;
using System.Collections.Generic;

namespace AMS.Models
{
    public class RoomCharge
    {
        public string RoomChargeId { get; set; } = Guid.NewGuid().ToString("N");
        public string CycleId { get; set; } //Liên kết tới PaymentCycle
        public string RoomCode { get; set; } //Mã phòng, liên kết tới Room hoặc RoomCode trong Contract
        public decimal BaseRent { get; set; } //Tiền phòng, dựa trên Contract tại thời điểm tạo RoomCharge
        public PaymentStatus Status { get; set; } = PaymentStatus.MissingData; // Trạng thái thanh toán của RoomCharge

        public decimal TotalFees => (UtilityFeesTotal + CustomFeesTotal); //Chưa hiểu, chưa xài tới

            
        public decimal UtilityFeesTotal => (WaterAmount + ElectricAmount); // Tổng của Điện và Nước
        public decimal CustomFeesTotal { get; set; } // Tổng của các chi phí khác được tạo thêm
        public decimal ElectricAmount { get; set; } //Tổng tiền điện
        public decimal WaterAmount { get; set; } // Tổng tiền nước

        public decimal TotalDue => BaseRent + UtilityFeesTotal + CustomFeesTotal; // Tổng tiền phải trả
        public decimal AmountPaid { get; set; } // Tổng tiền đã thanh toán
        public decimal AmountRemaining => TotalDue - AmountPaid; // Tiền còn nợ, hiển thị trong giao diện, dùng trong gửi email nhắc nợ

        public DateTime? FirstSentAt { get; set; } //Có vẻ không cần thiết
        public DateTime? LastReminderSentAt { get; set; } //Có vẻ không cần thiết
        public DateTime? PaidAt { get; set; } //Có vẻ không cần thiết, sẽ được cập nhật khi chủ nhà cập nhật PaymentStatus

        public List<FeeInstance> Fees { get; set; } = new(); //Danh sách các chi phí phát sinh (FeeInstance model)
        public ElectricReading? ElectricReading { get; set; } //Ghi nhận điện (ElectricReading model)
        public WaterReading? WaterReading { get; set; } //Ghi nhận nước (WaterReading model)
        public List<PaymentRecord> Payments { get; set; } = new(); //Danh sách các ghi nhận thanh toán (PaymentRecord model)
    }
}