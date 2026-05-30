using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Data.Entities
{
    public class OrderDetail
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!; // Báo cho C# biết EF Core sẽ lo việc điền dữ liệu

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!; // Báo cho C# biết EF Core sẽ lo việc điền dữ liệu

        [Required]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; } // Giá chốt tại thời điểm gọi món (đề phòng sau này giá menu tăng)

        [MaxLength(200)]
        public string? Note { get; set; } // Ghi chú riêng cho bếp: Ít cay, không hành, nhiều đá...

        public bool IsServed { get; set; } = false; // Trạng thái: Bếp đã làm xong và Phục vụ đã mang ra bàn chưa?
    }
}