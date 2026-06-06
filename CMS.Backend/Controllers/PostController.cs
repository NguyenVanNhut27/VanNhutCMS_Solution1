using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using CMS.Data;
using CMS.Data.Entities;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace CMS.Backend.Controllers
{
    [Authorize(Roles = "Admin")] // Đảm bảo chỉ Admin mới vào được trang này
    public class PostController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PostController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // 1. DANH SÁCH BÀI VIẾT
        public async Task<IActionResult> Index()
        {
            var posts = await _context.Posts.OrderByDescending(p => p.CreatedDate).ToListAsync();
            return View(posts);
        }

        // 2. THÊM MỚI (GET)
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Post model, IFormFile? uploadImage)
        {
            if (ModelState.IsValid)
            {
                if (uploadImage != null && uploadImage.Length > 0)
                {
                    model.ImageUrl = await SaveImage(uploadImage);
                }

                model.CreatedDate = DateTime.Now;

                // BỔ SUNG: Lấy Username của người đang đăng nhập và tìm ID của họ
                var currentUsername = User.Identity?.Name;
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == currentUsername);

                if (currentUser != null)
                {
                    model.UserId = currentUser.Id; // Gán ID người viết bài
                }

                _context.Posts.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }
        // 3. SỬA (POST)
        [HttpGet] // <-- Thêm [HttpGet] vào đây cho chắc chắn
        public async Task<IActionResult> Edit(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null)
            {
                return NotFound(); // Nếu không tìm thấy ID thì báo lỗi 404
            }
            return View(post);
        }

        // ==========================================
        // 3. SỬA (POST) - Hàm này để LƯU dữ liệu khi ấn nút "Cập nhật"
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Post model, IFormFile? uploadImage)
        {
            // ... (Giữ nguyên đoạn code xử lý lưu ảnh và Update Database của bạn ở đây)
            if (ModelState.IsValid)
            {
                var oldPost = await _context.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == model.Id);

                if (uploadImage != null && uploadImage.Length > 0)
                {
                    DeleteImage(oldPost?.ImageUrl);
                    model.ImageUrl = await SaveImage(uploadImage);
                }
                else
                {
                    model.ImageUrl = oldPost?.ImageUrl;
                }

                _context.Posts.Update(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // 4. XÓA BÀI VIẾT
        public async Task<IActionResult> Delete(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post != null)
            {
                DeleteImage(post.ImageUrl);
                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // --- CÁC HÀM XỬ LÝ ẢNH (HELPER) ---

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

        private void DeleteImage(string? url)
        {
            if (string.IsNullOrEmpty(url)) return;

            var path = Path.Combine(_webHostEnvironment.WebRootPath, url.TrimStart('/'));
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
        }
    }
}