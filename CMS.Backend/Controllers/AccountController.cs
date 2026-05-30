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
                    Role = UserRole.Cashier, // Đã sửa
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                var chefUser = new User
                {
                    Username = "chef",
                    PasswordHash = HashPassword("123456"),
                    FullName = "Bếp trưởng",
                    Email = "chef@gmail.com",
                    Role = UserRole.Chef, // Đã sửa
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                var waiterUser = new User
                {
                    Username = "waiter",
                    PasswordHash = HashPassword("123456"),
                    FullName = "Nhân viên Phục vụ",
                    Email = "waiter@gmail.com",
                    Role = UserRole.Waiter, // Đã sửa
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