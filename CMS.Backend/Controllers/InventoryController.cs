using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
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

        public async Task<IActionResult> Index()
        {
            var receipts = await _context.InventoryReceipts
                .Include(i => i.Supplier)
                .Include(i => i.User) // Hiển thị người tạo phiếu
                .OrderByDescending(i => i.ReceiptDate)
                .ToListAsync();
            return View(receipts);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Truyền dữ liệu ra View để hiển thị Dropdown
            ViewBag.SupplierId = new SelectList(await _context.Suppliers.ToListAsync(), "Id", "Name");
            ViewBag.ProductId = new SelectList(await _context.Products.ToListAsync(), "Id", "Name");
            return View();
        }

        // Action nhận mảng chi tiết phiếu nhập từ View gửi lên
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InventoryReceipt model, List<InventoryReceiptDetail> details)
        {
            // Transaction đảm bảo nếu lỗi ở dòng nào thì toàn bộ quá trình Nhập kho sẽ hủy bỏ (Rollback)
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    model.ReceiptDate = DateTime.Now;

                    // 1. Lưu phiếu nhập để sinh ra Id
                    _context.InventoryReceipts.Add(model);
                    await _context.SaveChangesAsync();

                    foreach (var item in details)
                    {
                        // 2. Thêm từng chi tiết nhập kho
                        item.InventoryReceiptId = model.Id;
                        _context.InventoryReceiptDetails.Add(item);

                        // 3. LOGIC CỘNG TỒN KHO: Tìm món ăn và cập nhật số lượng
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product != null)
                        {
                            product.StockQuantity += item.Quantity;
                            _context.Products.Update(product);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync(); // Xác nhận lưu vĩnh viễn vào SQL
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync(); // Hủy bỏ nếu có lỗi
                    ModelState.AddModelError("", "Lỗi khi lưu phiếu nhập. Vui lòng kiểm tra lại.");
                }
            }

            ViewBag.SupplierId = new SelectList(await _context.Suppliers.ToListAsync(), "Id", "Name", model.SupplierId);
            return View(model);
        }
    }
}