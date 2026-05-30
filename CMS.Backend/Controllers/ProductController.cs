using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;
using CMS.Data;
using CMS.Data.Entities;

namespace CMS.Backend.Controllers
{
    [Authorize(Roles = "Admin")] // Chỉ Quản lý mới được chỉnh sửa món ăn
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==============================
        // 1. DANH SÁCH MÓN ĂN
        // ==============================
        public async Task<IActionResult> Index()
        {
            // Join 2 bảng để lấy tên Danh mục ra ngoài giao diện
            var products = await _context.Products.Include(p => p.CategoryProduct).ToListAsync();
            return View(products);
        }

        // ==============================
        // 2. THÊM MỚI MÓN ĂN
        // ==============================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Truyền danh sách Category vào ViewBag để tạo Dropdown chọn
            var categories = await _context.CategoriesProducts.ToListAsync();
            ViewBag.CategoryList = new SelectList(categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model, IFormFile? uploadImage)
        {
            ModelState.Remove("CategoryProduct"); // Bỏ qua kiểm tra khóa ngoại tự động để tránh lỗi Form

            if (ModelState.IsValid)
            {
                if (uploadImage != null && uploadImage.Length > 0)
                {
                    string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(uploadImage.FileName);
                    string filePath = Path.Combine(folder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadImage.CopyToAsync(stream);
                    }
                    model.ImageUrl = "/uploads/" + fileName;
                }

                _context.Products.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var categories = await _context.CategoriesProducts.ToListAsync();
            ViewBag.CategoryList = new SelectList(categories, "Id", "Name", model.CategoryProductId);
            return View(model);
        }

        // ==============================
        // 3. CẬP NHẬT MÓN ĂN
        // ==============================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            var categories = await _context.CategoriesProducts.ToListAsync();
            ViewBag.CategoryList = new SelectList(categories, "Id", "Name", product.CategoryProductId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product model, IFormFile? uploadImage)
        {
            if (id != model.Id) return NotFound();
            ModelState.Remove("CategoryProduct");

            if (ModelState.IsValid)
            {
                if (uploadImage != null && uploadImage.Length > 0)
                {
                    string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(uploadImage.FileName);
                    string filePath = Path.Combine(folder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadImage.CopyToAsync(stream);
                    }
                    model.ImageUrl = "/uploads/" + fileName;
                }

                // --------------------------------------------------------
                // BẢO TOÀN DỮ LIỆU CŨ TỪ DATABASE (CHỐNG MẤT TỒN KHO)
                // --------------------------------------------------------
                var oldProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == model.Id);
                if (oldProduct != null)
                {
                    // 1. Giữ lại ảnh cũ nếu không có ảnh mới upload lên
                    if (string.IsNullOrEmpty(model.ImageUrl))
                    {
                        model.ImageUrl = oldProduct.ImageUrl;
                    }

                    // 2. Ép cứng số lượng tồn kho bằng với data cũ trong SQL, 
                    // tránh việc Form Edit không truyền lên làm reset về 0
                    model.StockQuantity = oldProduct.StockQuantity;
                }

                _context.Products.Update(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var categories = await _context.CategoriesProducts.ToListAsync();
            ViewBag.CategoryList = new SelectList(categories, "Id", "Name", model.CategoryProductId);
            return View(model);
        }

        // ==============================
        // 4. XÓA MÓN ĂN
        // ==============================
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}