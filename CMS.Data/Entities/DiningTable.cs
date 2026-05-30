using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CMS.Data.Entities
{
    public class DiningTable
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty; // Sửa lỗi Cảnh báo Nullable chuỗi
        public int Capacity { get; set; } = 4;

        public TableStatus Status { get; set; } = TableStatus.Available;

        // Một bàn có thể phục vụ nhiều hóa đơn (ở các thời điểm khác nhau)
        // Khởi tạo List rỗng để tránh lỗi Null
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}