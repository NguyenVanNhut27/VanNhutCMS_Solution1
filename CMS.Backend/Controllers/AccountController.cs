using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using CMS.Data;
using CMS.Data.Entities;

namespace CMS.Backend.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Đăng ký
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: Đăng ký
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User model, string confirmPassword)
        {
            // Kiểm tra xem tên đăng nhập đã tồn tại chưa
            if (_context.Users.Any(u => u.Username == model.Username))
            {
                ModelState.AddModelError("", "Tên đăng nhập này đã được sử dụng!");
                return View(model);
            }

            // Kiểm tra mật khẩu xác nhận
            if (model.Password != confirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu xác nhận không khớp.");
                return View(model);
            }

            if (ModelState.IsValid)
            {
                // 1. Hash mật khẩu trước khi lưu vào PasswordHash
                model.PasswordHash = HashPassword(model.Password);

                // 2. Thiết lập các giá trị mặc định
                model.CreatedAt = DateTime.Now;
                model.IsActive = true; // Cho phép đăng nhập ngay sau khi đăng ký
                model.Role = UserRole.Waiter; // Mặc định là nhân viên phục vụ (hoặc tùy bạn chọn)

                _context.Users.Add(model);
                await _context.SaveChangesAsync();

                // 3. Thông báo thành công và chuyển hướng
                TempData["Success"] = "Đăng ký thành công! Mời bạn đăng nhập.";
                return RedirectToAction("Login", "Account");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            string hashedPassword = HashPassword(password);
            var user = _context.Users.FirstOrDefault(u => u.Username == username && u.PasswordHash == hashedPassword);

            if (user != null)
            {
                if (!user.IsActive)
                {
                    ViewBag.Error = "Tài khoản của bạn đã bị khóa!";
                    return View();
                }

                // ==========================================
                // CHỐT CHẶN BẢO MẬT: TỪ CHỐI WAITER
                // ==========================================
                if (user.Role == UserRole.Waiter)
                {
                    ViewBag.Error = "Tài khoản Phục vụ vui lòng đăng nhập trên Ứng dụng POS (Frontend cổng 3000).";
                    return View();
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role.ToString()),
                    new Claim("FullName", user.FullName)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng!";
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ==========================================
        // CẬP NHẬT: Tự động tạo 4 tài khoản mẫu cho Nhà hàng
        // ==========================================
        [HttpGet]
        public IActionResult InitAccounts()
        {
            if (!_context.Users.Any())
            {
                var adminUser = new User
                {
                    Username = "admin",
                    PasswordHash = HashPassword("123456"),
                    FullName = "Quản lý Nhà hàng",
                    Email = "admin@gmail.com",
                    Role = UserRole.Admin,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                var cashierUser = new User
                {
                    Username = "cashier",
                    PasswordHash = HashPassword("123456"),
                    FullName = "Thu ngân Ca 1",
                    Email = "cashier@gmail.com",
                    Role = UserRole.Cashier,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                var chefUser = new User
                {
                    Username = "chef",
                    PasswordHash = HashPassword("123456"),
                    FullName = "Bếp trưởng",
                    Email = "chef@gmail.com",
                    Role = UserRole.Chef,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                var waiterUser = new User
                {
                    Username = "waiter",
                    PasswordHash = HashPassword("123456"),
                    FullName = "Nhân viên Phục vụ",
                    Email = "waiter@gmail.com",
                    Role = UserRole.Waiter,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.Users.AddRange(adminUser, cashierUser, chefUser, waiterUser);
                _context.SaveChanges();

                return Content("Đã tạo thành công 4 tài khoản mẫu!\n1. admin / 123456 (Quản lý)\n2. cashier / 123456 (Thu ngân)\n3. chef / 123456 (Bếp)\n4. waiter / 123456 (Phục vụ)");
            }

            return Content("Dữ liệu đã tồn tại trong hệ thống, không cần tạo thêm!");
        }

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