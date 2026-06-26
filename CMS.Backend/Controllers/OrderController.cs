using CMS.Data;
using CMS.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

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
            var table = await _context.DiningTables.FindAsync(tableId);
            if (table == null || table.Status != TableStatus.Available)
            {
                return BadRequest("Bàn không hợp lệ hoặc đang có khách.");
            }

            var order = new Order
            {
                DiningTableId = tableId,
                OrderTime = DateTime.Now,
                Status = OrderStatus.Pending,
                OrderType = "DineIn", // Mặc định mở bàn là ăn tại quán
                GuestCount = 1
            };

            table.Status = TableStatus.Occupied;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

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

            var orderDetail = new OrderDetail
            {
                OrderId = orderId,
                ProductId = productId,
                Quantity = quantity,
                UnitPrice = product.Price,
                Note = note,
                IsServed = false
            };

            _context.OrderDetails.Add(orderDetail);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        // ==============================================================
        // 7. TRỪ TỒN KHO KHI THANH TOÁN
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
                            detail.Product.StockQuantity -= detail.Quantity;

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

        // ==============================================================
        // 8. ADMIN: SỬA HÓA ĐƠN (ĐỔI BÀN / GÁN KHÁCH HÀNG / CẬP NHẬT GHI CHÚ)
        // ==============================================================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null || order.Status == OrderStatus.Completed)
                return NotFound("Không tìm thấy đơn hoặc đơn đã thanh toán không thể sửa.");

            ViewBag.DiningTables = new SelectList(_context.DiningTables
                .Where(t => t.Status == TableStatus.Available || t.Id == order.DiningTableId), "Id", "Name", order.DiningTableId);

            // CẬP NHẬT: Định dạng lại danh sách Khách hàng để Select2 hiển thị được cả Tên + Số điện thoại
            var customersList = await _context.Customers.Select(c => new
            {
                Id = c.Id,
                DisplayText = c.FullName + " - " + (c.PhoneNumber ?? "Không có SĐT")
            }).ToListAsync();

            ViewBag.Customers = new SelectList(customersList, "Id", "DisplayText", order.CustomerId);

            return View(order);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Order updatedOrder)
        {
            if (id != updatedOrder.Id) return NotFound();

            var currentOrder = await _context.Orders.FindAsync(id);
            if (currentOrder == null) return NotFound();

            // Xử lý đổi bàn
            if (currentOrder.DiningTableId != updatedOrder.DiningTableId)
            {
                if (currentOrder.DiningTableId.HasValue)
                {
                    var oldTable = await _context.DiningTables.FindAsync(currentOrder.DiningTableId.Value);
                    if (oldTable != null) oldTable.Status = TableStatus.Available;
                }

                if (updatedOrder.DiningTableId.HasValue)
                {
                    var newTable = await _context.DiningTables.FindAsync(updatedOrder.DiningTableId.Value);
                    if (newTable != null) newTable.Status = TableStatus.Occupied;
                }
            }

            // Cập nhật thông tin mới
            currentOrder.DiningTableId = updatedOrder.DiningTableId;
            currentOrder.CustomerId = updatedOrder.CustomerId;
            currentOrder.OrderType = updatedOrder.OrderType;     // Lưu Loại đơn
            currentOrder.GuestCount = updatedOrder.GuestCount;   // Lưu Số lượng khách
            currentOrder.Note = updatedOrder.Note;               // Lưu Ghi chú

            _context.Orders.Update(currentOrder);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ==============================================================
        // 9. ADMIN: XÓA / HỦY ĐƠN HÀNG HOÀN TOÀN
        // ==============================================================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order != null)
            {
                if (order.DiningTableId.HasValue)
                {
                    var table = await _context.DiningTables.FindAsync(order.DiningTableId.Value);
                    if (table != null)
                    {
                        table.Status = TableStatus.Available;
                        _context.DiningTables.Update(table);
                    }
                }

                if (order.OrderDetails.Any())
                {
                    _context.OrderDetails.RemoveRange(order.OrderDetails);
                }

                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // ==============================================================
        // 10. ADMIN: TẠO ĐƠN HÀNG THỦ CÔNG
        // ==============================================================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.DiningTables = new SelectList(await _context.DiningTables.Where(t => t.Status == TableStatus.Available).ToListAsync(), "Id", "Name");

            // CẬP NHẬT: Gộp Tên và Số điện thoại cho Select2
            var customersList = await _context.Customers.Select(c => new
            {
                Id = c.Id,
                DisplayText = c.FullName + " - " + (c.PhoneNumber ?? "Không có SĐT")
            }).ToListAsync();

            ViewBag.Customers = new SelectList(customersList, "Id", "DisplayText");

            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order order)
        {
            order.OrderTime = DateTime.Now;
            order.Status = OrderStatus.Pending;

            if (order.DiningTableId.HasValue)
            {
                var table = await _context.DiningTables.FindAsync(order.DiningTableId.Value);
                if (table != null) table.Status = TableStatus.Occupied;
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = order.Id });
        }
    }
}