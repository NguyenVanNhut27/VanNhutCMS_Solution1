using System;
using System.ComponentModel.DataAnnotations;

namespace CMS.Data.Entities
{
    public class Promotion
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên chương trình khuyến mãi không được để trống")]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nội dung/Điều kiện áp dụng không được để trống")]
        public string Content { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ImageUrl { get; set; } // Banner quảng cáo

        // ==========================================
        // CÁC TRƯỜNG ĐẶC THÙ CHO KHUYẾN MÃI
        // ==========================================

        [Range(0, 100, ErrorMessage = "Phần trăm giảm giá chỉ từ 0 đến 100")]
        public int DiscountPercent { get; set; } = 0; // % Giảm giá (Ví dụ: 20%)

        public DateTime StartDate { get; set; } = DateTime.Now; // Ngày bắt đầu

        public DateTime EndDate { get; set; } = DateTime.Now.AddDays(7); // Ngày kết thúc (Mặc định chạy 7 ngày)

        public bool IsActive { get; set; } = true; // Trạng thái Ẩn/Hiện khuyến mãi
    }
}