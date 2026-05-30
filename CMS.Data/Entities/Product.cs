using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Data.Entities
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên món ăn không được để trống")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; } // Mô tả thành phần món ăn

        [Required(ErrorMessage = "Giá tiền không được để trống")]
        [Column(TypeName = "decimal(18,2)")] // Ép kiểu chuẩn cho tiền tệ trong SQL
        public decimal Price { get; set; }

        public string? ImageUrl { get; set; } // Hình ảnh món ăn

        public bool IsAvailable { get; set; } = true; // Trạng thái: Còn món (true) / Hết nguyên liệu (false)

        // ==========================================
        // QUẢN LÝ TỒN KHO (INVENTORY)
        // ==========================================
        [Required(ErrorMessage = "Số lượng tồn kho không được để trống")]
        public int StockQuantity { get; set; } = 0; // Số lượng đang còn trong kho

        [Required]
        public int LowStockThreshold { get; set; } = 5; // Ngưỡng báo động (Dưới số này sẽ cảnh báo sắp hết hàng)

        // ==========================================
        // KHÓA NGOẠI LIÊN KẾT VỚI BẢNG DANH MỤC
        // ==========================================
        [Required(ErrorMessage = "Vui lòng chọn danh mục cho món ăn")]
        public int CategoryProductId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        // Khai báo rõ ForeignKey giúp EF Core kết nối 2 bảng chính xác 100%
        [ForeignKey("CategoryProductId")]
        public virtual CategoryProduct? CategoryProduct { get; set; }
    }
}