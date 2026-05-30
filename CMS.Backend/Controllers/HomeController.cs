using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;
using System;
using CMS.Data;
using CMS.Data.Entities; // Bổ sung namespace này để nhận diện OrderStatus

namespace CMS.Backend.Controllers
{
    [Authorize] // BẢO MẬT: Bắt buộc phải đăng nhập
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // =========================================================
            // 1. TÍNH TOÁN THỐNG KÊ THẬT CHO BẢNG ĐIỀU KHIỂN (DASHBOARD)
            // =========================================================

            // Tổng số đơn hàng đã hoàn thành (thu tiền thành công)
            ViewBag.TotalOrders = await _context.Orders
                .CountAsync(o => o.Status == OrderStatus.Completed);

            // Tổng doanh thu (Sum của TotalAmount các đơn đã hoàn thành)
            ViewBag.TotalRevenue = await _context.Orders
                .Where(o => o.Status == OrderStatus.Completed)
                .SumAsync(o => o.TotalAmount);

            // Tổng số món ăn đang khả dụng trong thực đơn
            ViewBag.TotalProducts = await _context.Products
                .CountAsync(p => p.IsAvailable == true);

            // Tổng số khách hàng thành viên
            ViewBag.TotalCustomers = await _context.Customers.CountAsync();


            // =========================================================
            // 2. LẤY DANH SÁCH KHUYẾN MÃI (Thay thế cho Bài viết cũ)
            // =========================================================

            // Lấy 3 chương trình khuyến mãi đang Active và mới nhất
            var latestPromotions = await _context.Promotions
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.StartDate)
                .Take(3)
                .ToListAsync();

            // Truyền danh sách khuyến mãi ra View
            return View(latestPromotions);
        }
    }
}