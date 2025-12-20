using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using FashionStore.Models;

namespace FashionStore.Services
{
    public class OrderService
    {
        private readonly FashionStoreEntities _db;

        public OrderService(FashionStoreEntities db)
        {
            _db = db;
        }

        public bool CanChangeStatus(string fromStatus, string toStatus)
        {
            if (string.IsNullOrEmpty(fromStatus) || string.IsNullOrEmpty(toStatus))
                return false;

            if (fromStatus == toStatus)
                return false;

            var from = ParseOrderStatus(fromStatus);
            var to = ParseOrderStatus(toStatus);

            if (!from.HasValue || !to.HasValue)
                return false;

            return CanChangeStatus(from.Value, to.Value);
        }

        public bool CanChangeStatus(OrderStatus fromStatus, OrderStatus toStatus)
        {
            if (fromStatus == toStatus)
                return false;

            var allowedTransitions = new Dictionary<OrderStatus, List<OrderStatus>>
            {
                { OrderStatus.Pending, new List<OrderStatus> { OrderStatus.Processing, OrderStatus.Cancelled } },
                { OrderStatus.Processing, new List<OrderStatus> { OrderStatus.Shipped, OrderStatus.Cancelled } },
                { OrderStatus.Shipped, new List<OrderStatus> { OrderStatus.Completed } },
                { OrderStatus.Completed, new List<OrderStatus>() },
                { OrderStatus.Cancelled, new List<OrderStatus>() }
            };

            if (!allowedTransitions.ContainsKey(fromStatus))
                return false;

            return allowedTransitions[fromStatus].Contains(toStatus);
        }

        public void ValidateStockForOrder(List<CartItem> cartItems)
        {
            foreach (var cartItem in cartItems)
            {
                var variant = _db.ProductVariants
                    .Where(v => v.VariantID == cartItem.VariantID)
                    .FirstOrDefault();

                if (variant == null)
                    throw new InvalidOperationException($"Product variant {cartItem.VariantID} not found.");

                if (cartItem.Quantity > variant.Stock)
                    throw new InvalidOperationException(
                        $"Insufficient stock for product {variant.Product?.ProductName}. " +
                        $"Requested: {cartItem.Quantity}, Available: {variant.Stock}");
            }
        }

        public void DeductStock(List<CartItem> cartItems)
        {
            foreach (var cartItem in cartItems)
            {
                var variant = _db.ProductVariants
                    .Where(v => v.VariantID == cartItem.VariantID)
                    .FirstOrDefault();

                if (variant == null)
                    throw new InvalidOperationException($"Product variant {cartItem.VariantID} not found.");

                if (cartItem.Quantity > variant.Stock)
                    throw new InvalidOperationException(
                        $"Insufficient stock for product {variant.Product?.ProductName}. " +
                        $"Requested: {cartItem.Quantity}, Available: {variant.Stock}");

                variant.Stock -= cartItem.Quantity;

                if (variant.Stock < 0)
                    throw new InvalidOperationException($"Stock cannot be negative for variant {variant.VariantID}.");
            }
        }

        public void RestoreStock(Order order)
        {
            foreach (var orderDetail in order.OrderDetails)
            {
                var variant = _db.ProductVariants
                    .Where(v => v.VariantID == orderDetail.VariantID)
                    .FirstOrDefault();

                if (variant != null)
                {
                    variant.Stock += orderDetail.Quantity;
                }
            }
        }

        public void LogStatusChange(int orderId, string oldStatus, string newStatus, string changedBy, string note = null)
        {
            try
            {
                // Use raw SQL to insert into OrderStatusHistory
                // This works even if the table is not in EDMX yet
                var sql = @"
                    INSERT INTO OrderStatusHistory (OrderID, OldStatus, NewStatus, ChangedBy, ChangedAt, Note)
                    VALUES (@p0, @p1, @p2, @p3, @p4, @p5)";
                
                _db.Database.ExecuteSqlCommand(sql,
                    orderId,
                    (object)oldStatus ?? DBNull.Value,
                    newStatus,
                    changedBy ?? "System",
                    DateTime.Now,
                    (object)note ?? DBNull.Value);
            }
            catch
            {
                // Silently fail if table doesn't exist yet
                // This allows the code to work before migration is run
                // After running migration, this will work correctly
            }
        }

        public void OnOrderCompleted(Order order)
        {
            if (order.Status != OrderStatus.Completed.ToString())
                return;

            // Extension points for future features
            // TODO: Add loyalty points
            // TODO: Send email notification
            // TODO: Update revenue statistics
        }

        public void OnOrderCancelled(Order order)
        {
            if (order.Status != OrderStatus.Cancelled.ToString())
                return;

            // Restore stock
            RestoreStock(order);

            // Payment status handling
            // COD orders remain Unpaid
            // Future: Handle refunds for paid orders
        }

        private OrderStatus? ParseOrderStatus(string status)
        {
            if (string.IsNullOrEmpty(status))
                return null;

            if (Enum.TryParse<OrderStatus>(status, true, out var result))
                return result;

            return null;
        }
    }
}

