-- ============================================
-- MIGRATION: Add PaymentStatus and OrderStatusHistory
-- Date: 2024-12-XX
-- ============================================

USE FashionStore;
GO

-- ============================================
-- 1. Add PaymentStatus column to Orders table
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'PaymentStatus')
BEGIN
    ALTER TABLE Orders
    ADD PaymentStatus NVARCHAR(50) DEFAULT 'Unpaid';
    
    -- Update existing records
    UPDATE Orders
    SET PaymentStatus = 'Unpaid'
    WHERE PaymentStatus IS NULL;
    
    PRINT 'Added PaymentStatus column to Orders table';
END
GO

-- ============================================
-- 2. Create OrderStatusHistory table
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OrderStatusHistory')
BEGIN
    CREATE TABLE OrderStatusHistory
    (
        HistoryID INT PRIMARY KEY IDENTITY(1,1),
        OrderID INT NOT NULL,
        OldStatus NVARCHAR(50),
        NewStatus NVARCHAR(50) NOT NULL,
        ChangedBy NVARCHAR(100),
        ChangedAt DATETIME NOT NULL DEFAULT GETDATE(),
        Note NVARCHAR(500),
        
        FOREIGN KEY (OrderID) REFERENCES Orders(OrderID) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_OrderStatusHistory_OrderID ON OrderStatusHistory(OrderID);
    CREATE INDEX IX_OrderStatusHistory_ChangedAt ON OrderStatusHistory(ChangedAt);
    
    PRINT 'Created OrderStatusHistory table';
END
GO

-- ============================================
-- 3. Update existing Orders to use enum values
-- ============================================
UPDATE Orders
SET Status = 'Pending'
WHERE Status NOT IN ('Pending', 'Processing', 'Shipped', 'Completed', 'Cancelled')
   OR Status IS NULL;
GO

UPDATE Orders
SET PaymentMethod = 'COD'
WHERE PaymentMethod IS NULL;
GO

PRINT 'Migration completed successfully';
GO

