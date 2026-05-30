using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using CMS.Data;
using CMS.Data.Entities;

namespace CMS.Backend.Controllers
{
    [Authorize] // Bắt buộc đăng nhập
    public class DiningTableController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DiningTableController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. XEM SƠ ĐỒ BÀN (Admin và Phục vụ đều xem được)
        [Authorize(Roles = "Admin,Waiter,Cashier")]
        public async Task<IActionResult> Index()
        {
            var tables = await _context.DiningTables.ToListAsync();
            return View(tables);
        }

        // 2. THÊM BÀN MỚI (Chỉ Admin)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create() => View();

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DiningTable table)
        {
            if (ModelState.IsValid)
            {
                _context.DiningTables.Add(table);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(table);
        }

        // 3. CẬP NHẬT TRẠNG THÁI BÀN (Ví dụ: Chuyển từ Bàn trống sang Đang có khách)
        [Authorize(Roles = "Admin,Waiter")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var table = await _context.DiningTables.FindAsync(id);
            if (table == null) return NotFound();
            return View(table);
        }

        [Authorize(Roles = "Admin,Waiter")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DiningTable table)
        {
            if (id != table.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(table);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(table);
        }

        // 4. XÓA BÀN (Chỉ Admin)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var table = await _context.DiningTables.FindAsync(id);
            if (table != null)
            {
                _context.DiningTables.Remove(table);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}