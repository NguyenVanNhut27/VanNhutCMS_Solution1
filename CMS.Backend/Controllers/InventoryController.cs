using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
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
        public async Task<IActionResult> Create()
        {
            // Load danh sách Nhà cung cấp đưa vào thẻ Dropdown
            ViewBag.Suppliers = new SelectList(await _context.Suppliers.ToListAsync(), "Id", "Name");

            // Load danh sách Sản phẩm để JavaScript vẽ giao diện chọn món
            ViewBag.Products = await _context.Products.Select(p => new { Id = p.Id, Name = p.Name }).ToListAsync();

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
                ModelState.AddModelError("", "Bạn cần đăng nhập để tạo phiếu nhập.");
                ViewBag.Suppliers = new SelectList(await _context.Suppliers.ToListAsync(), "Id", "Name", model.SupplierId);
                ViewBag.Products = await _context.Products.Select(p => new { Id = p.Id, Name = p.Name }).ToListAsync();
                return View(model);
            }

            // 2. Gán UserID và ngày giờ vào model
            model.UserId = int.Parse(userId);
            model.ReceiptDate = DateTime.Now;

            // Loại bỏ lỗi validate của các Navigation Properties để ModelState hợp lệ
            ModelState.Remove("Supplier");
            ModelState.Remove("User");
            ModelState.Remove("Details"); // ĐÃ THÊM: Bỏ qua kiểm tra Details rỗng để cho phép tạo vỏ phiếu trước

            if (ModelState.IsValid)
            {
                // Bắt đầu một Transaction để đảm bảo tính toàn vẹn dữ liệu
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    decimal totalAmount = 0;

                    // Lặp qua từng chi tiết mặt hàng được gửi lên
                    if (model.Details != null && model.Details.Any())
                    {
                        foreach (var detail in model.Details)
                        {
                            totalAmount += (detail.Quantity * detail.UnitPrice);

                            // Tìm sản phẩm trong DB và CỘNG DỒN số lượng tồn kho (StockQuantity)
                            var product = await _context.Products.FindAsync(detail.ProductId);
                            if (product != null)
                            {
                                product.StockQuantity += detail.Quantity;
                                _context.Products.Update(product);
                            }
                        }
                    }

                    model.TotalAmount = totalAmount;

                    // Lưu phiếu nhập và chi tiết vào DB
                    _context.InventoryReceipts.Add(model);
                    await _context.SaveChangesAsync();

                    // Xác nhận Transaction thành công
                    await transaction.CommitAsync();

                    return RedirectToAction("Details", new { id = model.Id });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi lưu phiếu nhập: " + ex.Message);
                }
            }

            // Nếu có lỗi, load lại danh sách nhà cung cấp và sản phẩm
            ViewBag.Suppliers = new SelectList(await _context.Suppliers.ToListAsync(), "Id", "Name", model.SupplierId);
            ViewBag.Products = await _context.Products.Select(p => new { Id = p.Id, Name = p.Name }).ToListAsync();
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

            ModelState.Remove("Supplier");
            ModelState.Remove("User");

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
            var receipt = await _context.InventoryReceipts
                .Include(r => r.Details)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (receipt != null)
            {
                // TRỪ ĐI số lượng tồn kho khi xóa phiếu nhập
                foreach (var detail in receipt.Details)
                {
                    var product = await _context.Products.FindAsync(detail.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity -= detail.Quantity;
                        if (product.StockQuantity < 0) product.StockQuantity = 0;
                        _context.Products.Update(product);
                    }
                }

                _context.InventoryReceipts.Remove(receipt);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // 5. CHI TIẾT PHIẾU NHẬP (DETAILS)
        // ==========================================
        public async Task<IActionResult> Details(int id)
        {
            var receipt = await _context.InventoryReceipts
                .Include(r => r.Supplier)
                .Include(r => r.User) // Include người lập phiếu
                .Include(r => r.Details)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (receipt == null) return NotFound();
            return View(receipt);
        }
    }
}