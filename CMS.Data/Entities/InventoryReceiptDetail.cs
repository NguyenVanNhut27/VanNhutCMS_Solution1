using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Data.Entities
{
    public class InventoryReceiptDetail
    {
        [Key]
        public int Id { get; set; }

        public int InventoryReceiptId { get; set; }

        [ForeignKey("InventoryReceiptId")]
        public virtual InventoryReceipt? InventoryReceipt { get; set; }

        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng")]
        public int Quantity { get; set; } // Số lượng nhập thêm

        // ĐÃ SỬA: Đổi tên từ PurchasePrice thành UnitPrice để khớp với ViewModel và Controller
        [Required(ErrorMessage = "Vui lòng nhập đơn giá")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; } // Đơn giá nhập vào (Giá vốn)
    }
}