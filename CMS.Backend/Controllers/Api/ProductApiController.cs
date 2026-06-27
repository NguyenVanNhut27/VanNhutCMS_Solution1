using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMS.Data;
using System.Threading.Tasks;
using System.Linq;

namespace CMS.Backend.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            // Trả về danh sách món ăn kèm Giá, Mã danh mục và các CỜ TRẠNG THÁI để React lọc
            var products = await _context.Products
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    price = p.Price,
                    categoryId = p.CategoryProductId,
                    image = p.ImageUrl,
                    description = p.Description,

                    // ĐÃ THÊM: Gửi 2 cờ này sang cho Next.js nhận diện trạng thái Sale/New
                    isSale = p.IsSale,
                    isNew = p.IsNew
                })
                .ToListAsync();

            return Ok(products);
        }
    }
}