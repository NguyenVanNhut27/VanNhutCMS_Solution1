using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMS.Data;
using System.Threading.Tasks;
using System.Linq;

namespace CMS.Backend.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PostApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetPosts()
        {
            // Lấy danh sách bài viết, sắp xếp bài mới nhất lên đầu
            // Đổi tên các trường (Title, Content, Image...) cho khớp với Model Post của bạn
            var posts = await _context.Posts
                .OrderByDescending(p => p.Id) // Hoặc p.CreatedAt nếu bạn có cột ngày tháng
                .Select(p => new
                {
                    id = p.Id,
                    title = p.Title,
                    content = p.Content, // Nội dung tóm tắt hoặc chi tiết
                    image = p.ImageUrl, // Sửa lại cho đúng với thuộc tính của Post
                    // Giả sử bạn có cột phân loại (Khuyến mãi, Món mới...)
                    // category = p.Category 
                })
                .ToListAsync();

            return Ok(posts);
        }
    }
}