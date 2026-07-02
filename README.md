# 🍔 Hệ Thống Quản Lý & Gọi Món Nhà Hàng (Quick Order POS System)

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Next.js](https://img.shields.io/badge/Next.js-000000?style=for-the-badge&logo=next.js&logoColor=white)
![TypeScript](https://img.shields.io/badge/TypeScript-007ACC?style=for-the-badge&logo=typescript&logoColor=white)
![Tailwind CSS](https://img.shields.io/badge/Tailwind_CSS-38B2AC?style=for-the-badge&logo=tailwind-css&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=for-the-badge&logo=microsoft-sql-server&logoColor=white)
![VNPay](https://img.shields.io/badge/Payment-VNPay-blue?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-green.svg?style=for-the-badge)

Một hệ thống Point of Sale (POS) và gọi món tại bàn toàn diện được thiết kế chuyên biệt để tối ưu hóa quy trình vận hành cho nhà hàng, quán ăn và quán cafe. 

Hệ thống được xây dựng theo kiến trúc phân tách (Decoupled Architecture), bao gồm một ứng dụng Web/Tablet (Frontend Next.js) dành cho nhân viên phục vụ thao tác order cực nhanh, và một hệ thống máy chủ Quản trị (Backend ASP.NET Core) để xử lý logic, quản lý nhà bếp, tồn kho và tích hợp thanh toán trực tuyến qua cổng VNPay.

---

## ✨ Chức Năng Nổi Bật (Key Features)

Hệ thống bao quát toàn bộ luồng nghiệp vụ thực tế, phân chia rõ ràng theo 4 vai trò (Role):

### 📱 1. Dành cho Phục vụ (Frontend - Next.js)
* **Giao diện Quick Order:** Trực quan, tối ưu UI/UX cho màn hình cảm ứng, hỗ trợ Responsive trên Tablet/Mobile.
* **Bộ lọc & Tìm kiếm thông minh:** Tìm kiếm món ăn thời gian thực (Real-time search), lọc theo danh mục, đánh dấu các "Món mới" và "Đang Sale".
* **Phân trang (Pagination):** Xử lý mượt mà danh sách thực đơn lớn (hiển thị 20 món/trang) không gây giật lag.
* **Quản lý Giỏ hàng & Bàn ăn:** Mở bàn, thêm/bớt món, tùy chỉnh số lượng linh hoạt, hỗ trợ điền ghi chú chế biến (VD: "Ít đá", "Không hành") và gửi trực tiếp lệnh xuống bếp.
* **Tự động áp dụng Khuyến mãi:** Nhận diện chương trình Sale đang diễn ra, tự động tính toán lại giá tiền, hiển thị giá gốc (gạch ngang) và thẻ SALE.

### 🍳 2. Dành cho Nhà bếp (Kitchen Display System - KDS)
* **Nhận Order Thời gian thực:** Bếp nhận ngay lập tức danh sách món cần làm khi nhân viên phục vụ bấm "Gửi Bếp".
* **Điều phối Trạng thái:** Chuyển đổi trạng thái món ăn linh hoạt (`Chờ nấu` ➔ `Đang chế biến` ➔ `Đã xong`) để nhân viên phục vụ nhận diện và mang món ra bàn.

### 💵 3. Dành cho Thu ngân (Cashier)
* **Quản lý Sơ đồ bàn:** Theo dõi trực quan trạng thái bàn (Trống, Đang phục vụ, Chờ thanh toán).
* **Thanh toán & Giải phóng bàn:** In hóa đơn tạm tính, xuất hóa đơn chi tiết và tự động đưa bàn về trạng thái "Trống" sau khi hoàn tất.
* **Tích hợp VNPay:** Xử lý thanh toán trực tuyến tự động qua mã QR/Link.

### ⚙️ 4. Dành cho Admin (Backend - ASP.NET Core MVC)
* **Quản lý Thực đơn (Products):** Thêm, sửa, xóa món ăn. Quản lý hình ảnh và trạng thái món.
* **Quản lý Khuyến mãi (Promotions):** Lên lịch các chương trình giảm giá tự động theo thời gian cấu hình sẵn.
* **Kiểm soát Tồn kho (Inventory):** Lập "Phiếu nhập kho" (Master-Detail). Tự động cộng dồn và theo dõi số lượng nguyên liệu, thiết lập cảnh báo khi sắp hết hàng (`LowStockThreshold`).
* **Quản trị Nhân sự:** Phân quyền chặt chẽ theo Role (Admin/User/Cashier/Chef).

---

## 🔍 Phân Tích Chuyên Sâu Kiến Trúc & Logic Nghiệp Vụ (Deep Dive)

### 1. Tính năng Tìm kiếm & Bộ lọc Thông minh (Smart Search & Filtering)
* **Góc độ Nghiệp vụ:** Trong giờ cao điểm, nhân viên phục vụ không có thời gian lướt tìm từng món ăn trong một thực đơn hàng trăm món. Việc tìm kiếm phải diễn ra ngay lập tức khi nhân viên gõ chữ cái đầu tiên (Real-time).
* **Luồng Kỹ thuật:**
  * **Frontend (Next.js):** Sử dụng các Hook cơ bản như `useState` để lưu trữ từ khóa tìm kiếm (`searchTerm`) và danh mục đang chọn (`activeCategory`).
  * **Backend (.NET Core):** Cung cấp API `GET /api/products?keyword=...&categoryId=...`. Hệ thống sử dụng LINQ để truy vấn Database: `_context.Products.Where(p => p.Name.Contains(keyword))`.
* **Điểm Tối ưu (Highlight) - Kỹ thuật Debouncing:** Nếu nhân viên gõ từ "Cà phê sữa" (11 ký tự), hệ thống thông thường sẽ gọi API 11 lần liên tục, gây quá tải Server (DDoS cục bộ). Hệ thống được tối ưu bằng kỹ thuật Debounce. Frontend được lập trình để trì hoãn 300ms sau khi nhân viên ngừng gõ phím thì mới gửi Request cuối cùng lên máy chủ. Điều này giảm thiểu 90% lượng Request rác.

### 2. Quản lý Giỏ hàng & Chốt Order (Cart Management & Snapshot)
* **Góc độ Nghiệp vụ:** Nhân viên chọn món, điều chỉnh số lượng, ghi chú từng món (VD: Phở - không hành) và nhấn "Gửi Bếp". Hóa đơn phải lưu lại chính xác số tiền tại thời điểm khách gọi món.
* **Luồng Kỹ thuật:**
  * **Frontend:** Sử dụng `React Context API` (cụ thể là `CartContext`) để lưu trữ giỏ hàng tạm thời trên RAM của máy tính bảng.
  * **Backend:** API `POST /api/orders` tiếp nhận một chuỗi JSON lồng nhau (Master-Detail) gồm thông tin Bàn (Order) và Danh sách món (OrderDetail).
* **Điểm Tối ưu (Highlight) - Bảo toàn giá trị bằng Snapshot:** Điểm ăn tiền ở đây là kiến trúc Database. Bảng `OrderDetail` bắt buộc phải có cột `UnitPrice`. Khi lưu đơn hàng, Backend sẽ lấy giá hiện tại của món ăn copy vào cột `UnitPrice` này. Nhờ vậy, nếu ngày mai Admin tăng giá món "Phở" từ 40k lên 50k, thì hóa đơn của ngày hôm nay khi xem lại vẫn giữ nguyên giá 40k. Báo cáo doanh thu sẽ không bao giờ bị sai lệch.

### 3. Phân hệ Nhà bếp (Kitchen Display System - KDS)
* **Góc độ Nghiệp vụ:** Khu vực bếp luôn ồn ào và lộn xộn. Bếp trưởng cần một màn hình chỉ hiển thị những món đang chờ nấu theo thứ tự ưu tiên (Ai gọi trước nấu trước), và có thể báo cáo "Đã xong" để phục vụ ra lấy.
* **Luồng Kỹ thuật:**
  * Bảng `OrderDetail` có một trường dữ liệu `KitchenStatus` (Kiểu Enum: 0 = Chờ nấu, 1 = Đang chế biến, 2 = Đã xong).
  * Màn hình bếp (Frontend) sẽ liên tục gọi API `GET` để lấy các món có trạng thái 0 và 1. Khi bếp bấm hoàn thành, Frontend gọi API `PUT /api/orders/details/{id}/status` để cập nhật thành 2.
* **Điểm Tối ưu (Highlight) - Quản lý luồng trạng thái (State Machine):** Tính năng này tách biệt hoàn toàn ranh giới công việc. Nhân viên phục vụ chỉ có quyền thêm món (tạo status 0). Chỉ có tài khoản Bếp mới có quyền thay đổi status lên 1 và 2. (Trong tương lai, tính năng này nên được tối ưu bằng SignalR / WebSockets thay vì gọi API liên tục để màn hình bếp tự động nhảy món mới mà không cần F5).

### 4. Quản lý Sơ đồ bàn & Thanh toán (Table Management & Checkout)
* **Góc độ Nghiệp vụ:** Thu ngân cần biết bàn nào đang ăn, bàn nào đã ăn xong chờ tính tiền để in hóa đơn và dọn bàn đón khách mới.
* **Luồng Kỹ thuật:**
  * Bảng `Order` có trường `Status` (0 = Đang phục vụ, 1 = Chờ thanh toán, 2 = Đã thanh toán).
  * Khi nhân viên phục vụ bấm "Yêu cầu thanh toán", bàn chuyển sang trạng thái 1 (Khóa không cho gọi món thêm). Khi thu ngân bấm "Hoàn tất", bàn chuyển sang trạng thái 2 (Giải phóng sơ đồ bàn).
* **Điểm Tối ưu (Highlight) - Khóa trạng thái (State Lock):** Hệ thống giải quyết được bài toán xung đột (Concurrency). Giả sử thu ngân đang in hóa đơn tính tiền, nhân viên phục vụ ở ngoài sân không thể vô tình ấn thêm một ly nước vào bàn đó nữa. Hệ thống Backend sẽ từ chối Request thêm món nếu `Order.Status == 1`.

### 5. Tích hợp Thanh toán trực tuyến VNPay
* **Góc độ Nghiệp vụ:** Nhà hàng cung cấp mã QR để khách thanh toán trực tuyến qua thẻ ngân hàng/ví điện tử. Hệ thống phải tự động nhận biết khách đã chuyển tiền thành công để đóng hóa đơn.
* **Luồng Kỹ thuật:**
  * Backend tạo một URL chứa thông tin số tiền, mã hóa đơn và gửi sang VNPay. VNPay hiển thị cổng thanh toán cho khách.
  * Khách thanh toán xong, VNPay sẽ gọi ngược lại một API của máy chủ (gọi là Webhook / Return URL) để báo kết quả.
* **Điểm Tối ưu (Highlight) - Thuật toán Băm mã hóa (SHA-512 Checksum):** Làm sao để biết kết quả "Thành công" là do VNPay gửi về chứ không phải do một Hacker dùng Postman gửi giả mạo? Hệ thống Backend xử lý bằng cách: Ghép tất cả các tham số lại, kết hợp với một chuỗi `HashSecret` (Chỉ VNPay và máy chủ của bạn biết), sau đó mã hóa bằng thuật toán SHA-512 để ra một chuỗi Checksum (Chữ ký điện tử). Backend sẽ so sánh chữ ký này với chữ ký VNPay gửi về, nếu khớp 100% thì mới cập nhật trạng thái "Đã thanh toán".

### 6. Quản lý Nhập Kho & Cảnh báo Tồn (Inventory Master-Detail)
* **Góc độ Nghiệp vụ:** Mỗi khi nhập nguyên liệu (Bia, Nước ngọt), quản lý tạo Phiếu nhập. Số lượng trong kho phải tăng lên. Mỗi khi khách uống một lon, số lượng trong kho phải giảm đi. Nếu kho sắp hết, hệ thống phải báo động.
* **Luồng Kỹ thuật:**
  * Bảng `Product` có 2 cột: `StockQuantity` (Tồn kho hiện tại) và `LowStockThreshold` (Ngưỡng cảnh báo, ví dụ: 10).
  * Mỗi khi API `/api/orders` thành công -> `StockQuantity` trừ đi.
  * Mỗi khi API `/api/inventory` (Nhập kho) thành công -> `StockQuantity` cộng lên.
* **Điểm Tối ưu (Highlight) - Database Transaction (Giao dịch CSDL):** Đây là tính năng thể hiện trình độ Backend cao nhất. Khi lưu Phiếu nhập kho, hệ thống phải thực hiện 3 lệnh SQL: Lưu vỏ phiếu -> Lưu chi tiết phiếu -> Cập nhật cộng dồn Tồn kho. Backend .NET Core được bọc bằng cơ chế `IDbContextTransaction`. Nếu lúc đang cộng dồn tồn kho mà máy chủ bị cúp điện hoặc lỗi CSDL, cơ chế này sẽ Rollback (Hoàn tác) toàn bộ quá trình về lại vạch xuất phát. Tránh tình trạng tiền mặt đã chi ra ghi vào sổ, nhưng hàng trong kho lại không thấy tăng lên.

---
## 📂 Cấu Trúc Thư Mục (Folder Structure)

```text
📦 Quick-Order-System
 ┣ 📂 Backend                 # ASP.NET Core 8.0 Web API & MVC
 ┃ ┣ 📂 Controllers           # Xử lý các Endpoints API
 ┃ ┣ 📂 Data                  # ApplicationDbContext, Migrations
 ┃ ┣ 📂 Entities              # Models/Thực thể (Product, Order, Inventory...)
 ┃ ┣ 📂 Services              # Logic xử lý VNPay, File Upload, Auth
 ┃ ┣ 📂 Views                 # Giao diện Admin (Razor Pages)
 ┃ ┗ 📜 appsettings.json      # Cấu hình ConnectionString, VNPay Keys
 ┃
 ┗ 📂 Frontend                # Next.js App
   ┣ 📂 src
   ┃ ┣ 📂 components          # UI Components (Buttons, Modals, FoodCards)
   ┃ ┣ 📂 context             # AuthContext, CartContext
   ┃ ┣ 📂 pages               # Các trang chính (Home, Menu, Checkout, Login)
   ┃ ┣ 📂 services            # axiosClient.js (Cấu hình Interceptors)
   ┃ ┗ 📂 styles              # Tailwind CSS Config, Global Styles
   ┗ 📜 .env.local            # Chứa biến môi trường (API URL)
---

🔌 Danh Mục API Cơ Bản (API Reference)Dưới đây là một số Endpoints chính giao tiếp giữa Frontend và Backend:MethodEndpointDescriptionAuth RequiredPOST/api/auth/loginXác thực người dùng, trả về JWT Token❌GET/api/productsLấy danh sách món ăn (Hỗ trợ Pagination, Search)❌POST/api/ordersTạo mới một hóa đơn/order từ Giỏ hàng✅ (Waiter)PUT/api/orders/{id}/statusCập nhật trạng thái chế biến món ăn✅ (Chef)POST/api/payment/vnpayTạo URL thanh toán VNPay✅ (Cashier)POST/api/inventoryLập Phiếu nhập kho & tự động cộng dồn tồn kho✅ (Admin)

🚀 Hướng Dẫn Cài Đặt (Getting Started)
Yêu cầu hệ thống
.NET 8 SDK

Node.js (Phiên bản 18.x trở lên)

SQL Server

1. Triển khai Backend (.NET Core)
Bash
# Clone dự án về máy
git clone [https://github.com/your-username/quick-order-system.git](https://github.com/your-username/quick-order-system.git)

# Di chuyển vào thư mục Backend
cd quick-order-system/Backend

# 1. Mở file appsettings.json và cấu hình lại ConnectionStrings trỏ về SQL Server của bạn.
# 2. Chạy lệnh Migration để tạo Database và Seed dữ liệu mẫu:
dotnet ef database update

# Khởi chạy Backend Server
dotnet run
2. Triển khai Frontend (Next.js)
Bash
# Mở một terminal mới, di chuyển vào thư mục Frontend
cd quick-order-system/Frontend

# Cài đặt thư viện
npm install

# Tạo file .env.local và cấu hình biến môi trường kết nối API
echo "VITE_API_URL=https://localhost:7108/api/" > .env.local

# Khởi chạy ứng dụng Frontend
npm run dev
🔐 Cấu Hình VNPay (VNPay Setup)
Để kích hoạt thanh toán trực tuyến, đăng ký tài khoản Sandbox VNPay và bổ sung mã vào appsettings.json (Backend):

JSON
"Vnpay": {
  "TmnCode": "YOUR_TMN_CODE",
  "HashSecret": "YOUR_HASH_SECRET",
  "BaseUrl": "[https://sandbox.vnpayment.vn/paymentv2/vpcpay.html](https://sandbox.vnpayment.vn/paymentv2/vpcpay.html)",
  "ReturnUrl": "https://localhost:7108/api/payment/vnpay-return"
}
🌍 Hướng Dẫn Triển Khai (Deployment)
Frontend: Tối ưu hóa triển khai miễn phí trên Vercel. Chỉ cần liên kết tài khoản GitHub và thiết lập biến môi trường VITE_API_URL.

Backend & Database: Có thể triển khai lên Microsoft Azure (App Service & Azure SQL Database) hoặc máy chủ ảo VPS Linux/Windows thông qua IIS/Nginx reverse proxy.

🗺️ Lộ Trình Phát Triển (Roadmap)
[x] Tích hợp thanh toán VNPay

[x] Tính năng cảnh báo tồn kho nguyên liệu (Low Stock Alert)

[ ] Tích hợp WebSocket/SignalR để cập nhật màn hình Bếp theo thời gian thực (Real-time).

[ ] Báo cáo Doanh thu dạng Biểu đồ (Chart.js) cho Admin.

[ ] Ứng dụng Quét mã QR tự gọi món dành cho khách hàng (Self-order).
