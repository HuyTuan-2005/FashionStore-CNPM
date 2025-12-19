# FashionStore
#### Công nghệ phần mềm nhóm 1

Ứng dụng web bán quần áo xây dựng bằng ASP.NET MVC, cho phép người dùng xem sản phẩm, thêm vào giỏ hàng và đặt hàng online.

## Mục lục

- Giới thiệu
- Tính năng
- Công nghệ sử dụng
- Cài đặt
- Cách sử dụng
- Cấu trúc thư mục
- Hạn chế hiện tại

## Giới thiệu

FashionStore là project demo dành cho môn lập trình web, dùng để thực hành và báo cáo đồ án môn học C# với ASP.NET MVC, Entity Framework và SQL Server.  
Mục tiêu là mô phỏng một website bán quần áo cơ bản với đầy đủ luồng đăng ký, đăng nhập, xem sản phẩm, đặt hàng và trang quản lý cơ bản dành cho admin.

## Tính năng

- Đăng ký, đăng nhập, đăng xuất người dùng.
- Quản lý sản phẩm (thêm/sửa/xóa) dành cho admin.
- Xem danh sách sản phẩm, lọc theo loại, xem chi tiết.
- Giỏ hàng, cập nhật số lượng, tạo đơn hàng.
- Trang quản lý đơn hàng cho admin.

## Công nghệ sử dụng

- ASP.NET MVC trên .NET Framework v4.8.1.
- Entity Framework 6 (Database First, sử dụng file `.edmx`).
- SQL Server cho lưu trữ dữ liệu.
- Bootstrap 5 cho giao diện responsive.
- jQuery / JavaScript cho một số tương tác client-side.

## Cài đặt

### Điều kiện tiên quyết

- .NET SDK phù hợp với version project (ví dụ .NET 6).
- SQL Server (hoặc SQL Server Express).
- Visual Studio 2022 trở lên/ JetBrains Rider.

### Các bước

1. Clone repo: 
```
git clone https://github.com/HuyTuan-2005/FashionStore-CNPM.git
cd FashionStore
```

2. Khôi phục database:
- Mở folder `/Database` và chạy file script `.sql` trên SQL Server để tạo database và dữ liệu mẫu nếu `Models` không có sẵn.

3. Cấu hình connection string:
- Mở `Web.config`.
- Tìm `connectionStrings` và sửa `Data Source`, `Initial Catalog`, `User ID`, `Password` cho đúng SQL Server trên máy bạn.

4. Build và chạy:
- Set project web làm **Startup Project**.
- Nhấn **F5** để chạy bằng IIS Express.

## Cách sử dụng

- Truy cập địa chỉ `https://localhost:5000` sau khi project chạy.
- Đăng ký tài khoản mới hoặc dùng tài khoản demo (nếu đã cấu hình sẵn trong script SQL).
- Dùng tài khoản admin để quản lý sản phẩm và đơn hàng; dùng tài khoản user thường để trải nghiệm luồng mua hàng.

## Cấu trúc thư mục

- `/Controllers`: Chứa các controller chính như `HomeController`, `ProductController`, `CartController`, `OrderController`, `AccountController`.
- `/Models`: Chứa các lớp entity sinh từ `DataModel.edmx` và các view model nếu có.
- `/Views`: Razor views, chia folder theo tên controller (Home, Product, Cart, Order, Account…).
- `/Content`: CSS, ảnh, file Bootstrap.
- `/Scripts`: jQuery, Bootstrap JS và các script tùy chỉnh.

## Hạn chế hiện tại

- Chưa có phân quyền chi tiết (role-based) cho các chức năng nhạy cảm.
- Validation phía server và client còn đơn giản, chưa kiểm soát hết các trường hợp edge case.
- Chưa tối ưu hiệu năng truy vấn khi dữ liệu lớn.

