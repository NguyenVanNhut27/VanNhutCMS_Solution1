namespace CMS.Data.Entities
{
    public enum UserRole
    {
        Admin = 0,      // Quản lý (Full quyền: Xem báo cáo, Thêm món)
        Cashier = 1,    // Thu ngân (Thanh toán, In bill)
        Chef = 2,       // Bếp (Màn hình xem món cần nấu)
        Waiter = 3      // Phục vụ (Màn hình Order món, xem sơ đồ bàn)
    }

    public enum TableStatus
    {
        Available = 0,  // Bàn trống
        Occupied = 1,   // Đang có khách
        Reserved = 2    // Đã đặt trước
    }

    public enum OrderStatus
    {
        Pending = 0,    // Mới gọi (Bếp chuẩn bị nấu)
        Serving = 1,    // Đang lên món (Khách đang dùng bữa)
        Completed = 2,  // Đã thanh toán (Hoàn thành)
        Cancelled = 3   // Hủy đơn (Khách bỏ về hoặc sai sót)
    }

    public enum PaymentMethod
    {
        Cash = 0,       // Tiền mặt
        BankTransfer = 1, // Chuyển khoản
        CreditCard = 2  // Quẹt thẻ
    }
}