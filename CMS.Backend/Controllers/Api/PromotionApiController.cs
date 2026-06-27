using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // ĐÃ THÊM
using Microsoft.EntityFrameworkCore;
using CMS.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Backend.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class PromotionApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PromotionApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lấy chương trình khuyến mãi ĐANG CHẠY hiện tại
        [AllowAnonymous] // ĐÃ THÊM: Cho phép Frontend lấy dữ liệu không cần token
        [HttpGet("active")]
        public async Task<IActionResult> GetActivePromotion()
        {
            var now = DateTime.Now;
            var activePromo = await _context.Promotions
                .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now)
                .OrderByDescending(p => p.DiscountPercent) // Ưu tiên chương trình giảm nhiều nhất
                .FirstOrDefaultAsync();

            if (activePromo == null)
            {
                return Ok(new { discountPercent = 0 });
            }

            return Ok(new { discountPercent = activePromo.DiscountPercent });
        }
    }
}