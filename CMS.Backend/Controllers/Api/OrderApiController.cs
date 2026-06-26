using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMS.Data;
using CMS.Data.Entities;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace CMS.Backend.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OrderApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. API: Lấy danh sách món đã gọi của hóa đơn
        // ==========================================
        [HttpGet("{orderId}/items")]
        public async Task<IActionResult> GetOrderItems(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return NotFound(new { message = "Không tìm thấy hóa đơn này!" });
            }

            var items = await _context.OrderDetails
                .Where(od => od.OrderId == orderId)
                .Select(od => new
                {
                    id = od.Id,
                    productId = od.ProductId,
                    name = od.Product.Name,
                    originalPrice = od.Product.Price, // ĐÃ THÊM: Trả về giá gốc ban đầu của món ăn
                    price = od.UnitPrice,             // Giá thực tế chốt trong hóa đơn (đã giảm nếu có)
                    quantity = od.Quantity,
                    image = od.Product.ImageUrl,
                    total = od.UnitPrice * od.Quantity,
                    isServed = od.IsServed,
                    orderTime = od.Order.OrderTime,

                    // ĐÃ THÊM: Tự động tính toán % giảm giá thực tế của món này tại thời điểm gọi món
                    discountPercent = od.Product.Price > od.UnitPrice && od.Product.Price > 0
                        ? (int)System.Math.Round((double)(od.Product.Price - od.UnitPrice) / (double)od.Product.Price * 100)
                        : 0
                })
                .ToListAsync();

            return Ok(items);
        }
        // ==========================================
        // 2. API: Nhận món từ Giỏ hàng và Gửi vào bếp
        // ==========================================
        [HttpPost("{orderId}/items")]
        public async Task<IActionResult> AddItemsToOrder(int orderId, [FromBody] List<CartItemDto> cartItems)
        {
            if (cartItems == null || !cartItems.Any())
            {
                return BadRequest(new { message = "Giỏ hàng trống, không thể gửi bếp!" });
            }

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return NotFound(new { message = "Không tìm thấy hóa đơn này!" });
            }

            if (order.Status != OrderStatus.Pending)
            {
                return BadRequest(new { message = "Hóa đơn này đã hoàn thành hoặc đã bị hủy, không thể gọi thêm món!" });
            }

            // ------------------------------------------------------------------------------
            // ĐÃ THÊM: Tìm chương trình khuyến mãi Đang kích hoạt & Trong thời gian áp dụng
            // ------------------------------------------------------------------------------
            var now = System.DateTime.Now;
            var activePromo = await _context.Promotions
                .FirstOrDefaultAsync(p => p.IsActive && p.StartDate <= now && p.EndDate >= now);
            int discountPercent = activePromo?.DiscountPercent ?? 0;
            // ------------------------------------------------------------------------------

            foreach (var item in cartItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null) continue;

                // Tính toán giá bán thực tế sau khi áp dụng giảm giá chương trình (nếu có)
                decimal finalUnitPrice = product.Price;

                // ĐÃ SỬA Ở ĐÂY: Thêm điều kiện product.IsSale == true để chỉ giảm giá món được phép Sale
                if (discountPercent > 0 && product.IsSale == true)
                {
                    finalUnitPrice = product.Price * (100 - discountPercent) / 100;
                }

                var existingDetail = await _context.OrderDetails
                    .FirstOrDefaultAsync(od => od.OrderId == orderId && od.ProductId == item.ProductId);

                if (existingDetail != null)
                {
                    // Nếu món ăn đã có sẵn, cộng dồn số lượng và cập nhật lại theo giá ưu đãi mới nhất
                    existingDetail.Quantity += item.Quantity;
                    existingDetail.UnitPrice = finalUnitPrice;
                    _context.OrderDetails.Update(existingDetail);
                }
                else
                {
                    var orderDetail = new OrderDetail
                    {
                        OrderId = orderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = finalUnitPrice // Chốt giá đã giảm trực tiếp vào DB
                    };
                    _context.OrderDetails.Add(orderDetail);
                }

                // Cộng dồn tổng tiền hóa đơn dựa trên giá thực tế đã tính giảm giá
                order.TotalAmount += finalUnitPrice * item.Quantity;
            }

            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã gửi món vào bếp và cập nhật hóa đơn thành công!" });
        }
        // ==========================================
        // 3. API: Xác nhận Thanh Toán và Đóng bàn
        // ==========================================
        [HttpPost("{orderId}/checkout")]
        public async Task<IActionResult> CheckoutOrder(int orderId, [FromBody] CheckoutDto request)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound(new { message = "Không tìm thấy hóa đơn này!" });

            // NẾU LÀ VNPAY -> KHÔNG ĐÓNG BÀN NGAY, MÀ TRẢ VỀ URL
            if (request.PaymentMethod == "vnpay")
            {
                // 1. Dùng thư viện VNPay tạo URL (Truyền thêm HttpContext để lấy IP)
                string vnpayUrl = CreateVnPayUrl(order.Id, order.TotalAmount, HttpContext);

                // 2. Trả về cho Next.js để nó redirect
                return Ok(new { paymentUrl = vnpayUrl });
            }

            // NẾU LÀ TIỀN MẶT/CHUYỂN KHOẢN -> THANH TOÁN VÀ ĐÓNG BÀN LUÔN
            order.Status = OrderStatus.Completed;
            _context.Orders.Update(order);

            // BỔ SUNG: Giải phóng bàn khi thanh toán bằng Tiền Mặt
            var table = await _context.DiningTables.FindAsync(order.DiningTableId);
            if (table != null)
            {
                table.Status = TableStatus.Available;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Thanh toán thành công!" });
        }

        // ==========================================
        // 4. API: Nhận kết quả trả về từ VNPay
        // ==========================================
        [HttpGet("vnpay-return")]
        public async Task<IActionResult> VnPayReturn()
        {
            // Lấy toàn bộ Query String
            var queryDictionary = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
            string vnp_HashSecret = "S8KQJLWL5RV3F44A9OV7GVI0OOBNRX8H"; // Test Secret Key

            if (!queryDictionary.ContainsKey("vnp_SecureHash"))
            {
                return BadRequest(new { message = "Giao dịch không hợp lệ!" });
            }

            string vnp_SecureHash = queryDictionary["vnp_SecureHash"];
            queryDictionary.Remove("vnp_SecureHash");
            queryDictionary.Remove("vnp_SecureHashType");

            // Tạo chuỗi SignData
            var sortedDict = new System.Collections.Generic.SortedList<string, string>(queryDictionary, System.StringComparer.Ordinal);
            var sb = new System.Text.StringBuilder();
            foreach (var kvp in sortedDict)
            {
                if (!string.IsNullOrEmpty(kvp.Value) && kvp.Key.StartsWith("vnp_"))
                {
                    sb.Append(System.Net.WebUtility.UrlEncode(kvp.Key) + "=" + System.Net.WebUtility.UrlEncode(kvp.Value) + "&");
                }
            }
            string signData = sb.ToString().TrimEnd('&');

            // Mã hóa kiểm tra
            string checkSum;
            using (var hmac = new System.Security.Cryptography.HMACSHA512(System.Text.Encoding.UTF8.GetBytes(vnp_HashSecret)))
            {
                var hashValue = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signData));
                var hashSb = new System.Text.StringBuilder();
                foreach (var b in hashValue) hashSb.Append(b.ToString("x2"));
                checkSum = hashSb.ToString();
            }

            if (checkSum != vnp_SecureHash)
            {
                return BadRequest(new { message = "Chữ ký không hợp lệ!" });
            }

            if (queryDictionary["vnp_ResponseCode"] != "00")
            {
                return BadRequest(new { message = "Giao dịch thất bại hoặc đã bị hủy!" });
            }

            // Giao dịch thành công -> Đóng bàn
            string txnRef = queryDictionary["vnp_TxnRef"];
            int orderId = int.Parse(txnRef.Split('_')[0]);

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound(new { message = "Không tìm thấy hóa đơn!" });

            if (order.Status != OrderStatus.Completed)
            {
                order.Status = OrderStatus.Completed;
                _context.Orders.Update(order);

                var table = await _context.DiningTables.FindAsync(order.DiningTableId);
                if (table != null)
                {
                    table.Status = TableStatus.Available; // Đổi bàn thành trống
                }

                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Thanh toán thành công và đã đóng bàn!" });
        }


        // DTO nhận dữ liệu giỏ hàng từ Next.js gửi lên
        public class CartItemDto
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }

        // DTO nhận dữ liệu Thanh toán từ Next.js gửi lên
        public class CheckoutDto
        {
            public string PaymentMethod { get; set; } = string.Empty;
            public decimal AmountGiven { get; set; }
        }

        /// <summary>
        /// Generates a VNPay payment URL for the given order.
        /// </summary>
        /// <param name="orderId">The order ID.</param>
        /// <param name="amount">The total amount to pay.</param>
        /// <param name="context">The HttpContext to get the IP address.</param>
        /// <returns>VNPay payment URL as a string.</returns>
        private string CreateVnPayUrl(int orderId, decimal amount, Microsoft.AspNetCore.Http.HttpContext context)
        {
            // SỬA Ở ĐÂY: Đá về trang vnpay-return của Next.js
            string vnp_Returnurl = "http://localhost:3000/vnpay-return";
            string vnp_Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

            // Cấu hình từ email môi trường Test của bạn
            string vnp_TmnCode = "A881Z4R6";
            string vnp_HashSecret = "S8KQJLWL5RV3F44A9OV7GVI0OOBNRX8H";

            // 1. Sử dụng SortedList với System.StringComparer.Ordinal để đảm bảo sort chính xác 100%
            var vnp_Params = new System.Collections.Generic.SortedList<string, string>(System.StringComparer.Ordinal)
            {
                { "vnp_Amount", ((long)(amount * 100)).ToString() }, // VNPay yêu cầu nhân 100
                { "vnp_Command", "pay" },
                { "vnp_CreateDate", System.DateTime.Now.ToString("yyyyMMddHHmmss") },
                { "vnp_CurrCode", "VND" },
                { "vnp_IpAddr", "127.0.0.1" }, // Ép cứng IPv4 thay vì lấy IP động để tránh lỗi "::1"
                { "vnp_Locale", "vn" },
                { "vnp_OrderInfo", "ThanhToanHoaDon_" + orderId }, // Bỏ khoảng trắng để tránh lỗi mã hóa URL Encode
                { "vnp_OrderType", "other" },
                { "vnp_ReturnUrl", vnp_Returnurl },
                { "vnp_TmnCode", vnp_TmnCode },
                { "vnp_TxnRef", orderId.ToString() + "_" + System.DateTime.Now.Ticks.ToString() },
                { "vnp_Version", "2.1.0" }
            };

            // 2. Build chuỗi QueryString và SignData chuẩn VNPay
            var sb = new System.Text.StringBuilder();
            foreach (var kvp in vnp_Params)
            {
                if (!string.IsNullOrEmpty(kvp.Value))
                {
                    sb.Append(System.Net.WebUtility.UrlEncode(kvp.Key) + "=" + System.Net.WebUtility.UrlEncode(kvp.Value) + "&");
                }
            }

            string queryString = sb.ToString();
            string signData = queryString.TrimEnd('&'); // Xóa dấu & thừa ở cuối

            // 3. Băm chuỗi SHA512 tạo SecureHash (Chuyển byte sang chữ thường x2)
            string vnp_SecureHash;
            using (var hmac = new System.Security.Cryptography.HMACSHA512(System.Text.Encoding.UTF8.GetBytes(vnp_HashSecret)))
            {
                var hashValue = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signData));
                var hashSb = new System.Text.StringBuilder();
                foreach (var b in hashValue)
                {
                    hashSb.Append(b.ToString("x2"));
                }
                vnp_SecureHash = hashSb.ToString();
            }

            // 4. Trả về Link hoàn chỉnh
            return $"{vnp_Url}?{queryString}vnp_SecureHash={vnp_SecureHash}";
        }
    }
}