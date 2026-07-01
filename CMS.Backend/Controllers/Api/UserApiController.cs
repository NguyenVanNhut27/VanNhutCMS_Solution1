using CMS.Data;
using CMS.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class UserApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public UserApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet] // Đây là GET api/User (nếu controller tên UserApi)
    public async Task<IActionResult> GetCurrentUser()
    {
        // Giả sử bạn lấy username từ Cookie hoặc Claims của người dùng đã đăng nhập
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return Unauthorized();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return NotFound();

        return Ok(new
        {
            fullName = user.FullName,
            username = user.Username
        });
    }
}