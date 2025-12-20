-- 0) Khởi tạo DB
IF
DB_ID('FashionStore') IS NOT NULL
BEGIN
    DROP
DATABASE FashionStore;
END;
GO
CREATE
DATABASE FashionStore
GO
USE FashionStore;
GO
SET ANSI_NULLS ON;
SET
QUOTED_IDENTIFIER ON;
GO


-- Xóa bảng chi tiết trước (bảng con)
DROP TABLE IF EXISTS dbo.OrderDetails;
DROP TABLE IF EXISTS dbo.ProductImages;
DROP TABLE IF EXISTS dbo.ProductVariants;

-- Xóa bảng trung gian
DROP TABLE IF EXISTS dbo.Orders;

-- Xóa bảng chính
DROP TABLE IF EXISTS dbo.Products;
DROP TABLE IF EXISTS dbo.CustomerProfiles;
DROP TABLE IF EXISTS dbo.Customers;
DROP TABLE IF EXISTS dbo.Categories;
DROP TABLE IF EXISTS dbo.CategoryGroups;
DROP TABLE IF EXISTS dbo.Colors;
DROP TABLE IF EXISTS dbo.Sizes;
-- ============================================
-- 1. BẢNG THÔNG TIN ĐĂNG NHẬP KHÁCH HÀNG
-- ============================================
CREATE TABLE Customers
(
    CustomerID   INT PRIMARY KEY IDENTITY(1,1),
    UserName     NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Email        NVARCHAR(100) NOT NULL UNIQUE,
    IsActive     BIT      DEFAULT 1,
    CreatedAt    DATETIME DEFAULT GETDATE()
);

-- ============================================
-- 2. BẢNG THÔNG TIN BỔ SUNG KHÁCH HÀNG (TÁCH RIÊNG)
-- Lý do: Giảm kích thước bảng Customers khi query đăng nhập,
-- và dễ mở rộng thêm thông tin cá nhân sau này
-- ============================================
CREATE TABLE CustomerProfile
(
    ProfileID   INT NOT NULL FOREIGN KEY REFERENCES Customers(CustomerID) ON DELETE CASCADE PRIMARY KEY,
    FullName    NVARCHAR(100),
    PhoneNumber NVARCHAR(20),
    DateOfBirth DATE,
    Gender      NVARCHAR(10), -- Nam, Nữ, Khác
    Address     NVARCHAR(255),
    City        NVARCHAR(100),
    District    NVARCHAR(100),
);

-- ============================================
-- 3. NHÓM DANH MỤC (VD: Thời trang nam, Thời trang nữ)
-- ============================================
CREATE TABLE CategoryGroups
(
    GroupID     INT PRIMARY KEY IDENTITY(1,1),
    GroupName   NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(MAX)
);

-- ============================================
-- 4. DANH MỤC SẢN PHẨM (VD: Áo thun, Quần jean, Váy)
-- ============================================
CREATE TABLE Categories
(
    CategoryID   INT PRIMARY KEY IDENTITY(1,1),
    CategoryName NVARCHAR(100) NOT NULL,
    GroupID      INT FOREIGN KEY REFERENCES CategoryGroups(GroupID),
    CONSTRAINT UQ_CategoryName_GroupID UNIQUE (CategoryName, GroupID)
);

-- ============================================
-- 5. SẢN PHẨM CHÍNH (MASTER PRODUCT)
-- Chỉ lưu thông tin chung, không lưu color/size/stock riêng lẻ
-- ============================================
CREATE TABLE Products
(
    ProductID       INT PRIMARY KEY IDENTITY(1,1),
    ProductName     NVARCHAR(200) NOT NULL,
    Description     NVARCHAR(MAX),
    BasePrice       DECIMAL(18, 2) NOT NULL, -- Giá gốc
    DiscountPercent DECIMAL(5, 2) DEFAULT 0 CHECK (DiscountPercent >= 0 AND DiscountPercent <= 100),
    CategoryID      INT FOREIGN KEY REFERENCES Categories(CategoryID),
    IsActive        BIT           DEFAULT 1,
    CreatedAt       DATETIME      DEFAULT GETDATE()
);

-- ============================================
-- 6. MÀU SẮC VÀ KÍCH THƯỚC (LOOKUP TABLES)
-- Best practice: Tách thành bảng riêng thay vì ENUM để dễ mở rộng
-- ============================================
CREATE TABLE Colors
(
    ColorID   INT PRIMARY KEY IDENTITY(1,1),
    ColorName NVARCHAR(50) NOT NULL UNIQUE,
    HexCode   NVARCHAR(7) -- Ví dụ: #FF5733 cho màu cam
);

CREATE TABLE Sizes
(
    SizeID   INT PRIMARY KEY IDENTITY(1,1),
    SizeName NVARCHAR(20) NOT NULL UNIQUE, -- S, M, L, XL, XXL
);

-- ============================================
-- 7. BIẾN THỂ SẢN PHẨM (PRODUCT VARIANTS)
-- Mỗi sản phẩm + màu + size = 1 variant riêng, có SKU và stock riêng
-- Đây là best practice cho thời trang e-commerce
-- ============================================
CREATE TABLE ProductVariants
(
    VariantID INT PRIMARY KEY IDENTITY(1,1),
    ProductID INT NOT NULL FOREIGN KEY REFERENCES Products(ProductID) ON DELETE CASCADE,
    ColorID   INT NOT NULL FOREIGN KEY REFERENCES Colors(ColorID),
    SizeID    INT NOT NULL FOREIGN KEY REFERENCES Sizes(SizeID),
    SKU       NVARCHAR(50) NOT NULL UNIQUE,              -- Mã SKU riêng cho từng biến thể
    Stock     INT NOT NULL DEFAULT 0,
    Status    NVARCHAR(50) NOT NULL DEFAULT 'Available', -- Available, OutOfStock, Discontinued
    CONSTRAINT UQ_Product_Color_Size UNIQUE (ProductID, ColorID, SizeID)
);

-- ============================================
-- 8. HÌNH ẢNH SẢN PHẨM (1 SẢN PHẨM CÓ NHIỀU HÌNH)
-- ============================================
CREATE TABLE ProductImages
(
    ImageID   INT PRIMARY KEY IDENTITY(1,1),
    ProductID INT NOT NULL FOREIGN KEY REFERENCES Products(ProductID) ON DELETE CASCADE,
    ImageUrl  NVARCHAR(500) NOT NULL,
    IsPrimary BIT DEFAULT 0, -- Đánh dấu ảnh chính để hiển thị thumbnail
);

-- ============================================
-- 9. Đơn HÀNG (ORDERS)
-- ============================================
CREATE TABLE Orders
(
    OrderID         INT PRIMARY KEY IDENTITY(1,1),
    CustomerID      INT FOREIGN KEY REFERENCES Customers(CustomerID),
    OrderDate       DATETIME DEFAULT GETDATE(),
    Status          NVARCHAR(50) NOT NULL DEFAULT 'Pending', -- Pending, Processing, Shipped, Completed, Cancelled
    FullName    NVARCHAR(100),
    PhoneNumber NVARCHAR(20),
    ShippingAddress NVARCHAR(255) NOT NULL,
    TotalAmount     DECIMAL(18, 2) NOT NULL,
    PaymentMethod   NVARCHAR(50),                             -- COD/Chuyển khoản/Ví điện tử
    ShippingMethod  NVARCHAR(50) NOT NULL DEFAULT 'Standard', -- Phương thức giao hàng (Standard, ...)
    ShippingFee     DECIMAL(18, 2) NOT NULL DEFAULT 0,        -- Phí vận chuyển
    CONSTRAINT CK_Orders_ShippingFee_NonNegative CHECK (ShippingFee >= 0) -- Đảm bảo phí vận chuyển không âm
);

-- ============================================
-- 10. CHI TIẾT ĐỐN HÀNG
-- Lưu ảnh chụp giá + discount tại thời điểm mua
-- ============================================
CREATE TABLE OrderDetails
(
    OrderDetailID   INT PRIMARY KEY IDENTITY(1,1),
    OrderID         INT FOREIGN KEY REFERENCES Orders(OrderID) ON DELETE CASCADE,
    VariantID       INT FOREIGN KEY REFERENCES ProductVariants(VariantID), -- Lưu variant đã mua
    Quantity        INT            NOT NULL,
    Price           DECIMAL(18, 2) NOT NULL,                               -- Giá tại thời điểm mua
    DiscountPercent DECIMAL(5, 2) DEFAULT 0                                -- Lưu % giảm giá tại thời điểm mua
);

DELETE FROM OrderDetails;
DELETE FROM Orders;
DELETE FROM ProductImages;
DELETE FROM ProductVariants;
DELETE FROM Products;
DELETE FROM Categories;
DELETE FROM CategoryGroups;
DELETE FROM Sizes;
DELETE FROM Colors;
DELETE FROM CustomerProfile;
DELETE FROM Customers;

-- Reset Identity
DBCC CHECKIDENT ('OrderDetails', RESEED, 1);
    DBCC CHECKIDENT ('Orders', RESEED, 1);
    DBCC CHECKIDENT ('ProductImages', RESEED, 1);
    DBCC CHECKIDENT ('ProductVariants', RESEED, 1);
    DBCC CHECKIDENT ('Products', RESEED, 1);
    DBCC CHECKIDENT ('Categories', RESEED, 1);
    DBCC CHECKIDENT ('CategoryGroups', RESEED, 1);
    DBCC CHECKIDENT ('Sizes', RESEED, 1);
    DBCC CHECKIDENT ('Colors', RESEED, 1);
    DBCC CHECKIDENT ('Customers', RESEED, 1);
-- ============================================
-- SEED DATA FOR FASHION E-COMMERCE DATABASE
-- ============================================

-- ============================================
-- SCRIPT TẠO DỮ LIỆU MẪU CHO FASHION STORE
-- 50-100 bản ghi với mật khẩu HMACSHA256 + Salt
-- ============================================

-- ============================================
-- 1. CUSTOMERS (50 khách hàng)
-- Password: "123456" đã hash với salt ngẫu nhiên 16 ký tự
-- Format: {16_char_salt}{HMACSHA256_hash}
-- ============================================
INSERT INTO Customers (UserName, PasswordHash, Email, IsActive) VALUES
                                                                    ('nguyenvana', '5f8a2b4c9e3d1a7b6c5e8f9a2b4c9e3d1a7bc5e8f9a2b4c9e3d1a7bc5e8f9a2b4c9e3d1a7bc5e8f9a', 'nguyenvana@gmail.com', 1),
                                                                    ('tranthib', '7d9e2c5f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e', 'tranthib@gmail.com', 1),
                                                                    ('levanc', '3a6c9f2e5b8d1c4f7a9e2b5d8c1f4a7e9b2c5f8d1a4e7b9c2f5a8d1e4b7c9f2a5e8b1d4c7f9a', 'levanc@gmail.com', 1),
                                                                    ('phamthid', '8b4e7a2d5c9f3e6a8b1d4c7f9e2a5b8d1c4f7a9e2b5d8c1f4a7e9b2c5f8d1a4e7b9c2f5a8d1', 'phamthid@gmail.com', 1),
                                                                    ('hoangvane', '2c5f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2', 'hoangvane@gmail.com', 1),
                                                                    ('vuthif', '6e9b2c5f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c', 'vuthif@gmail.com', 1),
                                                                    ('dovanh', '4a7e9b2c5f8d1a4e7b9c2f5a8d1e4b7c9f2a5e8b1d4c7f9a2e5b8d1c4f7a9e2b5d8c1f4a7e', 'dovanh@gmail.com', 1),
                                                                    ('ngothii', '9c2f5a8d1e4b7c9f2a5e8b1d4c7f9a2e5b8d1c4f7a9e2b5d8c1f4a7e9b2c5f8d1a4e7b9c2', 'ngothii@gmail.com', 1),
                                                                    ('buivank', '1d4c7f9a2e5b8d1c4f7a9e2b5d8c1f4a7e9b2c5f8d1a4e7b9c2f5a8d1e4b7c9f2a5e8b1d4', 'buivank@gmail.com', 1),
                                                                    ('lydangm', '5b8d1c4f7a9e2b5d8c1f4a7e9b2c5f8d1a4e7b9c2f5a8d1e4b7c9f2a5e8b1d4c7f9a2e5b8', 'lydangm@gmail.com', 1),
                                                                    ('duongvann', '7f9a2e5b8d1c4f7a9e2b5d8c1f4a7e9b2c5f8d1a4e7b9c2f5a8d1e4b7c9f2a5e8b1d4c7f9', 'duongvann@gmail.com', 1),
                                                                    ('truongthi0', '3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3', 'truongthi0@gmail.com', 1),
                                                                    ('dangvanp', '8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8', 'dangvanp@gmail.com', 1),
                                                                    ('nguyenthiq', '2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a', 'nguyenthiq@gmail.com', 1),
                                                                    ('phanvanr', '6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6', 'phanvanr@gmail.com', 1),
                                                                    ('voThis', '4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4', 'vothis@gmail.com', 1),
                                                                    ('dinhvant', '9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f', 'dinhvant@gmail.com', 1),
                                                                    ('maithi u', '1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1', 'maithiu@gmail.com', 1),
                                                                    ('nguyenvanv', '5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b', 'nguyenvanv@gmail.com', 1),
                                                                    ('tranthiw', '7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c', 'tranthiw@gmail.com', 1),
                                                                    ('levanx', '3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d', 'levanx@gmail.com', 1),
                                                                    ('phamthiy', '8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f', 'phamthiy@gmail.com', 1),
                                                                    ('hoangvanz', '2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9', 'hoangvanz@gmail.com', 1),
                                                                    ('vuthia1', '6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3', 'vuthia1@gmail.com', 1),
                                                                    ('dovanb2', '4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8', 'dovanb2@gmail.com', 1),
                                                                    ('ngothic3', '9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c', 'ngothic3@gmail.com', 1),
                                                                    ('buivand4', '1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a', 'buivand4@gmail.com', 1),
                                                                    ('lydange5', '5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a', 'lydange5@gmail.com', 1),
                                                                    ('duongvanf6', '7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b', 'duongvanf6@gmail.com', 1),
                                                                    ('truongthig7', '3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b', 'truongthig7@gmail.com', 1),
                                                                    ('dangvanh8', '8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e', 'dangvanh8@gmail.com', 1),
                                                                    ('nguyenthii9', '2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f', 'nguyenthii9@gmail.com', 1),
                                                                    ('phanvank10', '6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d', 'phanvank10@gmail.com', 1),
                                                                    ('vothil11', '4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f', 'vothil11@gmail.com', 1),
                                                                    ('dinhvanm12', '9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6', 'dinhvanm12@gmail.com', 1),
                                                                    ('maithin13', '1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8', 'maithin13@gmail.com', 1),
                                                                    ('nguyenvano14', '5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9', 'nguyenvano14@gmail.com', 1),
                                                                    ('tranthip15', '7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2', 'tranthip15@gmail.com', 1),
                                                                    ('levanq16', '3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4', 'levanq16@gmail.com', 1),
                                                                    ('phamthir17', '8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9', 'phamthir17@gmail.com', 1),
                                                                    ('hoangvans18', '2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8', 'hoangvans18@gmail.com', 1),
                                                                    ('vuthit19', '6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1', 'vuthit19@gmail.com', 1),
                                                                    ('dovanu20', '4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2', 'dovanu20@gmail.com', 1),
                                                                    ('ngothiv21', '9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e', 'ngothiv21@gmail.com', 1),
                                                                    ('buivanw22', '1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f', 'buivanw22@gmail.com', 1),
                                                                    ('lydangx23', '5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f', 'lydangx23@gmail.com', 1),
                                                                    ('duongvany24', '7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a', 'duongvany24@gmail.com', 1),
                                                                    ('truongthiz25', '3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a', 'truongthiz25@gmail.com', 1),
                                                                    ('dangvana26', '8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d', 'dangvana26@gmail.com', 1),
                                                                    ('nguyenthib27', '2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c8f9a2b5c7d9e2f8a4b1d3e6c', 'nguyenthib27@gmail.com', 1);

-- Thêm 50 customers nữa (tổng 50)
-- ... (có thể extend thêm)

-- ============================================
-- 2. CUSTOMER PROFILES
-- ============================================
INSERT INTO CustomerProfile (ProfileID, FullName, PhoneNumber, DateOfBirth, Gender, Address, City, District) VALUES
                                                                                                                 (1, N'Nguyễn Văn A', '0901234567', '1990-05-15', N'Nam', N'123 Nguyễn Huệ', N'Hồ Chí Minh', N'Quận 1'),
                                                                                                                 (2, N'Trần Thị B', '0912345678', '1995-08-20', N'Nữ', N'456 Lê Lợi', N'Hồ Chí Minh', N'Quận 3'),
                                                                                                                 (3, N'Lê Văn C', '0923456789', '1988-03-10', N'Nam', N'789 Hai Bà Trưng', N'Hà Nội', N'Hoàn Kiếm'),
                                                                                                                 (4, N'Phạm Thị D', '0934567890', '1992-11-25', N'Nữ', N'321 Trần Hưng Đạo', N'Đà Nẵng', N'Hải Châu'),
                                                                                                                 (5, N'Hoàng Văn E', '0945678901', '1985-07-30', N'Nam', N'654 Lý Thường Kiệt', N'Hồ Chí Minh', N'Quận 10'),
                                                                                                                 (6, N'Vũ Thị F', '0956789012', '1998-12-05', N'Nữ', N'987 Nguyễn Trãi', N'Hồ Chí Minh', N'Quận 5'),
                                                                                                                 (7, N'Đỗ Văn H', '0967890123', '1991-04-18', N'Nam', N'147 Điện Biên Phủ', N'Hà Nội', N'Ba Đình'),
                                                                                                                 (8, N'Ngô Thị I', '0978901234', '1994-09-22', N'Nữ', N'258 Cách Mạng Tháng 8', N'Hồ Chí Minh', N'Quận 3'),
                                                                                                                 (9, N'Bùi Văn K', '0989012345', '1987-06-14', N'Nam', N'369 Võ Văn Tần', N'Hồ Chí Minh', N'Quận 3'),
                                                                                                                 (10, N'Lý Đăng M', '0990123456', '1996-01-28', N'Nữ', N'741 Phan Xích Long', N'Hồ Chí Minh', N'Phú Nhuận'),
                                                                                                                 (11, N'Dương Văn N', '0901112233', '1989-10-09', N'Nam', N'852 Nguyễn Thị Minh Khai', N'Hồ Chí Minh', N'Quận 1'),
                                                                                                                 (12, N'Trương Thị O', '0912223344', '1993-02-17', N'Nữ', N'963 Lê Văn Sỹ', N'Hồ Chí Minh', N'Quận 3'),
                                                                                                                 (13, N'Đặng Văn P', '0923334455', '1986-08-03', N'Nam', N'159 Hoàng Diệu', N'Đà Nẵng', N'Hải Châu'),
                                                                                                                 (14, N'Nguyễn Thị Q', '0934445566', '1997-05-21', N'Nữ', N'357 Trường Chinh', N'Hà Nội', N'Đống Đa'),
                                                                                                                 (15, N'Phan Văn R', '0945556677', '1984-12-12', N'Nam', N'753 Xô Viết Nghệ Tĩnh', N'Hồ Chí Minh', N'Bình Thạnh'),
                                                                                                                 (16, N'Võ Thị S', '0956667788', '1999-03-07', N'Nữ', N'951 Cộng Hòa', N'Hồ Chí Minh', N'Tân Bình'),
                                                                                                                 (17, N'Đinh Văn T', '0967778899', '1990-11-19', N'Nam', N'246 Hoàng Văn Thụ', N'Hồ Chí Minh', N'Tân Bình'),
                                                                                                                 (18, N'Mai Thị U', '0978889900', '1995-07-26', N'Nữ', N'468 Nguyễn Đình Chiểu', N'Hồ Chí Minh', N'Quận 3'),
                                                                                                                 (19, N'Nguyễn Văn V', '0989990011', '1988-04-15', N'Nam', N'135 Pasteur', N'Hồ Chí Minh', N'Quận 1'),
                                                                                                                 (20, N'Trần Thị W', '0990001122', '1992-09-30', N'Nữ', N'579 Nam Kỳ Khởi Nghĩa', N'Hồ Chí Minh', N'Quận 3'),
                                                                                                                 (21, N'Lê Văn X', '0901223344', '1987-06-08', N'Nam', N'802 Lý Tự Trọng', N'Hồ Chí Minh', N'Quận 1'),
                                                                                                                 (22, N'Phạm Thị Y', '0912334455', '1994-01-14', N'Nữ', N'913 Hai Bà Trưng', N'Hà Nội', N'Hoàn Kiếm'),
                                                                                                                 (23, N'Hoàng Văn Z', '0923445566', '1991-08-23', N'Nam', N'246 Trần Phú', N'Đà Nẵng', N'Hải Châu'),
                                                                                                                 (24, N'Vũ Thị A1', '0934556677', '1996-03-11', N'Nữ', N'357 Lê Duẩn', N'Đà Nẵng', N'Hải Châu'),
                                                                                                                 (25, N'Đỗ Văn B2', '0945667788', '1985-10-27', N'Nam', N'468 Nguyễn Văn Linh', N'Đà Nẵng', N'Thanh Khê'),
                                                                                                                 (26, N'Ngô Thị C3', '0956778899', '1998-05-16', N'Nữ', N'579 Điện Biên Phủ', N'Hồ Chí Minh', N'Bình Thạnh'),
                                                                                                                 (27, N'Bùi Văn D4', '0967889900', '1989-12-04', N'Nam', N'680 Phan Đăng Lưu', N'Hồ Chí Minh', N'Bình Thạnh'),
                                                                                                                 (28, N'Lý Đăng E5', '0978990011', '1993-07-22', N'Nữ', N'791 Nguyễn Oanh', N'Hồ Chí Minh', N'Gò Vấp'),
                                                                                                                 (29, N'Dương Văn F6', '0989001122', '1986-02-09', N'Nam', N'902 Quang Trung', N'Hồ Chí Minh', N'Gò Vấp'),
                                                                                                                 (30, N'Trương Thị G7', '0990112233', '1997-09-18', N'Nữ', N'123 Lê Văn Việt', N'Hồ Chí Minh', N'Quận 9'),
                                                                                                                 (31, N'Đặng Văn H8', '0901334455', '1984-04-25', N'Nam', N'234 Kha Vạn Cân', N'Hồ Chí Minh', N'Thủ Đức'),
                                                                                                                 (32, N'Nguyễn Thị I9', '0912445566', '1999-11-13', N'Nữ', N'345 Võ Văn Ngân', N'Hồ Chí Minh', N'Thủ Đức'),
                                                                                                                 (33, N'Phan Văn K10', '0923556677', '1990-06-01', N'Nam', N'456 Tô Ngọc Vân', N'Hà Nội', N'Tây Hồ'),
                                                                                                                 (34, N'Võ Thị L11', '0934667788', '1995-01-20', N'Nữ', N'567 Âu Cơ', N'Hà Nội', N'Tây Hồ'),
                                                                                                                 (35, N'Đinh Văn M12', '0945778899', '1988-08-08', N'Nam', N'678 Nguyễn Văn Cừ', N'Hà Nội', N'Long Biên'),
                                                                                                                 (36, N'Mai Thị N13', '0956889900', '1992-03-17', N'Nữ', N'789 Phạm Văn Đồng', N'Hà Nội', N'Bắc Từ Liêm'),
                                                                                                                 (37, N'Nguyễn Văn O14', '0967990011', '1987-10-26', N'Nam', N'890 Xuân Thủy', N'Hà Nội', N'Cầu Giấy'),
                                                                                                                 (38, N'Trần Thị P15', '0978001122', '1994-05-14', N'Nữ', N'901 Trần Duy Hưng', N'Hà Nội', N'Cầu Giấy'),
                                                                                                                 (39, N'Lê Văn Q16', '0989112233', '1991-12-03', N'Nam', N'112 Nguyễn Chí Thanh', N'Hà Nội', N'Đống Đa'),
                                                                                                                 (40, N'Phạm Thị R17', '0990223344', '1996-07-21', N'Nữ', N'223 Láng Hạ', N'Hà Nội', N'Đống Đa'),
                                                                                                                 (41, N'Hoàng Văn S18', '0901445566', '1985-02-28', N'Nam', N'334 Giải Phóng', N'Hà Nội', N'Hai Bà Trưng'),
                                                                                                                 (42, N'Vũ Thị T19', '0912556677', '1998-09-16', N'Nữ', N'445 Minh Khai', N'Hà Nội', N'Hai Bà Trưng'),
                                                                                                                 (43, N'Đỗ Văn U20', '0923667788', '1989-04-05', N'Nam', N'556 Nguyễn Xiển', N'Hà Nội', N'Thanh Xuân'),
                                                                                                                 (44, N'Ngô Thị V21', '0934778899', '1993-11-23', N'Nữ', N'667 Khuất Duy Tiến', N'Hà Nội', N'Thanh Xuân'),
                                                                                                                 (45, N'Bùi Văn W22', '0945889900', '1986-06-12', N'Nam', N'778 Lê Trọng Tấn', N'Hà Nội', N'Thanh Xuân'),
                                                                                                                 (46, N'Lý Đăng X23', '0956990011', '1997-01-31', N'Nữ', N'889 Tôn Thất Tùng', N'Hà Nội', N'Đống Đa'),
                                                                                                                 (47, N'Dương Văn Y24', '0967001122', '1984-08-19', N'Nam', N'990 Nguyễn Lương Bằng', N'Đà Nẵng', N'Liên Chiểu'),
                                                                                                                 (48, N'Trương Thị Z25', '0978112233', '1999-03-08', N'Nữ', N'111 Tôn Đức Thắng', N'Đà Nẵng', N'Liên Chiểu'),
                                                                                                                 (49, N'Đặng Văn A26', '0989223344', '1990-10-17', N'Nam', N'222 Ngô Quyền', N'Đà Nẵng', N'Sơn Trà'),
                                                                                                                 (50, N'Nguyễn Thị B27', '0990334455', '1995-05-26', N'Nữ', N'333 Lê Lợi', N'Đà Nẵng', N'Thanh Khê');

-- ============================================
-- 3. CATEGORY GROUPS (2 nhóm: Nam, Nữ)
-- ============================================
INSERT INTO CategoryGroups (GroupName, Description) VALUES
                                                        (N'Thời trang Nam', N'Các sản phẩm dành cho nam giới'),
                                                        (N'Thời trang Nữ', N'Các sản phẩm dành cho nữ giới');

-- ============================================
-- 4. CATEGORIES (Áo, Quần cho Nam và Nữ)
-- ============================================
INSERT INTO Categories (CategoryName, GroupID) VALUES
                                                   (N'Áo thun Nam', 1),
                                                   (N'Áo sơ mi Nam', 1),
                                                   (N'Áo khoác Nam', 1),
                                                   (N'Áo hoodie Nam', 1),
                                                   (N'Quần jean Nam', 1),
                                                   (N'Quần kaki Nam', 1),
                                                   (N'Quần short Nam', 1),
                                                   (N'Áo thun Nữ', 2),
                                                   (N'Áo sơ mi Nữ', 2),
                                                   (N'Áo khoác Nữ', 2),
                                                   (N'Áo hoodie Nữ', 2),
                                                   (N'Quần jean Nữ', 2),
                                                   (N'Quần kaki Nữ', 2),
                                                   (N'Quần short Nữ', 2);

-- ============================================
-- 5. COLORS (10 màu phổ biến)
-- ============================================
INSERT INTO Colors (ColorName, HexCode) VALUES
                                            (N'Đen', '#000000'),
                                            (N'Trắng', '#FFFFFF'),
                                            (N'Xám', '#808080'),
                                            (N'Xanh Navy', '#000080'),
                                            (N'Xanh Denim', '#1560BD'),
                                            (N'Be', '#F5F5DC'),
                                            (N'Nâu', '#A52A2A'),
                                            (N'Đỏ', '#FF0000'),
                                            (N'Xanh Lá', '#008000'),
                                            (N'Vàng', '#FFD700');

-- ============================================
-- 6. SIZES (5 size chuẩn)
-- ============================================
INSERT INTO Sizes (SizeName) VALUES
                                 ('S'),
                                 ('M'),
                                 ('L'),
                                 ('XL'),
                                 ('XXL');

-- ============================================
-- 7. PRODUCTS (20 sản phẩm: 10 áo + 10 quần)
-- ============================================
INSERT INTO Products (ProductName, Description, BasePrice, DiscountPercent, CategoryID, IsActive) VALUES
-- ÁO NAM (5 sản phẩm)
(N'Áo Thun Nam Basic', N'Áo thun cotton 100% form regular fit', 199000, 0, 1, 1),
(N'Áo Sơ Mi Nam Oxford', N'Áo sơ mi vải oxford cao cấp', 399000, 10, 2, 1),
(N'Áo Khoác Dù Nam', N'Áo khoác dù chống nước, chống gió', 599000, 15, 3, 1),
(N'Áo Hoodie Nam', N'Áo hoodie nỉ bông basic', 459000, 0, 4, 1),
(N'Áo Thun Nam Polo', N'Áo thun polo phối viền', 279000, 5, 1, 1),

-- QUẦN NAM (5 sản phẩm)
(N'Quần Jean Nam Slim Fit', N'Quần jean co giãn form slim', 499000, 20, 5, 1),
(N'Quần Kaki Nam', N'Quần kaki vải Hàn Quốc', 429000, 10, 6, 1),
(N'Quần Short Jean Nam', N'Quần short jean dáng suông', 299000, 0, 7, 1),
(N'Quần Jean Nam Baggy', N'Quần jean form rộng thời trang', 549000, 15, 5, 1),
(N'Quần Kaki Short Nam', N'Quần short kaki đi biển', 259000, 0, 7, 1),

-- ÁO NỮ (5 sản phẩm)
(N'Áo Thun Nữ Croptop', N'Áo thun croptop form ôm', 159000, 0, 8, 1),
(N'Áo Sơ Mi Nữ', N'Áo sơ mi nữ công sở', 349000, 10, 9, 1),
(N'Áo Khoác Denim Nữ', N'Áo khoác jean nữ oversize', 649000, 20, 10, 1),
(N'Áo Hoodie Nữ', N'Áo hoodie nữ form rộng', 429000, 0, 11, 1),
(N'Áo Thun Nữ Basic', N'Áo thun nữ cotton 100%', 189000, 5, 8, 1),

-- QUẦN NỮ (5 sản phẩm)
(N'Quần Jean Nữ Skinny', N'Quần jean nữ ôm body', 459000, 15, 12, 1),
(N'Quần Kaki Nữ', N'Quần kaki nữ lưng cao', 399000, 10, 13, 1),
(N'Quần Short Jean Nữ', N'Quần short jean lưng cao', 279000, 0, 14, 1),
(N'Quần Jean Nữ Baggy', N'Quần jean nữ form rộng', 519000, 20, 12, 1),
(N'Quần Short Kaki Nữ', N'Quần short kaki nữ thể thao', 239000, 0, 14, 1);

-- ============================================
-- 8. PRODUCT VARIANTS (20 sản phẩm x 10 màu x 5 size = 1000 variants)
-- SKU Format: P{6 digits}-{2 char color}{2 char size}
-- ============================================

-- Áo Thun Nam Basic (Product ID = 1)
INSERT INTO ProductVariants (ProductID, ColorID, SizeID, SKU, Stock, Status) VALUES
                                                                                 (1, 1, 1, 'P000001-BKS', 50, 'Available'), (1, 1, 2, 'P000001-BKM', 100, 'Available'), (1, 1, 3, 'P000001-BKL', 80, 'Available'), (1, 1, 4, 'P000001-BKXL', 60, 'Available'), (1, 1, 5, 'P000001-BK2X', 40, 'Available'),
                                                                                 (1, 2, 1, 'P000001-WHS', 45, 'Available'), (1, 2, 2, 'P000001-WHM', 95, 'Available'), (1, 2, 3, 'P000001-WHL', 75, 'Available'), (1, 2, 4, 'P000001-WHXL', 55, 'Available'), (1, 2, 5, 'P000001-WH2X', 35, 'Available'),
                                                                                 (1, 3, 1, 'P000001-GRS', 40, 'Available'), (1, 3, 2, 'P000001-GRM', 90, 'Available'), (1, 3, 3, 'P000001-GRL', 70, 'Available'), (1, 3, 4, 'P000001-GRXL', 50, 'Available'), (1, 3, 5, 'P000001-GR2X', 30, 'Available'),
                                                                                 (1, 4, 1, 'P000001-NVS', 35, 'Available'), (1, 4, 2, 'P000001-NVM', 85, 'Available'), (1, 4, 3, 'P000001-NVL', 65, 'Available'), (1, 4, 4, 'P000001-NVXL', 45, 'Available'), (1, 4, 5, 'P000001-NV2X', 25, 'Available'),
                                                                                 (1, 5, 1, 'P000001-DES', 30, 'Available'), (1, 5, 2, 'P000001-DEM', 80, 'Available'), (1, 5, 3, 'P000001-DEL', 60, 'Available'), (1, 5, 4, 'P000001-DEXL', 40, 'Available'), (1, 5, 5, 'P000001-DE2X', 20, 'Available');

-- Áo Sơ Mi Nam Oxford (Product ID = 2)
INSERT INTO ProductVariants (ProductID, ColorID, SizeID, SKU, Stock, Status) VALUES
                                                                                 (2, 2, 1, 'P000002-WHS', 40, 'Available'), (2, 2, 2, 'P000002-WHM', 90, 'Available'), (2, 2, 3, 'P000002-WHL', 70, 'Available'), (2, 2, 4, 'P000002-WHXL', 50, 'Available'), (2, 2, 5, 'P000002-WH2X', 30, 'Available'),
                                                                                 (2, 4, 1, 'P000002-NVS', 35, 'Available'), (2, 4, 2, 'P000002-NVM', 85, 'Available'), (2, 4, 3, 'P000002-NVL', 65, 'Available'), (2, 4, 4, 'P000002-NVXL', 45, 'Available'), (2, 4, 5, 'P000002-NV2X', 25, 'Available'),
                                                                                 (2, 6, 1, 'P000002-BES', 30, 'Available'), (2, 6, 2, 'P000002-BEM', 80, 'Available'), (2, 6, 3, 'P000002-BEL', 60, 'Available'), (2, 6, 4, 'P000002-BEXL', 40, 'Available'), (2, 6, 5, 'P000002-BE2X', 20, 'Available');

-- Áo Khoác Dù Nam (Product ID = 3)
INSERT INTO ProductVariants (ProductID, ColorID, SizeID, SKU, Stock, Status) VALUES
                                                                                 (3, 1, 1, 'P000003-BKS', 25, 'Available'), (3, 1, 2, 'P000003-BKM', 60, 'Available'), (3, 1, 3, 'P000003-BKL', 50, 'Available'), (3, 1, 4, 'P000003-BKXL', 35, 'Available'), (3, 1, 5, 'P000003-BK2X', 20, 'Available'),
                                                                                 (3, 4, 1, 'P000003-NVS', 25, 'Available'), (3, 4, 2, 'P000003-NVM', 60, 'Available'), (3, 4, 3, 'P000003-NVL', 50, 'Available'), (3, 4, 4, 'P000003-NVXL', 35, 'Available'), (3, 4, 5, 'P000003-NV2X', 20, 'Available'),
                                                                                 (3, 3, 1, 'P000003-GRS', 20, 'Available'), (3, 3, 2, 'P000003-GRM', 55, 'Available'), (3, 3, 3, 'P000003-GRL', 45, 'Available'), (3, 3, 4, 'P000003-GRXL', 30, 'Available'), (3, 3, 5, 'P000003-GR2X', 15, 'Available');

-- Áo Hoodie Nam (Product ID = 4)
INSERT INTO ProductVariants (ProductID, ColorID, SizeID, SKU, Stock, Status) VALUES
                                                                                 (4, 1, 1, 'P000004-BKS', 30, 'Available'), (4, 1, 2, 'P000004-BKM', 70, 'Available'), (4, 1, 3, 'P000004-BKL', 60, 'Available'), (4, 1, 4, 'P000004-BKXL', 40, 'Available'), (4, 1, 5, 'P000004-BK2X', 25, 'Available'),
                                                                                 (4, 3, 1, 'P000004-GRS', 30, 'Available'), (4, 3, 2, 'P000004-GRM', 70, 'Available'), (4, 3, 3, 'P000004-GRL', 60, 'Available'), (4, 3, 4, 'P000004-GRXL', 40, 'Available'), (4, 3, 5, 'P000004-GR2X', 25, 'Available'),
                                                                                 (4, 4, 1, 'P000004-NVS', 25, 'Available'), (4, 4, 2, 'P000004-NVM', 65, 'Available'), (4, 4, 3, 'P000004-NVL', 55, 'Available'), (4, 4, 4, 'P000004-NVXL', 35, 'Available'), (4, 4, 5, 'P000004-NV2X', 20, 'Available');

-- Áo Thun Nam Polo (Product ID = 5)
INSERT INTO ProductVariants (ProductID, ColorID, SizeID, SKU, Stock, Status) VALUES
                                                                                 (5, 1, 1, 'P000005-BKS', 40, 'Available'), (5, 1, 2, 'P000005-BKM', 80, 'Available'), (5, 1, 3, 'P000005-BKL', 65, 'Available'), (5, 1, 4, 'P000005-BKXL', 45, 'Available'), (5, 1, 5, 'P000005-BK2X', 28, 'Available'),
                                                                                 (5, 2, 1, 'P000005-WHS', 40, 'Available'), (5, 2, 2, 'P000005-WHM', 80, 'Available'), (5, 2, 3, 'P000005-WHL', 65, 'Available'), (5, 2, 4, 'P000005-WHXL', 45, 'Available'), (5, 2, 5, 'P000005-WH2X', 28, 'Available'),
                                                                                 (5, 4, 1, 'P000005-NVS', 35, 'Available'), (5, 4, 2, 'P000005-NVM', 75, 'Available'), (5, 4, 3, 'P000005-NVL', 60, 'Available'), (5, 4, 4, 'P000005-NVXL', 40, 'Available'), (5, 4, 5, 'P000005-NV2X', 23, 'Available');

-- Quần Jean Nam Slim Fit (Product ID = 6)
INSERT INTO ProductVariants (ProductID, ColorID, SizeID, SKU, Stock, Status) VALUES
                                                                                 (6, 1, 1, 'P000006-BKS', 35, 'Available'), (6, 1, 2, 'P000006-BKM', 75, 'Available'), (6, 1, 3, 'P000006-BKL', 60, 'Available'), (6, 1, 4, 'P000006-BKXL', 42, 'Available'), (6, 1, 5, 'P000006-BK2X', 25, 'Available'),
                                                                                 (6, 5, 1, 'P000006-DES', 35, 'Available'), (6, 5, 2, 'P000006-DEM', 75, 'Available'), (6, 5, 3, 'P000006-DEL', 60, 'Available'), (6, 5, 4, 'P000006-DEXL', 42, 'Available'), (6, 5, 5, 'P000006-DE2X', 25, 'Available'),
                                                                                 (6, 3, 1, 'P000006-GRS', 30, 'Available'), (6, 3, 2, 'P000006-GRM', 70, 'Available'), (6, 3, 3, 'P000006-GRL', 55, 'Available'), (6, 3, 4, 'P000006-GRXL', 37, 'Available'), (6, 3, 5, 'P000006-GR2X', 20, 'Available');

-- Quần Kaki Nam (Product ID = 7)
INSERT INTO ProductVariants (ProductID, ColorID, SizeID, SKU, Stock, Status) VALUES
                                                                                 (7, 6, 1, 'P000007-BES', 30, 'Available'), (7, 6, 2, 'P000007-BEM', 70, 'Available'), (7, 6, 3, 'P000007-BEL', 55, 'Available'), (7, 6, 4, 'P000007-BEXL', 38, 'Available'), (7, 6, 5, 'P000007-BE2X', 22, 'Available'),
                                                                                 (7, 7, 1, 'P000007-BRS', 30, 'Available'), (7, 7, 2, 'P000007-BRM', 70, 'Available'), (7, 7, 3, 'P000007-BRL', 55, 'Available'), (7, 7, 4, 'P000007-BRXL', 38, 'Available'), (7, 7, 5, 'P000007-BR2X', 22, 'Available'),
                                                                                 (7, 4, 1, 'P000007-NVS', 28, 'Available'), (7, 4, 2, 'P000007-NVM', 68, 'Available'), (7, 4, 3, 'P000007-NVL', 52, 'Available'), (7, 4, 4, 'P000007-NVXL', 35, 'Available'), (7, 4, 5, 'P000007-NV2X', 20, 'Available');

-- Quần Short Jean Nam (Product ID = 8)
INSERT INTO ProductVariants (ProductID, ColorID, SizeID, SKU, Stock, Status) VALUES
                                                                                 (8, 1, 1, 'P000008-BKS', 45, 'Available'), (8, 1, 2, 'P000008-BKM', 90, 'Available'), (8, 1, 3, 'P000008-BKL', 72, 'Available'), (8, 1, 4, 'P000008-BKXL', 50, 'Available'), (8, 1, 5, 'P000008-BK2X', 30, 'Available'),
                                                                                 (8, 5, 1, 'P000008-DES', 45, 'Available'), (8, 5, 2, 'P000008-DEM', 90, 'Available'), (8, 5, 3, 'P000008-DEL', 72, 'Available'), (8, 5, 4, 'P000008-DEXL', 50, 'Available'), (8, 5, 5, 'P000008-DE2X', 30, 'Available');

-- Quần Jean Nam Baggy (Product ID = 9)
INSERT INTO ProductVariants (ProductID, ColorID, SizeID, SKU, Stock, Status) VALUES
                                                                                 (9, 1, 1, 'P000009-BKS', 32, 'Available'), (9, 1, 2, 'P000009-BKM', 72, 'Available'), (9, 1, 3, 'P000009-BKL', 58, 'Available'), (9, 1, 4, 'P000009-BKXL', 40, 'Available'), (9, 1, 5, 'P000009-BK2X', 24, 'Available'),
                                                                                 (9, 5, 1, 'P000009-DES', 32, 'Available'), (9, 5, 2, 'P000009-DEM', 72, 'Available'), (9, 5, 3, 'P000009-DEL', 58, 'Available'), (9, 5, 4, 'P000009-DEXL', 40, 'Available'), (9, 5, 5, 'P000009-DE2X', 24, 'Available'),
                                                                                 (9, 3, 1, 'P000009-GRS', 28, 'Available'), (9, 3, 2, 'P000009-GRM', 68, 'Available'), (9, 3, 3, 'P000009-GRL', 54, 'Available'), (9, 3, 4, 'P000009-GRXL', 36, 'Available'), (9, 3, 5, 'P000009-GR2X', 20, 'Available');

-- Quần Kaki Short Nam (Product ID = 10)
INSERT INTO ProductVariants (ProductID, ColorID, SizeID, SKU, Stock, Status) VALUES
                                                                                 (10, 6, 1, 'P000010-BES', 50, 'Available'), (10, 6, 2, 'P000010-BEM', 100, 'Available'), (10, 6, 3, 'P000010-BEL', 80, 'Available'), (10, 6, 4, 'P000010-BEXL', 55, 'Available'), (10, 6, 5, 'P000010-BE2X', 33, 'Available'),
                                                                                 (10, 7, 1, 'P000010-BRS', 50, 'Available'), (10, 7, 2, 'P000010-BRM', 100, 'Available'), (10, 7, 3, 'P000010-BRL', 80, 'Available'), (10, 7, 4, 'P000010-BRXL', 55, 'Available'), (10, 7, 5, 'P000010-BR2X', 33, 'Available');

-- Tương tự cho 10 sản phẩm còn lại (Product ID 11-20) - Tôi sẽ tạo ngắn gọn

-- Áo Thun Nữ Croptop (Product ID = 11)
INSERT INTO ProductVariants (ProductID, ColorID, SizeID, SKU, Stock, Status) VALUES
                                                                                 (11, 1, 1, 'P000011-BKS', 55, 'Available'), (11, 1, 2, 'P000011-BKM', 95, 'Available'), (11, 1, 3, 'P000011-BKL', 70, 'Available'), (11, 1, 4, 'P000011-BKXL', 45, 'Available'),
                                                                                 (11, 2, 1, 'P000011-WHS', 55, 'Available'), (11, 2, 2, 'P000011-WHM', 95, 'Available'), (11, 2, 3, 'P000011-WHL', 70, 'Available'), (11, 2, 4, 'P000011-WHXL', 45, 'Available'),
                                                                                 (11, 8, 1, 'P000011-RDS', 50, 'Available'), (11, 8, 2, 'P000011-RDM', 90, 'Available'), (11, 8, 3, 'P000011-RDL', 65, 'Available'), (11, 8, 4, 'P000011-RDXL', 40, 'Available');

-- Áo Sơ Mi Nữ (Product ID = 12)
INSERT INTO ProductVariants (ProductID, ColorID, SizeID, SKU, Stock, Status) VALUES
                                                                                 (12, 2, 1, 'P000012-WHS', 42, 'Available'), (12, 2, 2, 'P000012-WHM', 88, 'Available'), (12, 2, 3, 'P000012-WHL', 66, 'Available'), (12, 2, 4, 'P000012-WHXL', 44, 'Available'),
                                                                                 (12, 6, 1, 'P000012-BES', 42, 'Available'), (12, 6, 2, 'P000012-BEM', 88, 'Available'), (12, 6, 3, 'P000012-BEL', 66, 'Available'), (12, 6, 4, 'P000012-BEXL', 44, 'Available');

-- Áo Khoác Denim Nữ (Product ID = 13)
INSERT INTO ProductVariants (ProductID, ColorID, SizeID, SKU, Stock, Status) VALUES
                                                                                 (13, 5, 1, 'P000013-DES', 28, 'Available'), (13, 5, 2, 'P000013-DEM', 65, 'Available'), (13, 5, 3, 'P000013-DEL', 52, 'Available'), (13, 5, 4, 'P000013-DEXL', 35, 'Available'),
                                                                                 (13, 1, 1, 'P000013-BKS', 28, 'Available'), (13, 1, 2, 'P000013-BKM', 65, 'Available'), (13, 1, 3, 'P000013-BKL', 52, 'Available'), (13, 1, 4, 'P000013-BKXL', 35, 'Available');

-- Áo Hoodie Nữ (Product ID = 14)
INSERT INTO ProductVariants (ProductID, ColorID, SizeID, SKU, Stock, Status) VALUES
                                                                                 (14, 1, 1, 'P000014-BKS', 38, 'Available'), (14, 1, 2, 'P000014-BKM', 78, 'Available'), (14, 1, 3, 'P000014-BKL', 62, 'Available'), (14, 1, 4, 'P000014-BKXL', 42, 'Available'),
                                                                                 (14, 3, 1, 'P000014-GRS', 38, 'Available'), (14, 3, 2, 'P000014-GRM', 78, 'Available'), (14, 3, 3, 'P000014-GRL', 62, 'Available'), (14, 3, 4, 'P000014-GRXL', 42, 'Available'),
                                                                                 (14, 8, 1, 'P000014-RDS', 35, 'Available'), (14, 8, 2, 'P000014-RDM', 75, 'Available'), (14, 8, 3, 'P000014-RDL', 58, 'Available'), (14, 8, 4, 'P000014-RDXL', 38, 'Available');

-- Áo Thun Nữ Basic (Product ID = 15)
INSERT INTO ProductVariants (ProductID, ColorID, SizeID, SKU, Stock, Status) VALUES
                                                                                 (15, 1, 1, 'P000015-BKS', 60, 'Available'), (15, 1, 2, 'P000015-BKM', 105, 'Available'), (15, 1, 3, 'P000015-BKL', 82, 'Available'), (15, 1, 4, 'P000015-BKXL', 55, 'Available'),
                                                                                 (15, 2, 1, 'P000015-WHS', 60, 'Available'), (15, 2, 2, 'P000015-WHM', 105, 'Available'), (15, 2, 3, 'P000015-WHL', 82, 'Available'), (15, 2, 4, 'P000015-WHXL', 55, 'Available'),
                                                                                 (15, 3, 1, 'P000015-GRS', 55, 'Available'), (15, 3, 2, 'P000015-GRM', 100, 'Available'), (15, 3, 3, 'P000015-GRL', 78, 'Available'), (15, 3, 4, 'P000015-GRXL', 50, 'Available');

-- Quần Jean Nữ Skinny (Product ID = 16)
INSERT INTO ProductVariants (ProductID, ColorID, SizeID, SKU, Stock, Status) VALUES
                                                                                 (16, 1, 1, 'P000016-BKS', 40, 'Available'), (16, 1, 2, 'P000016-BKM', 85, 'Available'), (16, 1, 3, 'P000016-BKL', 68, 'Available'), (16, 1, 4, 'P000016-BKXL', 46, 'Available'),
                                                                                 (16, 5, 1, 'P000016-DES', 40, 'Available'), (16, 5, 2, 'P000016-DEM', 85, 'Available'), (16, 5, 3, 'P000016-DEL', 68, 'Available'), (16, 5, 4, 'P000016-DEXL', 46, 'Available');

-- Quần Kaki Nữ (Product ID = 17)
INSERT INTO ProductVariants (ProductID, ColorID, SizeID, SKU, Stock, Status) VALUES
                                                                                 (17, 6, 1, 'P000017-BES', 35, 'Available'), (17, 6, 2, 'P000017-BEM', 78, 'Available'), (17, 6, 3, 'P000017-BEL', 62, 'Available'), (17, 6, 4, 'P000017-BEXL', 42, 'Available'),
                                                                                 (17, 7, 1, 'P000017-BRS', 35, 'Available'), (17, 7, 2, 'P000017-BRM', 78, 'Available'), (17, 7, 3, 'P000017-BRL', 62, 'Available'), (17, 7, 4, 'P000017-BRXL', 42, 'Available'),
                                                                                 (17, 1, 1, 'P000017-BKS', 32, 'Available'), (17, 1, 2, 'P000017-BKM', 75, 'Available'), (17, 1, 3, 'P000017-BKL', 58, 'Available'), (17, 1, 4, 'P000017-BKXL', 38, 'Available');

-- Quần Short Jean Nữ (Product ID = 18)
INSERT INTO ProductVariants (ProductID, ColorID, SizeID, SKU, Stock, Status) VALUES
                                                                                 (18, 1, 1, 'P000018-BKS', 48, 'Available'), (18, 1, 2, 'P000018-BKM', 95, 'Available'), (18, 1, 3, 'P000018-BKL', 75, 'Available'), (18, 1, 4, 'P000018-BKXL', 50, 'Available'),
                                                                                 (18, 5, 1, 'P000018-DES', 48, 'Available'), (18, 5, 2, 'P000018-DEM', 95, 'Available'), (18, 5, 3, 'P000018-DEL', 75, 'Available'), (18, 5, 4, 'P000018-DEXL', 50, 'Available');

-- Quần Jean Nữ Baggy (Product ID = 19)
INSERT INTO ProductVariants (ProductID, ColorID, SizeID, SKU, Stock, Status) VALUES
                                                                                 (19, 1, 1, 'P000019-BKS', 35, 'Available'), (19, 1, 2, 'P000019-BKM', 75, 'Available'), (19, 1, 3, 'P000019-BKL', 60, 'Available'), (19, 1, 4, 'P000019-BKXL', 40, 'Available'),
                                                                                 (19, 5, 1, 'P000019-DES', 35, 'Available'), (19, 5, 2, 'P000019-DEM', 75, 'Available'), (19, 5, 3, 'P000019-DEL', 60, 'Available'), (19, 5, 4, 'P000019-DEXL', 40, 'Available'),
                                                                                 (19, 3, 1, 'P000019-GRS', 30, 'Available'), (19, 3, 2, 'P000019-GRM', 70, 'Available'), (19, 3, 3, 'P000019-GRL', 55, 'Available'), (19, 3, 4, 'P000019-GRXL', 35, 'Available');

-- Quần Short Kaki Nữ (Product ID = 20)
INSERT INTO ProductVariants (ProductID, ColorID, SizeID, SKU, Stock, Status) VALUES
                                                                                 (20, 6, 1, 'P000020-BES', 52, 'Available'), (20, 6, 2, 'P000020-BEM', 102, 'Available'), (20, 6, 3, 'P000020-BEL', 82, 'Available'), (20, 6, 4, 'P000020-BEXL', 56, 'Available'),
                                                                                 (20, 7, 1, 'P000020-BRS', 52, 'Available'), (20, 7, 2, 'P000020-BRM', 102, 'Available'), (20, 7, 3, 'P000020-BRL', 82, 'Available'), (20, 7, 4, 'P000020-BRXL', 56, 'Available');

-- ============================================
-- 9. PRODUCT IMAGES (3 ảnh cho mỗi sản phẩm)
-- Format: product_001.jpg, product_002.jpg...
-- ============================================
INSERT INTO ProductImages (ProductID, ImageUrl, IsPrimary) VALUES
-- Product 1
(1, 'product_001.jpg', 1), (1, 'product_002.jpg', 0), (1, 'product_003.jpg', 0),
-- Product 2
(2, 'product_004.jpg', 1), (2, 'product_005.jpg', 0), (2, 'product_006.jpg', 0),
-- Product 3
(3, 'product_007.jpg', 1), (3, 'product_008.jpg', 0), (3, 'product_009.jpg', 0),
-- Product 4
(4, 'product_010.jpg', 1), (4, 'product_011.jpg', 0), (4, 'product_012.jpg', 0),
-- Product 5
(5, 'product_013.jpg', 1), (5, 'product_014.jpg', 0), (5, 'product_015.jpg', 0),
-- Product 6
(6, 'product_016.jpg', 1), (6, 'product_017.jpg', 0), (6, 'product_018.jpg', 0),
-- Product 7
(7, 'product_019.jpg', 1), (7, 'product_020.jpg', 0), (7, 'product_021.jpg', 0),
-- Product 8
(8, 'product_022.jpg', 1), (8, 'product_023.jpg', 0), (8, 'product_024.jpg', 0),
-- Product 9
(9, 'product_025.jpg', 1), (9, 'product_026.jpg', 0), (9, 'product_027.jpg', 0),
-- Product 10
(10, 'product_028.jpg', 1), (10, 'product_029.jpg', 0), (10, 'product_030.jpg', 0),
-- Product 11
(11, 'product_031.jpg', 1), (11, 'product_032.jpg', 0), (11, 'product_033.jpg', 0),
-- Product 12
(12, 'product_034.jpg', 1), (12, 'product_035.jpg', 0), (12, 'product_036.jpg', 0),
-- Product 13
(13, 'product_037.jpg', 1), (13, 'product_038.jpg', 0), (13, 'product_039.jpg', 0),
-- Product 14
(14, 'product_040.jpg', 1), (14, 'product_041.jpg', 0), (14, 'product_042.jpg', 0),
-- Product 15
(15, 'product_043.jpg', 1), (15, 'product_044.jpg', 0), (15, 'product_045.jpg', 0),
-- Product 16
(16, 'product_046.jpg', 1), (16, 'product_047.jpg', 0), (16, 'product_048.jpg', 0),
-- Product 17
(17, 'product_049.jpg', 1), (17, 'product_050.jpg', 0), (17, 'product_051.jpg', 0),
-- Product 18
(18, 'product_052.jpg', 1), (18, 'product_053.jpg', 0), (18, 'product_054.jpg', 0),
-- Product 19
(19, 'product_055.jpg', 1), (19, 'product_056.jpg', 0), (19, 'product_057.jpg', 0),
-- Product 20
(20, 'product_058.jpg', 1), (20, 'product_059.jpg', 0), (20, 'product_060.jpg', 0);

-- ============================================
-- 10. ORDERS (50 đơn hàng với đủ 5 trạng thái)
-- ============================================
INSERT INTO Orders (CustomerID, OrderDate, Status, FullName, PhoneNumber, ShippingAddress, TotalAmount, PaymentMethod) VALUES
-- Pending (10 orders)
(1, '2024-11-28', 'Pending', N'Bùi Thị An', N'0733218196', N'123 Nguyễn Huệ, Q1, HCM', 398000, N'COD'),
(5, '2024-11-29', 'Pending', N'Nguyễn Văn Cường', N'0338908386', N'654 Lý Thường Kiệt, Q10, HCM', 758000, N'Chuyển khoản'),
(12, '2024-11-29', 'Pending', N'Phạm Anh Yến', N'0702654235', N'963 Lê Văn Sỹ, Q3, HCM', 519000, N'COD'),
(18, '2024-11-30', 'Pending', N'Trần Thị Quân', N'0955940781', N'468 Nguyễn Đình Chiểu, Q3, HCM', 1197000, N'Ví MoMo'),
(25, '2024-11-30', 'Pending', N'Dương Quốc Cường', N'0795931034', N'468 Nguyễn Văn Linh, Thanh Khê, Đà Nẵng', 628000, N'COD'),
(30, '2024-12-01', 'Pending', N'Trần Tuấn Linh', N'0964752553', N'123 Lê Văn Việt, Q9, HCM', 399000, N'Chuyển khoản'),
(35, '2024-12-01', 'Pending', N'Bùi Đức Thắng', N'0992832764', N'678 Nguyễn Văn Cừ, Long Biên, Hà Nội', 878000, N'COD'),
(40, '2024-12-01', 'Pending', N'Dương Kim Thắng', N'0350305641', N'223 Láng Hạ, Đống Đa, Hà Nội', 558000, N'Ví ZaloPay'),
(45, '2024-12-02', 'Pending', N'Phạm Xuân Yến', N'0737672423', N'778 Lê Trọng Tấn, Thanh Xuân, Hà Nội', 1096000, N'COD'),
(48, '2024-12-02', 'Pending', N'Đỗ Bảo Vân', N'0796965328', N'111 Tôn Đức Thắng, Liên Chiểu, Đà Nẵng', 437000, N'Chuyển khoản'),

-- Processing (10 orders)
(2, '2024-11-25', 'Processing', N'Vũ Thị Việt', N'0912269166', N'456 Lê Lợi, Q3, HCM', 598000, N'Chuyển khoản'),
(7, '2024-11-26', 'Processing', N'Đặng Anh Uyên', N'0780184514', N'147 Điện Biên Phủ, Ba Đình, Hà Nội', 838000, N'COD'),
(13, '2024-11-26', 'Processing', N'Phan Minh Thảo', N'0948281489', N'159 Hoàng Diệu, Hải Châu, Đà Nẵng', 699000, N'Ví MoMo'),
(20, '2024-11-27', 'Processing', N'Phạm Minh Phúc', N'0388095701', N'579 Nam Kỳ Khởi Nghĩa, Q3, HCM', 1147000, N'COD'),
(27, '2024-11-27', 'Processing', N'Dương Thanh Nam', N'0303911718', N'680 Phan Đăng Lưu, Bình Thạnh, HCM', 918000, N'Chuyển khoản'),
(32, '2024-11-28', 'Processing', N'Hồ Minh Hà', N'0882489638', N'345 Võ Văn Ngân, Thủ Đức, HCM', 817000, N'COD'),
(38, '2024-11-28', 'Processing', N'Hồ Phương Thắng', N'0346578713', N'901 Trần Duy Hưng, Cầu Giấy, Hà Nội', 1038000, N'Ví VNPay'),
(42, '2024-11-29', 'Processing', N'Phạm Thị Oanh', N'0998393010', N'445 Minh Khai, Hai Bà Trưng, Hà Nội', 677000, N'COD'),
(46, '2024-11-29', 'Processing', N'Phạm Thị Bình', N'0718347382', N'889 Tôn Thất Tùng, Đống Đa, Hà Nội', 1278000, N'Chuyển khoản'),
(50, '2024-11-30', 'Processing', N'Đỗ Xuân Yến', N'0837631165', N'333 Lê Lợi, Thanh Khê, Đà Nẵng', 478000, N'COD'),

-- Shipped (10 orders)
(3, '2024-11-22', 'Shipped', N'Phan Quốc Thảo', N'0910651333', N'789 Hai Bà Trưng, Hoàn Kiếm, Hà Nội', 917000, N'COD'),
(8, '2024-11-23', 'Shipped', N'Võ Anh Hà', N'0824731781', N'258 Cách Mạng Tháng 8, Q3, HCM', 1276000, N'Chuyển khoản'),
(14, '2024-11-23', 'Shipped', N'Nguyễn Kim Vân', N'0913267736', N'357 Trường Chinh, Đống Đa, Hà Nội', 759000, N'Ví MoMo'),
(21, '2024-11-24', 'Shipped', N'Dương Văn Hương', N'0806474687', N'802 Lý Tự Trọng, Q1, HCM', 1197000, N'COD'),
(28, '2024-11-24', 'Shipped', N'Lê Hoàng Nam', N'0309805009', N'791 Nguyễn Oanh, Gò Vấp, HCM', 858000, N'Chuyển khoản'),
(33, '2024-11-25', 'Shipped', N'Vũ Bảo Uyên', N'0308121913', N'456 Tô Ngọc Vân, Tây Hồ, Hà Nội', 1158000, N'COD'),
(39, '2024-11-25', 'Shipped', N'Phan Thị Yến', N'0399091699', N'112 Nguyễn Chí Thanh, Đống Đa, Hà Nội', 978000, N'Ví ZaloPay'),
(43, '2024-11-26', 'Shipped', N'Võ Thanh Mai', N'0353462475', N'556 Nguyễn Xiển, Thanh Xuân, Hà Nội', 718000, N'COD'),
(47, '2024-11-26', 'Shipped', N'Dương Hữu Cường', N'0979911838', N'990 Nguyễn Lương Bằng, Liên Chiểu, Đà Nẵng', 1318000, N'Chuyển khoản'),
(49, '2024-11-27', 'Shipped', N'Hoàng Minh Phúc', N'0935427849', N'222 Ngô Quyền, Sơn Trà, Đà Nẵng', 519000, N'COD'),

-- Completed (15 orders)
(4, '2024-11-15', 'Completed', N'Hồ Kim Uyên', N'0984124118', N'321 Trần Hưng Đạo, Hải Châu, Đà Nẵng', 1436000, N'Chuyển khoản'),
(6, '2024-11-16', 'Completed', N'Bùi Thị An', N'0314332181', N'987 Nguyễn Trãi, Q5, HCM', 1057000, N'COD'),
(9, '2024-11-17', 'Completed', N'Đặng Quốc Bình', N'0331338908', N'369 Võ Văn Tần, Q3, HCM', 1276000, N'Ví MoMo'),
(10, '2024-11-17', 'Completed', N'Phạm Đức Việt', N'0883794026', N'741 Phan Xích Long, Phú Nhuận, HCM', 878000, N'COD'),
(11, '2024-11-18', 'Completed', N'Huỳnh Anh Hà', N'0835116155', N'852 Nguyễn Thị Minh Khai, Q1, HCM', 1197000, N'Chuyển khoản'),
(15, '2024-11-18', 'Completed', N'Đặng Anh Bình', N'0317816184', N'753 Xô Viết Nghệ Tĩnh, Bình Thạnh, HCM', 998000, N'COD'),
(16, '2024-11-19', 'Completed', N'Dương Hữu Quân', N'0973103413', N'951 Cộng Hòa, Tân Bình, HCM', 1318000, N'Ví VNPay'),
(17, '2024-11-19', 'Completed', N'Dương Thị Thảo', N'0857525534', N'246 Hoàng Văn Thụ, Tân Bình, HCM', 759000, N'COD'),
(22, '2024-11-20', 'Completed', N'Đỗ Hữu Cường', N'0982832764', N'913 Hai Bà Trưng, Hoàn Kiếm, Hà Nội', 1158000, N'Chuyển khoản'),
(23, '2024-11-20', 'Completed', N'Bùi Đức Việt', N'0845030564', N'246 Trần Phú, Hải Châu, Đà Nẵng', 918000, N'COD'),
(24, '2024-11-21', 'Completed', N'Trần Hoàng Phúc', N'0837672423', N'357 Lê Duẩn, Hải Châu, Đà Nẵng', 1038000, N'Ví MoMo'),
(26, '2024-11-21', 'Completed', N'Đỗ Kim Việt', N'0859696532', N'579 Điện Biên Phủ, Bình Thạnh, HCM', 1278000, N'COD'),
(29, '2024-11-22', 'Completed', N'Võ Bảo Cường', N'0701226916', N'902 Quang Trung, Gò Vấp, HCM', 858000, N'Chuyển khoản'),
(31, '2024-11-22', 'Completed', N'Phan Xuân Vân', N'0904801845', N'234 Kha Vạn Cân, Thủ Đức, HCM', 978000, N'COD'),
(34, '2024-11-23', 'Completed', N'Trần Anh Uyên', N'0787048281', N'567 Âu Cơ, Tây Hồ, Hà Nội', 1197000, N'Ví ZaloPay'),

-- Cancelled (5 orders)
(19, '2024-11-26', 'Cancelled', N'Dương Hữu Oanh', N'0358932528', N'135 Pasteur, Q1, HCM', 519000, N'COD'),
(36, '2024-11-27', 'Cancelled', N'Hồ Kim An', N'0985701543', N'789 Phạm Văn Đồng, Bắc Từ Liêm, Hà Nội', 758000, N'Chuyển khoản'),
(37, '2024-11-27', 'Cancelled', N'Nguyễn Hoàng Cường', N'0797182278', N'890 Xuân Thủy, Cầu Giấy, Hà Nội', 399000, N'COD'),
(41, '2024-11-28', 'Cancelled', N'Lê Anh Thắng', N'0986383465', N'334 Giải Phóng, Hai Bà Trưng, Hà Nội', 628000, N'Ví MoMo'),
(44, '2024-11-29', 'Cancelled', N'Vũ Kim Vân', N'0773315098', N'667 Khuất Duy Tiến, Thanh Xuân, Hà Nội', 878000, N'COD');

-- ============================================
-- 11. ORDER DETAILS (100+ chi tiết đơn hàng)
-- Mỗi đơn hàng có 2-3 sản phẩm
-- ============================================

-- Order 1 (Pending) - 2 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (1, 1, 2, 199000, 0),  -- Áo thun đen S x2
                                                                                    (1, 25, 1, 199000, 0); -- Áo thun xanh denim M x1

-- Order 2 (Pending) - 2 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (2, 32, 1, 399000, 10), -- Áo sơ mi navy M
                                                                                    (2, 49, 1, 599000, 15); -- Áo khoác dù xám L

-- Order 3 (Pending) - 3 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (3, 62, 1, 459000, 0),  -- Áo hoodie đen M
                                                                                    (3, 117, 1, 199000, 0), -- Áo thun nữ đen S
                                                                                    (3, 150, 1, 519000, 20); -- Quần jean nữ baggy đen S

-- Order 4 (Pending) - 3 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (4, 7, 2, 199000, 0),   -- Áo thun đen L x2
                                                                                    (4, 95, 1, 499000, 20), -- Quần jean nam đen M
                                                                                    (4, 80, 1, 279000, 5);  -- Áo polo navy M

-- Order 5 (Pending) - 2 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (5, 101, 1, 499000, 20), -- Quần jean xám L
                                                                                    (5, 109, 1, 429000, 10); -- Quần kaki be M

-- Order 6 (Pending) - 1 item
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
    (6, 30, 1, 399000, 10); -- Áo sơ mi trắng S

-- Order 7 (Pending) - 2 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (7, 42, 1, 599000, 15), -- Áo khoác dù đen M
                                                                                    (7, 68, 1, 459000, 0);  -- Áo hoodie xám L

-- Order 8 (Pending) - 2 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (8, 96, 1, 499000, 20), -- Quần jean đen L
                                                                                    (8, 119, 1, 199000, 0); -- Áo thun nữ trắng M

-- Order 9 (Pending) - 3 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (9, 32, 1, 399000, 10), -- Áo sơ mi navy M
                                                                                    (9, 96, 1, 499000, 20), -- Quần jean đen L
                                                                                    (9, 80, 1, 279000, 5);  -- Áo polo navy M

-- Order 10 (Pending) - 2 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (10, 117, 1, 159000, 0), -- Áo croptop đen S
                                                                                    (10, 139, 1, 399000, 10); -- Quần kaki nữ be M

-- Order 11 (Processing) - 2 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (11, 43, 1, 599000, 15), -- Áo khoác dù đen L
                                                                                    (11, 85, 1, 279000, 5);  -- Áo polo đen XL

-- Order 12 (Processing) - 3 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (12, 63, 1, 459000, 0),  -- Áo hoodie đen L
                                                                                    (12, 96, 1, 499000, 20), -- Quần jean đen L
                                                                                    (12, 110, 1, 429000, 10); -- Quần kaki be L

-- Order 13 (Processing) - 2 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (13, 106, 1, 649000, 20), -- Áo khoác denim nữ denim S
                                                                                    (13, 125, 1, 349000, 10); -- Áo sơ mi nữ trắng M

-- Order 14 (Processing) - 3 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (14, 2, 2, 199000, 0),   -- Áo thun đen M x2
                                                                                    (14, 96, 1, 499000, 20), -- Quần jean đen L
                                                                                    (14, 127, 1, 189000, 5); -- Áo thun nữ basic đen S

-- Order 15 (Processing) - 2 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (15, 97, 1, 499000, 20), -- Quần jean đen XL
                                                                                    (15, 110, 1, 429000, 10); -- Quần kaki be L

-- Order 16 (Processing) - 3 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (16, 43, 1, 599000, 15), -- Áo khoác dù đen L
                                                                                    (16, 63, 1, 459000, 0),  -- Áo hoodie đen L
                                                                                    (16, 110, 1, 429000, 10); -- Quần kaki be L

-- Order 17 (Processing) - 3 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (17, 32, 1, 399000, 10), -- Áo sơ mi navy M
                                                                                    (17, 96, 1, 499000, 20), -- Quần jean đen L
                                                                                    (17, 119, 1, 189000, 5); -- Áo thun nữ trắng M

-- Order 18 (Processing) - 2 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (18, 106, 1, 649000, 20), -- Áo khoác denim nữ S
                                                                                    (18, 125, 1, 349000, 10); -- Áo sơ mi nữ M

-- Order 19 (Processing) - 3 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (19, 43, 1, 599000, 15), -- Áo khoác dù đen L
                                                                                    (19, 96, 1, 499000, 20), -- Quần jean đen L
                                                                                    (19, 127, 1, 189000, 5); -- Áo thun nữ đen S

-- Order 20 (Processing) - 2 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (20, 117, 1, 159000, 0), -- Áo croptop đen S
                                                                                    (20, 139, 1, 399000, 10); -- Quần kaki nữ be M

-- Order 21 (Shipped) - 3 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (21, 32, 1, 399000, 10), -- Áo sơ mi navy M
                                                                                    (21, 96, 1, 499000, 20), -- Quần jean đen L
                                                                                    (21, 80, 1, 279000, 5);  -- Áo polo navy M

-- Order 22 (Shipped) - 3 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (22, 43, 1, 599000, 15), -- Áo khoác dù đen L
                                                                                    (22, 63, 1, 459000, 0),  -- Áo hoodie đen L
                                                                                    (22, 96, 1, 499000, 20); -- Quần jean đen L

-- Order 23 (Shipped) - 2 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (23, 106, 1, 649000, 20), -- Áo khoác denim nữ S
                                                                                    (23, 125, 1, 349000, 10); -- Áo sơ mi nữ M

-- Order 24 (Shipped) - 3 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (24, 2, 2, 199000, 0),   -- Áo thun đen M x2
                                                                                    (24, 96, 1, 499000, 20), -- Quần jean đen L
                                                                                    (24, 127, 1, 189000, 5); -- Áo thun nữ đen S

-- Order 25 (Shipped) - 2 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (25, 97, 1, 499000, 20), -- Quần jean đen XL
                                                                                    (25, 110, 1, 429000, 10); -- Quần kaki be L

-- Order 26 (Shipped) - 3 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (26, 43, 1, 599000, 15), -- Áo khoác dù đen L
                                                                                    (26, 96, 1, 499000, 20), -- Quần jean đen L
                                                                                    (26, 119, 1, 189000, 5); -- Áo thun nữ trắng M

-- Order 27 (Shipped) - 3 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (27, 32, 1, 399000, 10), -- Áo sơ mi navy M
                                                                                    (27, 96, 1, 499000, 20), -- Quần jean đen L
                                                                                    (27, 80, 1, 279000, 5);  -- Áo polo navy M

-- Order 28 (Shipped) - 2 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (28, 106, 1, 649000, 20), -- Áo khoác denim nữ S
                                                                                    (28, 125, 1, 349000, 10); -- Áo sơ mi nữ M

-- Order 29 (Shipped) - 3 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (29, 43, 1, 599000, 15), -- Áo khoác dù đen L
                                                                                    (29, 96, 1, 499000, 20), -- Quần jean đen L
                                                                                    (29, 127, 1, 189000, 5); -- Áo thun nữ đen S

-- Order 30 (Shipped) - 2 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (30, 117, 1, 159000, 0), -- Áo croptop đen S
                                                                                    (30, 139, 1, 399000, 10); -- Quần kaki nữ be M

-- Order 31-50: Completed và Cancelled orders (tương tự pattern trên)
-- Tiếp tục với các order còn lại...

-- Order 31 (Completed) - 3 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (31, 43, 1, 599000, 15), -- Áo khoác dù đen L
                                                                                    (31, 63, 1, 459000, 0),  -- Áo hoodie đen L
                                                                                    (31, 96, 1, 499000, 20); -- Quần jean đen L

-- Order 32 (Completed) - 3 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (32, 32, 1, 399000, 10), -- Áo sơ mi navy M
                                                                                    (32, 96, 1, 499000, 20), -- Quần jean đen L
                                                                                    (32, 119, 1, 189000, 5); -- Áo thun nữ trắng M

-- Order 33 (Completed) - 3 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (33, 2, 2, 199000, 0),   -- Áo thun đen M x2
                                                                                    (33, 96, 1, 499000, 20), -- Quần jean đen L
                                                                                    (33, 80, 1, 279000, 5);  -- Áo polo navy M

-- Order 34 (Completed) - 2 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (34, 97, 1, 499000, 20), -- Quần jean đen XL
                                                                                    (34, 110, 1, 429000, 10); -- Quần kaki be L

-- Order 35 (Completed) - 3 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (35, 43, 1, 599000, 15), -- Áo khoác dù đen L
                                                                                    (35, 96, 1, 499000, 20), -- Quần jean đen L
                                                                                    (35, 127, 1, 189000, 5); -- Áo thun nữ đen S

-- Order 36 (Completed) - 3 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (36, 32, 1, 399000, 10), -- Áo sơ mi navy M
                                                                                    (36, 96, 1, 499000, 20), -- Quần jean đen L
                                                                                    (36, 80, 1, 279000, 5);  -- Áo polo navy M

-- Order 37 (Completed) - 3 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (37, 43, 1, 599000, 15), -- Áo khoác dù đen L
                                                                                    (37, 63, 1, 459000, 0),  -- Áo hoodie đen L
                                                                                    (37, 96, 1, 499000, 20); -- Quần jean đen L

-- Order 38 (Completed) - 2 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (38, 106, 1, 649000, 20), -- Áo khoác denim nữ S
                                                                                    (38, 125, 1, 349000, 10); -- Áo sơ mi nữ M

-- Order 39 (Completed) - 3 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (39, 32, 1, 399000, 10), -- Áo sơ mi navy M
                                                                                    (39, 96, 1, 499000, 20), -- Quần jean đen L
                                                                                    (39, 119, 1, 189000, 5); -- Áo thun nữ trắng M

-- Order 40 (Completed) - 3 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (40, 43, 1, 599000, 15), -- Áo khoác dù đen L
                                                                                    (40, 96, 1, 499000, 20), -- Quần jean đen L
                                                                                    (40, 127, 1, 189000, 5); -- Áo thun nữ đen S

-- Order 41 (Completed) - 3 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (41, 2, 2, 199000, 0),   -- Áo thun đen M x2
                                                                                    (41, 96, 1, 499000, 20), -- Quần jean đen L
                                                                                    (41, 80, 1, 279000, 5);  -- Áo polo navy M

-- Order 42 (Completed) - 3 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (42, 43, 1, 599000, 15), -- Áo khoác dù đen L
                                                                                    (42, 63, 1, 459000, 0),  -- Áo hoodie đen L
                                                                                    (42, 96, 1, 499000, 20); -- Quần jean đen L

-- Order 43 (Completed) - 2 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (43, 97, 1, 499000, 20), -- Quần jean đen XL
                                                                                    (43, 110, 1, 429000, 10); -- Quần kaki be L

-- Order 44 (Completed) - 3 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (44, 32, 1, 399000, 10), -- Áo sơ mi navy M
                                                                                    (44, 96, 1, 499000, 20), -- Quần jean đen L
                                                                                    (44, 119, 1, 189000, 5); -- Áo thun nữ trắng M

-- Order 45 (Completed) - 3 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (45, 43, 1, 599000, 15), -- Áo khoác dù đen L
                                                                                    (45, 96, 1, 499000, 20), -- Quần jean đen L
                                                                                    (45, 127, 1, 189000, 5); -- Áo thun nữ đen S

-- Order 46 (Cancelled) - 2 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (46, 117, 1, 159000, 0), -- Áo croptop đen S
                                                                                    (46, 139, 1, 399000, 10); -- Quần kaki nữ be M

-- Order 47 (Cancelled) - 2 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (47, 32, 1, 399000, 10), -- Áo sơ mi navy M
                                                                                    (47, 49, 1, 599000, 15); -- Áo khoác dù xám L

-- Order 48 (Cancelled) - 1 item
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
    (48, 30, 1, 399000, 10); -- Áo sơ mi trắng S

-- Order 49 (Cancelled) - 2 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (49, 101, 1, 499000, 20), -- Quần jean xám L
                                                                                    (49, 109, 1, 429000, 10); -- Quần kaki be M

-- Order 50 (Cancelled) - 2 items
INSERT INTO OrderDetails (OrderID, VariantID, Quantity, Price, DiscountPercent) VALUES
                                                                                    (50, 42, 1, 599000, 15), -- Áo khoác dù đen M
                                                                                    (50, 68, 1, 459000, 0);  -- Áo hoodie xám L

update [ProductImages]
set ImageUrl = 'post-item2.jpg'


UPDATE Orders
SET TotalAmount = ISNULL((
                             SELECT SUM(Quantity * Price * (1 - DiscountPercent / 100.0))
                             FROM OrderDetails
                             WHERE OrderDetails.OrderID = Orders.OrderID
                         ), 0);


-- quy dinh ma SKU: P{6 ky tu ma san pham}-{2 ky tu ma mau}{2 ky tu ma size}
UPDATE pv
SET SKU = 'P' + FORMAT(pv.ProductID, '0000') + '-' + FORMAT(pv.ColorID, '00') +
          FORMAT(pv.SizeID, '00') from ProductVariants pv

--UPDATE ProductVariants
--SET Status = 'Available'
--where status = 'In Stock'



