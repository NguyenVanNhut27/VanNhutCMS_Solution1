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
            // Trả về danh sách món ăn kèm Giá và Mã danh mục để React lọc
            var products = await _context.Products
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    price = p.Price,
                    categoryId = p.CategoryProductId, // Sửa lại đúng tên thuộc tính
                    // Nếu bạn có cột lưu ảnh, đổi "p.Image" thành tên cột ảnh của bạn
                    image = p.ImageUrl, // Sửa lại đúng tên thuộc tính ảnh
                    description = p.Description
                })
                .ToListAsync();

            return Ok(products);
        }
    }
}