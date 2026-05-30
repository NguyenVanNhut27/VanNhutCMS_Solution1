using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CMS.Data.Entities
{
    public class Supplier
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên nhà cung cấp không được để trống")]
        public string Name { get; set; } = string.Empty;

        public string? ContactName { get; set; } // Người liên hệ

        // Đã đồng bộ chuẩn tên biến
        public string? PhoneNumber { get; set; }

        // Khôi phục lại Address
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<InventoryReceipt> InventoryReceipts { get; set; } = new List<InventoryReceipt>();
    }
}