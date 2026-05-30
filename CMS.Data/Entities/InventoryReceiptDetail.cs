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

        [Required]
        public int Quantity { get; set; } // Số lượng nhập thêm

        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchasePrice { get; set; } // Giá vốn (giá nhập vào)
    }
}