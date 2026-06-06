using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using CMS.Data;
using CMS.Data.Entities;

namespace CMS.Backend.Controllers
{
    public class OrderDetailController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderDetailController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ĐÃ XÓA HÀM INDEX VÌ CHÚNG TA HIỂN THỊ CHUNG Ở TRANG ORDER/DETAILS RỒI

        // 1. GET: Gọi form Thêm món mới
        [HttpGet]
        public IActionResult Create(int orderId)
        {
            ViewBag.OrderId = orderId;
            ViewBag.Products = new SelectList(_context.Products, "Id", "Name");
            return View();
        }

        // 2. POST: Xử lý lưu món mới vào DB
        [HttpPost]
        public async Task<IActionResult> Create(OrderDetail orderDetail)
        {
            var product = await _context.Products.FindAsync(orderDetail.ProductId);
            if (product != null)
            {
                orderDetail.UnitPrice = product.Price;
            }

            _context.OrderDetails.Add(orderDetail);
            await _context.SaveChangesAsync();

            await UpdateOrderTotal(orderDetail.OrderId);

            // ĐÃ SỬA: Điều hướng thẳng về trang Details của Order
            return RedirectToAction("Details", "Order", new { id = orderDetail.OrderId });
        }

        // 3. GET: Gọi form Sửa số lượng món
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var orderDetail = await _context.OrderDetails
                .Include(od => od.Product)
                .FirstOrDefaultAsync(od => od.Id == id);

            if (orderDetail == null) return NotFound();

            return View(orderDetail);
        }

        // 4. POST: Xử lý lưu số lượng mới
        [HttpPost]
        public async Task<IActionResult> Edit(int id, OrderDetail orderDetail)
        {
            if (id != orderDetail.Id) return NotFound();

            _context.OrderDetails.Update(orderDetail);
            await _context.SaveChangesAsync();

            await UpdateOrderTotal(orderDetail.OrderId);

            // ĐÃ SỬA: Điều hướng thẳng về trang Details của Order
            return RedirectToAction("Details", "Order", new { id = orderDetail.OrderId });
        }

        // 5. Xử lý Xóa món (Click thẳng không cần ra form)
        public async Task<IActionResult> Delete(int id)
        {
            var orderDetail = await _context.OrderDetails.FindAsync(id);
            if (orderDetail != null)
            {
                int orderId = orderDetail.OrderId;

                _context.OrderDetails.Remove(orderDetail);
                await _context.SaveChangesAsync();

                await UpdateOrderTotal(orderId);

                // ĐÃ SỬA: Điều hướng thẳng về trang Details của Order
                return RedirectToAction("Details", "Order", new { id = orderId });
            }
            return RedirectToAction("Index", "Order");
        }

        // ==========================================
        // HÀM NGHIỆP VỤ: TỰ ĐỘNG TÍNH LẠI TỔNG TIỀN
        // ==========================================
        private async Task UpdateOrderTotal(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order != null)
            {
                order.TotalAmount = order.OrderDetails.Sum(od => od.Quantity * od.UnitPrice);

                _context.Orders.Update(order);
                await _context.SaveChangesAsync();
            }
        }
    }
}