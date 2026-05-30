using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CMS.Data.Entities
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty; // Sửa lỗi Nullable chuỗi

        [MaxLength(15)]
        public string? PhoneNumber { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }
        public int RewardPoints { get; set; } = 0; // Điểm tích lũy

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Khách hàng có thể có nhiều lần đến ăn (Nhiều Order)
        // Khởi tạo List rỗng để tránh lỗi NullReferenceException khi Add phần tử
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}