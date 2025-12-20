-- ============================================
-- SCRIPT TỔNG HỢP: THÊM SHIPPING METHOD VÀ SHIPPING FEE
-- Chạy file này để thực hiện tất cả các bước:
-- 1. Thêm 2 cột mới vào bảng Orders
-- 2. Update dữ liệu cho các đơn hàng cũ
-- 3. Kiểm tra kết quả
-- ============================================
USE FashionStore;
GO

PRINT '========================================';
PRINT 'BẮT ĐẦU CẬP NHẬT DATABASE';
PRINT '========================================';
GO

-- ============================================
-- BƯỚC 1: THÊM CỘT ShippingMethod
-- ============================================
PRINT '';
PRINT 'BƯỚC 1: Thêm cột ShippingMethod...';
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'ShippingMethod')
BEGIN
    ALTER TABLE Orders
    ADD ShippingMethod NVARCHAR(50) NOT NULL DEFAULT 'Standard';
    
    -- Update existing records
    UPDATE Orders SET ShippingMethod = 'Standard' WHERE ShippingMethod IS NULL;
    
    PRINT '✓ Đã thêm cột ShippingMethod';
END
ELSE
BEGIN
    PRINT '✓ Cột ShippingMethod đã tồn tại';
END
GO

-- ============================================
-- BƯỚC 2: THÊM CỘT ShippingFee
-- ============================================
PRINT '';
PRINT 'BƯỚC 2: Thêm cột ShippingFee...';
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'ShippingFee')
BEGIN
    ALTER TABLE Orders
    ADD ShippingFee DECIMAL(18, 2) NOT NULL DEFAULT 0;
    
    -- Update existing records
    UPDATE Orders SET ShippingFee = 0 WHERE ShippingFee IS NULL;
    
    -- Add check constraint to ensure shipping fee is not negative
    ALTER TABLE Orders
    ADD CONSTRAINT CK_Orders_ShippingFee_NonNegative 
    CHECK (ShippingFee >= 0);
    
    PRINT '✓ Đã thêm cột ShippingFee';
END
ELSE
BEGIN
    PRINT '✓ Cột ShippingFee đã tồn tại';
    
    -- Add check constraint if it doesn't exist
    IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_Orders_ShippingFee_NonNegative')
    BEGIN
        ALTER TABLE Orders
        ADD CONSTRAINT CK_Orders_ShippingFee_NonNegative 
        CHECK (ShippingFee >= 0);
        PRINT '✓ Đã thêm constraint cho ShippingFee';
    END
END
GO

-- ============================================
-- BƯỚC 3: UPDATE DỮ LIỆU CHO CÁC ĐƠN HÀNG CŨ
-- ============================================
PRINT '';
PRINT 'BƯỚC 3: Update dữ liệu cho các đơn hàng cũ...';
GO

-- Update ShippingMethod = 'Standard' cho tất cả đơn hàng
UPDATE Orders
SET ShippingMethod = 'Standard'
WHERE ShippingMethod IS NULL OR ShippingMethod = '';

DECLARE @UpdatedShippingMethod INT = @@ROWCOUNT;
PRINT '✓ Đã update ShippingMethod cho ' + CAST(@UpdatedShippingMethod AS NVARCHAR(10)) + ' đơn hàng';
GO

-- Update ShippingFee = 0 cho tất cả đơn hàng cũ
UPDATE Orders
SET ShippingFee = 0
WHERE ShippingFee IS NULL;

DECLARE @UpdatedShippingFee INT = @@ROWCOUNT;
PRINT '✓ Đã update ShippingFee cho ' + CAST(@UpdatedShippingFee AS NVARCHAR(10)) + ' đơn hàng';
GO

-- ============================================
-- BƯỚC 4: KIỂM TRA KẾT QUẢ
-- ============================================
PRINT '';
PRINT 'BƯỚC 4: Kiểm tra kết quả...';
GO

-- Kiểm tra cấu trúc columns
PRINT '';
PRINT '--- Cấu trúc columns ---';
SELECT 
    COLUMN_NAME AS TenCot,
    DATA_TYPE AS KieuDuLieu,
    IS_NULLABLE AS ChoPhepNull,
    COLUMN_DEFAULT AS GiaTriMacDinh
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Orders'
AND COLUMN_NAME IN ('ShippingMethod', 'ShippingFee')
ORDER BY COLUMN_NAME;
GO

-- Kiểm tra dữ liệu
PRINT '';
PRINT '--- Thống kê dữ liệu ---';
SELECT 
    COUNT(*) AS TongSoDonHang,
    COUNT(CASE WHEN ShippingMethod = 'Standard' THEN 1 END) AS DonHangStandard,
    COUNT(CASE WHEN ShippingMethod IS NULL THEN 1 END) AS DonHangChuaCoShippingMethod,
    COUNT(CASE WHEN ShippingFee = 0 THEN 1 END) AS DonHangShippingFee0,
    COUNT(CASE WHEN ShippingFee > 0 THEN 1 END) AS DonHangCoShippingFee,
    COUNT(CASE WHEN ShippingFee IS NULL THEN 1 END) AS DonHangChuaCoShippingFee,
    AVG(ShippingFee) AS TrungBinhShippingFee,
    MIN(ShippingFee) AS MinShippingFee,
    MAX(ShippingFee) AS MaxShippingFee
FROM Orders;
GO

-- Kiểm tra constraint
PRINT '';
PRINT '--- Kiểm tra constraint ---';
IF EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_Orders_ShippingFee_NonNegative')
BEGIN
    PRINT '✓ Constraint CK_Orders_ShippingFee_NonNegative đã được tạo';
END
ELSE
BEGIN
    PRINT '✗ Constraint CK_Orders_ShippingFee_NonNegative chưa được tạo';
END
GO

-- Kiểm tra dữ liệu không hợp lệ
PRINT '';
PRINT '--- Kiểm tra dữ liệu không hợp lệ ---';

-- Đơn hàng có ShippingFee < 0
DECLARE @InvalidShippingFee INT;
SELECT @InvalidShippingFee = COUNT(*) 
FROM Orders 
WHERE ShippingFee < 0;

IF @InvalidShippingFee > 0
BEGIN
    PRINT '✗ Có ' + CAST(@InvalidShippingFee AS NVARCHAR(10)) + ' đơn hàng có ShippingFee < 0 (KHÔNG HỢP LỆ)';
    SELECT OrderID, ShippingFee 
    FROM Orders 
    WHERE ShippingFee < 0;
END
ELSE
BEGIN
    PRINT '✓ Tất cả đơn hàng có ShippingFee >= 0';
END
GO

-- Đơn hàng có ShippingMethod NULL hoặc rỗng
DECLARE @InvalidShippingMethod INT;
SELECT @InvalidShippingMethod = COUNT(*) 
FROM Orders 
WHERE ShippingMethod IS NULL OR ShippingMethod = '';

IF @InvalidShippingMethod > 0
BEGIN
    PRINT '✗ Có ' + CAST(@InvalidShippingMethod AS NVARCHAR(10)) + ' đơn hàng có ShippingMethod NULL hoặc rỗng (KHÔNG HỢP LỆ)';
    SELECT OrderID, ShippingMethod 
    FROM Orders 
    WHERE ShippingMethod IS NULL OR ShippingMethod = '';
END
ELSE
BEGIN
    PRINT '✓ Tất cả đơn hàng đã có ShippingMethod';
END
GO

-- ============================================
-- HOÀN TẤT
-- ============================================
PRINT '';
PRINT '========================================';
PRINT 'HOÀN TẤT CẬP NHẬT DATABASE';
PRINT '========================================';
PRINT '';
PRINT 'BƯỚC TIẾP THEO:';
PRINT '1. Mở Visual Studio';
PRINT '2. Right-click vào FashionStore.edmx';
PRINT '3. Chọn "Update Model from Database..."';
PRINT '4. Tab "Refresh" → Check bảng "Orders" → Finish';
PRINT '5. Build Solution (Ctrl+Shift+B)';
PRINT '';
GO

