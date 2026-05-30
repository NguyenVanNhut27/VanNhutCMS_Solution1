using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CMS.Data.Entities
{
    public class CategoryProduct
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; } // Ghi chú thêm cho danh mục

        // Quan hệ: Một danh mục có thể chứa nhiều món ăn
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}