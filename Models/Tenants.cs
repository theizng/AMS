using System;
using System.Collections.Generic;
using System.Text;

namespace AMS.Models
{
    public class Tenants
    {
        //Thuộc tính nhận dạng người thuê 
        public string Id { get; set; }
        public string FullName { get; set; }

        //Thuộc tính thông tin liên hệ
        public string Email { get; set; }
        public string PhoneNumber { get; set; }

        //Thuộc tính cá nhân
        public string IdCardNumber { get; set; } //CMND hoặc Căn cước
        public DateTime DateOfBirth { get; set; } //Ngày tháng năm sinh 
        public string PermanentAddress { get; set; } //Địa chỉ thường trú

        //Thuộc tính thông tin thuê nhà của cá nhân
        public string RoomId { get; set; } //Mã phòng thuê
        public DateTime MoveInDate { get; set; } //Ngày chuyển đến
        public DateTime? MoveOutDate { get; set; } //Ngày chuyển đi (có thể null nếu chưa chuyển đi)
        public decimal MonthlyRent { get; set; } //Tiền thuê hàng tháng
        public decimal DepositAmount { get; set; } //Số tiền đặt cọc    
        public string ContractUrl { get; set; } //URL hợp đồng thuê nhà

        //Thuộc tính các thông tin phụ
        public List<string> EmergencyContacts { get; set; } //Danh sách liên hệ khẩn cấp
        public string Notes { get; set; } //Ghi chú thêm
        public string ProfilePictureUrl { get; set; } //URL ảnh đại diện
        public DateTime CreatedAt { get; set; } //Ngày tạo hồ sơ
        public DateTime UpdatedAt { get; set; } //Ngày cập nhật hồ sơ gần nhất
        public string CreatedBy { get; set; } //Người tạo hồ sơ
        public string UpdatedBy { get; set; } //Người cập nhật hồ sơ gần nhất

        //Thuộc tính trạng thái
        public bool IsActive { get; set; } //Trạng thái hoạt động (còn thuê hay không)
    }
}
