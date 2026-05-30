using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Data.Entities
{
    public class InventoryReceipt
    {
        [Key]
        public int Id { get; set; }

        public DateTime ReceiptDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public string? Note { get; set; } // Ghi chú: Nhập bia cho lễ hội...

        // Ai là người nhập kho?
        public int UserId { get; set; }
        public virtual User? User { get; set; }

        // Nhập từ nhà cung cấp nào?
        public int SupplierId { get; set; }
        public virtual Supplier? Supplier { get; set; }

        public virtual ICollection<InventoryReceiptDetail> Details { get; set; } = new List<InventoryReceiptDetail>();
    }
}