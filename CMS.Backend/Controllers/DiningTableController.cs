using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMS.Data;
using CMS.Data.Entities;
using System.Threading.Tasks;

namespace CMS.Backend.Controllers
{
    public class DiningTableController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DiningTableController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. TRANG DANH SÁCH BÀN
        public async Task<IActionResult> Index()
        {
            var tables = await _context.DiningTables.ToListAsync();
            return View(tables);
        }

        // 2. TRANG THÊM BÀN MỚI (GET)
        public IActionResult Create()
        {
            return View();
        }

        // 3. XỬ LÝ LƯU BÀN MỚI (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DiningTable table)
        {
            if (ModelState.IsValid)
            {
                table.Status = TableStatus.Available; // Bàn mới luôn ở trạng thái Trống
                _context.Add(table);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm bàn mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(table);
        }

        // ==========================================
        // 4. XÓA BÀN (POST) - ĐÃ SỬA LỖI 405
        // ==========================================
        [HttpPost("DiningTable/Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var table = await _context.DiningTables.FindAsync(id);
            if (table != null)
            {
                if (table.Status == TableStatus.Occupied)
                {
                    TempData["Error"] = "Không thể xóa bàn đang có khách!";
                    return RedirectToAction(nameof(Index));
                }

                _context.DiningTables.Remove(table);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa bàn thành công!";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy bàn này!";
            }
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // 5. TRANG CHỈNH SỬA BÀN (GET)
        // ==========================================
        [HttpGet("DiningTable/Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var table = await _context.DiningTables.FindAsync(id);
            if (table == null)
            {
                TempData["Error"] = "Không tìm thấy bàn cần sửa!";
                return RedirectToAction(nameof(Index));
            }
            return View(table);
        }

        // ==========================================
        // 6. LƯU THÔNG TIN CHỈNH SỬA (POST)
        // ==========================================
        [HttpPost("DiningTable/Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DiningTable table)
        {
            if (id != table.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Lấy bàn cũ ra để giữ nguyên trạng thái (Status) hiện tại
                    var existingTable = await _context.DiningTables.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
                    if (existingTable != null)
                    {
                        table.Status = existingTable.Status;
                    }

                    _context.Update(table);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật thông tin bàn thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    TempData["Error"] = "Có lỗi xảy ra khi lưu dữ liệu!";
                }
                return RedirectToAction(nameof(Index));
            }
            return View(table);
        }
    }
}