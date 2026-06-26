using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMS.Data;
using System.Threading.Tasks;
using System.Security.Cryptography; // Thêm thư viện mã hóa mật khẩu
using System.Text;                  // Thêm thư viện xử lý chuỗi

namespace CMS.Backend.Controllers.Api // ĐÃ CẬP NHẬT: Đường dẫn chuẩn trong thư mục Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuthApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel request)
        {
            // 1. Mã hóa mật khẩu từ Frontend gửi lên bằng SHA256 để khớp với dữ liệu trong DB
            string hashedPassword = HashPassword(request.Password);

            // 2. Tìm tài khoản trong cơ sở dữ liệu (so sánh với PasswordHash)
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.PasswordHash == hashedPassword);

            if (user == null)
            {
                return BadRequest(new { message = "Tài khoản hoặc mật khẩu không chính xác!" });
            }

            // 3. Kiểm tra trạng thái khóa
            if (!user.IsActive)
            {
                return BadRequest(new { message = "Tài khoản này đã bị vô hiệu hóa!" });
            }

            // ==========================================
            // 4. CHỐT CHẶN BẢO MẬT: CHỈ DÀNH CHO PHỤC VỤ (WAITER)
            // ==========================================
            if (user.Role != CMS.Data.Entities.UserRole.Waiter)
            {
                return BadRequest(new { message = "Tài khoản này không có quyền truy cập App Gọi Món. Vui lòng đăng nhập tại trang Quản trị (Backend)!" });
            }

            // 5. Trả về kết quả thành công cho Phục vụ
            return Ok(new
            {
                username = user.Username,
                role = user.Role.ToString().ToLower(), // Trả về "waiter"
                fullName = user.FullName,
                redirectUrl = "/home" // Đường dẫn điều hướng cho Next.js
            });
        }

        // Hàm băm mật khẩu SHA256 đồng bộ với hệ thống quản trị Backend
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

    public class LoginModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}