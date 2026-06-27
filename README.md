# 🍔 Hệ Thống Quản Lý & Gọi Món Nhà Hàng (Restaurant POS & Ordering System)

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Next.js](https://img.shields.io/badge/Next.js-000000?style=for-the-badge&logo=next.js&logoColor=white)
![Tailwind CSS](https://img.shields.io/badge/Tailwind_CSS-38B2AC?style=for-the-badge&logo=tailwind-css&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=for-the-badge&logo=microsoft-sql-server&logoColor=white)
![VNPay](https://img.shields.io/badge/Payment-VNPay-blue?style=for-the-badge)

Một hệ thống Point of Sale (POS) và gọi món tại bàn toàn diện dành cho nhà hàng/quán ăn. Ứng dụng bao gồm một trang Admin (quản lý thực đơn, bàn ăn, khuyến mãi) và một trang Client App (dành cho nhân viên/khách hàng gọi món trực tiếp), tích hợp thanh toán trực tuyến qua cổng VNPay.

---

## ✨ Chức Năng Nổi Bật (Key Features)

### 📱 Dành cho Client (Frontend - Next.js)
* **Giao diện Gọi món:** Trực quan, tối ưu UI/UX, hỗ trợ Responsive trên mọi thiết bị.
* **Bộ lọc & Tìm kiếm:** Lọc món ăn theo danh mục, tìm kiếm bằng từ khóa, đánh dấu các "Món mới" và "Đang Sale".
* **Phân trang (Pagination):** Xử lý mượt mà danh sách thực đơn lớn (20 món/trang).
* **Quản lý Giỏ hàng:** Thêm, bớt, tùy chỉnh số lượng món ăn linh hoạt.
* **Tự động áp dụng Khuyến mãi:** Hệ thống tự động nhận diện chương trình khuyến mãi đang diễn ra và tính toán lại giá tiền, hiển thị giá gốc (gạch ngang) và thẻ SALE.

### ⚙️ Dành cho Admin (Backend - ASP.NET Core MVC)
* **Quản lý Thực đơn (Products):** Thêm, sửa, xóa món ăn. Quản lý hình ảnh và trạng thái món.
* **Quản lý Khuyến mãi (Promotions):** Lên lịch các chương trình giảm giá tự động theo thời gian cấu hình sẵn.
* **Kiểm soát Tồn kho:** Theo dõi số lượng nguyên liệu/món ăn, thiết lập mức cảnh báo khi sắp hết hàng (`LowStockThreshold`).
* **Quản lý Bàn ăn:** Theo dõi trạng thái bàn (Trống, Đang phục vụ), tự động giải phóng bàn sau khi thanh toán.
* **Tích hợp VNPay:** Xử lý thanh toán trực tuyến, mã hóa bảo mật `SHA512`, kiểm tra tính toàn vẹn của giao dịch (Checksum).

---

## 🛠️ Công Nghệ Sử Dụng (Tech Stack)

### Backend
* **Framework:** ASP.NET Core MVC & Web API
* **ORM:** Entity Framework Core
* **Database:** Microsoft SQL Server
* **Authentication:** Cookie Authentication / JWT (Role-based Authorization: Admin/User)
* **API Documentation:** Swagger (Swashbuckle)

### Frontend
* **Framework:** Next.js (React)
* **Language:** TypeScript
* **Styling:** Tailwind CSS
* **State Management:** React Context API (`AuthContext`, `CartContext`)

---

