using CMS.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CMS.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Khai báo 7 bảng cơ sở dữ liệu
        public DbSet<User> Users { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<CategoryProduct> CategoriesProducts { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<DiningTable> DiningTables { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<InventoryReceipt> InventoryReceipts { get; set; }
        public DbSet<InventoryReceiptDetail> InventoryReceiptDetails { get; set; }
        public DbSet<Post> Posts { get; set; }

        // Bạn có thể xóa DbSet<Post> nếu không cần dùng tính năng viết tin tức nữa
    }
}