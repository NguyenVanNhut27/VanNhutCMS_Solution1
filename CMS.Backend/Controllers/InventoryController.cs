using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using CMS.Data;
using CMS.Data.Entities;

namespace CMS.Backend.Controllers
{
    [Authorize(Roles = "Admin")]
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InventoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. DANH SÁCH PHIẾU NHẬP KHO (INDEX)
        // ==========================================
        public async Task<IActionResult> Index()
        {
            // Include bảng Supplier (Nhà cung cấp) để lấy được tên hiển thị ra giao diện
            var receipts = await _context.InventoryReceipts
                .Include(r => r.Supplier)
                .OrderByDescending(r => r.ReceiptDate) // Phiếu mới nhất lên đầu
                .ToListAsync();

            return View(receipts);
        }

        // ==========================================
        // 2. TẠO PHIẾU NHẬP MỚI (CREATE)
        // ==========================================
        [HttpGet]
        public IActionResult Create()
        {
            // Load danh sách Nhà cung cấp đưa vào thẻ Dropdown
            ViewBag.Suppliers = new SelectList(_context.Suppliers, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InventoryReceipt model)
        {
            // 1. Lấy ID của người dùng đang đăng nhập
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                // Xử lý trường hợp không lấy được User (ví dụ: Session hết hạn)
                ModelState.AddModelError("", "Bạn cần đăng nhập để tạo phiếu nhập.");
                ViewBag.Suppliers = new SelectList(_context.Suppliers, "Id", "Name", model.SupplierId);
                return View(model);
            }

            // 2. Gán UserID vào model
            model.UserId = int.Parse(userId);
            model.ReceiptDate = DateTime.Now;

            if (ModelState.IsValid)
            {
                _context.InventoryReceipts.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", new { id = model.Id });
            }

            // Nếu có lỗi, load lại danh sách nhà cung cấp
            ViewBag.Suppliers = new SelectList(_context.Suppliers, "Id", "Name", model.SupplierId);
            return View(model);
        }
        // ==========================================
        // 4. SỬA PHIẾU NHẬP (EDIT)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var receipt = await _context.InventoryReceipts.FindAsync(id);
            if (receipt == null) return NotFound();

            // Nạp lại danh sách nhà cung cấp để chọn
            ViewBag.Suppliers = new SelectList(_context.Suppliers, "Id", "Name", receipt.SupplierId);
            return View(receipt);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, InventoryReceipt model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Lấy phiếu cũ từ DB để cập nhật
                    var receipt = await _context.InventoryReceipts.FindAsync(id);
                    if (receipt != null)
                    {
                        receipt.SupplierId = model.SupplierId;
                        receipt.Note = model.Note;
                        // Không cập nhật CreatedDate để giữ nguyên lịch sử

                        _context.Update(receipt);
                        await _context.SaveChangesAsync();
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    return NotFound();
                }
            }

            ViewBag.Suppliers = new SelectList(_context.Suppliers, "Id", "Name", model.SupplierId);
            return View(model);
        }
        // ==========================================
        // 3. XÓA PHIẾU NHẬP (DELETE)
        // ==========================================
        public async Task<IActionResult> Delete(int id)
        {
            var receipt = await _context.InventoryReceipts.FindAsync(id);
            if (receipt != null)
            {
                // Mẹo an toàn: Bạn có thể kiểm tra nếu phiếu đã nhập xong thì không cho xóa
                _context.InventoryReceipts.Remove(receipt);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Chỗ này tạm để sẵn hàm Details để kết nối với View Chi tiết phiếu nhập
        public async Task<IActionResult> Details(int id)
        {
            var receipt = await _context.InventoryReceipts
                .Include(r => r.Supplier)
                .Include(r => r.Details) // Khớp với ICollection<InventoryReceiptDetail> Details
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (receipt == null) return NotFound();
            return View(receipt);
        }
    }
}