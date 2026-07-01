using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;
using CMS.Data;
using CMS.Data.Entities;

namespace CMS.Backend.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // ==============================
        // 1. DANH SÁCH MÓN ĂN
        // ==============================
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products.Include(p => p.CategoryProduct).ToListAsync();
            return View(products);
        }

        // ==============================
        // 2. THÊM MỚI (GET & POST)
        // ==============================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.CategoryList = new SelectList(await _context.CategoriesProducts.ToListAsync(), "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model, IFormFile? uploadImage)
        {
            ModelState.Remove("CategoryProduct"); // Bỏ qua validation object để tránh lỗi

            if (ModelState.IsValid)
            {
                if (uploadImage != null && uploadImage.Length > 0)
                {
                    model.ImageUrl = await SaveImage(uploadImage);
                }

                _context.Products.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CategoryList = new SelectList(await _context.CategoriesProducts.ToListAsync(), "Id", "Name", model.CategoryProductId);
            return View(model);
        }

        // ==============================
        // 3. CẬP NHẬT (GET & POST)
        // ==============================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            ViewBag.CategoryList = new SelectList(await _context.CategoriesProducts.ToListAsync(), "Id", "Name", product.CategoryProductId);
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
                var oldProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                if (oldProduct == null) return NotFound();

                if (uploadImage != null && uploadImage.Length > 0)
                {
                    // Xóa ảnh cũ (Đã bọc kiểm tra null để fix cảnh báo)
                    if (!string.IsNullOrEmpty(oldProduct.ImageUrl))
                    {
                        DeleteImage(oldProduct.ImageUrl);
                    }

                    // Upload ảnh mới
                    model.ImageUrl = await SaveImage(uploadImage);
                }
                else
                {
                    model.ImageUrl = oldProduct.ImageUrl;
                }

                model.StockQuantity = oldProduct.StockQuantity; // Bảo toàn tồn kho

                _context.Products.Update(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CategoryList = new SelectList(await _context.CategoriesProducts.ToListAsync(), "Id", "Name", model.CategoryProductId);
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
                // Đã bọc kiểm tra null để fix cảnh báo
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    DeleteImage(product.ImageUrl);
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // ==============================
        // HÀM HỖ TRỢ (PRIVATE)
        // ==============================
        private async Task<string> SaveImage(IFormFile image)
        {
            string folder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
            string filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }
            return "/uploads/" + fileName;
        }

        // Đã thêm dấu "?" vào "string? imageUrl" để dứt điểm cảnh báo null
        private void DeleteImage(string? imageUrl)
        {
            if (!string.IsNullOrEmpty(imageUrl))
            {
                var oldPath = Path.Combine(_webHostEnvironment.WebRootPath, imageUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            }
        }
    }
}