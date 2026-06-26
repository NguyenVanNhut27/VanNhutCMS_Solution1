using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMS.Data;
using System.Threading.Tasks;
using System.Linq;
using System;
using CMS.Data.Entities;

namespace CMS.Backend.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiningTableApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DiningTableApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // ĐÃ SỬA: Bổ sung logic lấy thông tin Hóa đơn đang mở
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetTables()
        {
            // BƯỚC 1: Lấy dữ liệu thô từ Database (Bao gồm Bàn và Hóa đơn đang chờ)
            var rawTables = await _context.DiningTables
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Capacity,
                    t.Status,
                    // Lấy ra hóa đơn có trạng thái Pending của bàn này (nếu có)
                    ActiveOrder = _context.Orders
                        .Where(o => o.DiningTableId == t.Id && o.Status == OrderStatus.Pending)
                        .OrderByDescending(o => o.OrderTime)
                        .FirstOrDefault()
                })
                .ToListAsync();

            // BƯỚC 2: Map dữ liệu sang chuẩn JSON cho Next.js và định dạng giờ
            var tables = rawTables.Select(t => new
            {
                id = t.Id,
                name = t.Name,
                capacity = t.Capacity,
                status = t.Status.ToString().ToLower(),

                // Nếu có ActiveOrder thì lấy dữ liệu, không thì trả về 0 / rỗng
                guestCount = t.ActiveOrder != null ? t.ActiveOrder.GuestCount : 0,
                orderTotal = t.ActiveOrder != null ? t.ActiveOrder.TotalAmount : 0,
                time = t.ActiveOrder != null ? t.ActiveOrder.OrderTime.ToString("HH:mm") : ""
            }).ToList();

            return Ok(tables);
        }

        [HttpPost("{id}/open")]
        public async Task<IActionResult> OpenTable(int id, [FromQuery] int guestCount = 1)
        {
            var table = await _context.DiningTables.FindAsync(id);
            if (table == null)
            {
                return NotFound(new { message = "Không tìm thấy bàn này!" });
            }

            // Nếu bàn ĐANG CÓ KHÁCH, tìm hóa đơn Pending của bàn đó
            if (table.Status == TableStatus.Occupied)
            {
                var existingOrder = await _context.Orders
                    .Where(o => o.DiningTableId == id && o.Status == OrderStatus.Pending)
                    .FirstOrDefaultAsync();

                return Ok(new
                {
                    message = "Bàn đang phục vụ, chuyển đến trang gọi món.",
                    orderId = existingOrder?.Id
                });
            }

            // Nếu bàn TRỐNG, tiến hành tạo Hóa đơn mới (Mở bàn)
            var newOrder = new Order
            {
                DiningTableId = id,
                Status = OrderStatus.Pending,
                OrderTime = DateTime.Now,
                TotalAmount = 0,
                OrderType = "DineIn",
                GuestCount = guestCount             // Nhận chính xác số lượng khách từ Pop-up của ReactJS
            };

            _context.Orders.Add(newOrder);

            // Đổi trạng thái bàn thành Occupied
            table.Status = TableStatus.Occupied;
            _context.DiningTables.Update(table);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Mở bàn thành công!",
                orderId = newOrder.Id
            });
        }
        // ==========================================
        // API MỚI: Hủy bàn (Chỉ áp dụng nếu chưa có món)
        // ==========================================
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelTable(int id)
        {
            var table = await _context.DiningTables.FindAsync(id);
            if (table == null)
            {
                return NotFound(new { message = "Không tìm thấy bàn này!" });
            }

            // Tìm hóa đơn Pending của bàn đó
            var pendingOrder = await _context.Orders
                .Where(o => o.DiningTableId == id && o.Status == OrderStatus.Pending)
                .FirstOrDefaultAsync();

            if (pendingOrder != null)
            {
                // Kiểm tra an toàn: Nếu tổng tiền > 0 nghĩa là đã gọi món, không cho hủy ngang
                if (pendingOrder.TotalAmount > 0)
                {
                    return BadRequest(new { message = "Bàn này đã có món, không thể hủy! Hãy xóa món hoặc thanh toán trước." });
                }

                // Nếu chưa có món (Total = 0), xóa hóa đơn rỗng này đi
                _context.Orders.Remove(pendingOrder);
            }

            // Trả bàn về trạng thái Trống (Available)
            table.Status = TableStatus.Available;
            _context.DiningTables.Update(table);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã hủy bàn thành công!" });
        }
    }
}