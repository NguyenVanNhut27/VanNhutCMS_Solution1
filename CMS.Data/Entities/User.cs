using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.Data.Entities
{
    // KHÔNG để enum UserRole ở đây nữa, vì đã có ở file khác
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [NotMapped] // Dòng này cực quan trọng
        public string? Password { get; set; }

        public string PasswordHash { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public UserRole Role { get; set; } // C# sẽ tự động lấy từ file enum kia

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}