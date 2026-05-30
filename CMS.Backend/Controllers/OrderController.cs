using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System;
using CMS.Data;
using CMS.Data.Entities;

namespace CMS.Backend.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin,Cashier,Chef")]
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.DiningTable)
                .Include(o => o.Customer)
                .OrderByDescending(o => o.OrderTime)
                .ToListAsync();
            return View(orders);
        }

        [Authorize(Roles = "Admin,Chef")]
        public async Task<IActionResult> KitchenQueue()
        {
            var pendingDetails = await _context.OrderDetails
                .Include(d => d.Product)
                .Include(d => d.Order)
                .ThenInclude(o => o.DiningTable)
                .Where(d => d.IsServed == false && d.Order.Status != OrderStatus.Completed)
                .ToListAsync();
            return View(pendingDetails);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.DiningTable)
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails).ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            return View(order);
        }

        [Authorize(Roles = "Admin,Chef")]
        public async Task<IActionResult> CompleteDish(int detailId)
        {
            var detail = await _context.OrderDetails.FindAsync(detailId);
            if (detail != null)
            {
                detail.IsServed = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(KitchenQueue));
        }
        // ==============================================================
        // 5. PHỤC VỤ MỞ BÀN & TẠO ĐƠN HÀNG MỚI
        // ==============================================================
        [Authorize(Roles = "Admin,Waiter")]
        [HttpPost]
        public async Task<IActionResult> CreateOrder(int tableId)
        {
            // Kiểm tra xem bàn có tồn tại và đang trống không
            var table = await _context.DiningTables.FindAsync(tableId);
            if (table == null || table.Status != TableStatus.Available)
            {
                return BadRequest("Bàn không hợp lệ hoặc đang có khách.");
            }

            // Tạo hóa đơn mới
            var order = new Order
            {
                DiningTableId = tableId,
                OrderTime = DateTime.Now,
                Status = OrderStatus.Pending
            };

            // Khóa bàn lại (Chuyển sang trạng thái Đang có khách)
            table.Status = TableStatus.Occupied;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Chuyển hướng sang trang Chi tiết đơn hàng để bắt đầu chọn món
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        // ==============================================================
        // 6. PHỤC VỤ THÊM MÓN VÀO ĐƠN HÀNG (ORDER MÓN)
        // ==============================================================
        [Authorize(Roles = "Admin,Waiter")]
        [HttpPost]
        public async Task<IActionResult> AddItem(int orderId, int productId, int quantity, string note)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            // Lưu chi tiết món khách vừa gọi
            var orderDetail = new OrderDetail
            {
                OrderId = orderId,
                ProductId = productId,
                Quantity = quantity,
                UnitPrice = product.Price, // Chốt giá tại thời điểm gọi món
                Note = note, // Ít đá, không hành...
                IsServed = false // Trạng thái: Bếp chưa nấu
            };

            _context.OrderDetails.Add(orderDetail);
            await _context.SaveChangesAsync();

            // Tải lại trang chi tiết để thấy món vừa thêm
            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        // ==============================================================
        // BẢN CẬP NHẬT TRỌNG TÂM: TRỪ TỒN KHO KHI THANH TOÁN
        // ==============================================================
        [Authorize(Roles = "Admin,Cashier")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(int orderId, PaymentMethod method)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Lấy hóa đơn KÈM THÔNG TIN MÓN ĂN (Product) để trừ kho
                    var order = await _context.Orders
                        .Include(o => o.OrderDetails)
                        .ThenInclude(d => d.Product)
                        .FirstOrDefaultAsync(o => o.Id == orderId);

                    if (order == null) return NotFound();

                    order.TotalAmount = order.OrderDetails.Sum(d => d.Quantity * d.UnitPrice);
                    order.Status = OrderStatus.Completed;
                    order.PaymentTime = DateTime.Now;
                    order.PaymentMethod = method;

                    // 1. LOGIC TRỪ TỒN KHO
                    foreach (var detail in order.OrderDetails)
                    {
                        if (detail.Product != null)
                        {
                            detail.Product.StockQuantity -= detail.Quantity; // Phép trừ

                            // Ngăn chặn trường hợp số lượng kho bị âm
                            if (detail.Product.StockQuantity < 0)
                            {
                                detail.Product.StockQuantity = 0;
                            }
                            _context.Products.Update(detail.Product);
                        }
                    }

                    // 2. GIẢI PHÓNG BÀN ĂN
                    if (order.DiningTableId.HasValue)
                    {
                        var table = await _context.DiningTables.FindAsync(order.DiningTableId.Value);
                        if (table != null) table.Status = TableStatus.Available;
                    }

                    // 3. CỘNG ĐIỂM CHO KHÁCH HÀNG (Mỗi 100k = 1 điểm)
                    if (order.CustomerId.HasValue)
                    {
                        var customer = await _context.Customers.FindAsync(order.CustomerId.Value);
                        if (customer != null)
                        {
                            customer.RewardPoints += (int)(order.TotalAmount / 100000);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return RedirectToAction(nameof(Details), new { id = orderId });
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
        }
    }
}