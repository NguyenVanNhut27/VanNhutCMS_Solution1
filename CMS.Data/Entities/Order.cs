using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Data.Entities
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        public DateTime OrderTime { get; set; } = DateTime.Now;
        public DateTime? PaymentTime { get; set; } // Thời gian xuất bill

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public PaymentMethod? PaymentMethod { get; set; } // Hình thức thanh toán

        public string? Note { get; set; } // Ghi chú chung (Khách vội, cần xuất hóa đơn đỏ...)

        // ==========================================
        // CÁC TRƯỜNG BỔ SUNG CHO FORM ĐƠN HÀNG MỚI
        // ==========================================

        [StringLength(50)]
        public string? OrderType { get; set; } // Lưu loại đơn hàng: DineIn, Takeaway, Delivery

        public int GuestCount { get; set; } = 1; // Số lượng khách, mặc định là 1

        // ==========================================

        // Bàn nào gọi? (Có thể Null nếu là đơn mua mang đi - Takeaway)
        public int? DiningTableId { get; set; }
        public DiningTable? DiningTable { get; set; }

        // Khách nào mua? (Có thể Null nếu là khách vãng lai không tích điểm)
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        // Danh sách các món trong hóa đơn
        // Khởi tạo List rỗng để triệt tiêu cảnh báo vàng và tránh lỗi Null
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}