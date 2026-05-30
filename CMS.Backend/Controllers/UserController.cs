using CMS.Data;
using CMS.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace CMS.Backend.Controllers
{
    // ĐÃ SỬA: Đổi "Administrator" thành "Admin" để khớp chuẩn với Enum UserRole
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==============================
        // 1. DANH SÁCH (READ)
        // ==============================
        public async Task<IActionResult> Index()
        {
            var data = await _context.Users.ToListAsync();
            return View(data);
        }

        // ==============================
        // 2. THÊM MỚI (CREATE)
        // ==============================
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Bảo mật: Chống giả mạo Request
        public async Task<IActionResult> Create(User model)
        {
            if (ModelState.IsValid)
            {
                // Mã hóa mật khẩu trước khi lưu
                if (!string.IsNullOrEmpty(model.PasswordHash))
                {
                    model.PasswordHash = HashPassword(model.PasswordHash);
                }

                model.CreatedAt = System.DateTime.Now;

                _context.Users.Add(model);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // ==============================
        // 3. XÓA (DELETE)
        // ==============================
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // ==============================
        // 4. CẬP NHẬT (UPDATE)
        // ==============================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Xóa rỗng trường mật khẩu khi hiển thị lên giao diện để bảo mật
            user.PasswordHash = "";
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var existingUser = await _context.Users.FindAsync(model.Id);
                if (existingUser == null) return NotFound();

                // Cập nhật các thông tin cơ bản (Bổ sung thêm Email và IsActive)
                existingUser.Username = model.Username;
                existingUser.FullName = model.FullName;
                existingUser.Email = model.Email;
                existingUser.Role = model.Role;
                existingUser.IsActive = model.IsActive;

                // CHỈ mã hóa và ghi đè mật khẩu nếu người dùng có nhập dữ liệu mới
                if (!string.IsNullOrEmpty(model.PasswordHash))
                {
                    existingUser.PasswordHash = HashPassword(model.PasswordHash);
                }

                _context.Users.Update(existingUser);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // ==========================================
        // HÀM HỖ TRỢ: Mã hóa mật khẩu chuẩn SHA256
        // ==========================================
        private string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return string.Empty;

            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}