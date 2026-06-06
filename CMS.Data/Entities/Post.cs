using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Data.Entities
{
    public class Post
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Summary { get; set; } = string.Empty; // Tóm tắt bài viết

        [Required]
        public string Content { get; set; } = string.Empty; // Nội dung chi tiết

        public string? ImageUrl { get; set; } // Ảnh đại diện

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public int UserId { get; set; }
        public virtual User? User { get; set; }
    }
}