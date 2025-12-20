using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Web.Mvc;
using FashionStore.Models;
using FashionStore.ViewModels;
using FashionStore.Services;

namespace FashionStore.Areas.Admin.Controllers
{
    public class OrderController : Controller
    {
        private readonly FashionStoreEntities _entities = new FashionStoreEntities();

        // GET
        public ActionResult Index(int? page, int? search, string status, DateTime? fromDate)
        {
            int pageSize = 10;
            int pageNumber = (page ?? 1);

            IQueryable<Order> query = _entities.Orders;

            if (search.HasValue)
                query = query.Where(o => o.OrderID == search.Value);
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(o => o.Status == status);
            if (fromDate.HasValue)
                query = query.Where(o => o.OrderDate >= fromDate.Value);

            // sap xep giam dan theo ngay dat hang
            query = query.OrderByDescending(o => o.OrderDate);

            int total = query.Count();
            var lstOrder = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();


            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

            SetViewBagStatus();

            return View(lstOrder);
        }


        // GET
        public ActionResult Details(int id)
        {
            SetViewBagStatus();

            var order = _entities.Orders.Find(id);

            if (order == null) return HttpNotFound();

            var viewModel = new OrderDetailViewModel
            {
                Order = order,
                SubTotal = order.OrderDetails.Sum(x =>
                    (x.Quantity) * (x.Price)),
                
                TotalDiscount = order.OrderDetails.Sum(x =>
                    (x.Quantity) * (x.Price) * ((x.DiscountPercent ?? 0) / 100)),
                
                ShippingFee = order.ShippingFee ?? 0, // Lấy từ database, mặc định 0 nếu null
            };

            viewModel.GrandTotal = viewModel.SubTotal - viewModel.TotalDiscount + viewModel.ShippingFee;

            switch (order.Status)
            {
                case "Completed":
                    viewModel.StatusBadgeClass = "badge-success";
                    viewModel.StatusDisplayName = "Hoàn thành";
                    break;
                case "Shipped":
                    viewModel.StatusBadgeClass = "badge-info";
                    viewModel.StatusDisplayName = "Đang giao";
                    break;
                case "Processing":
                    viewModel.StatusBadgeClass = "badge-primary";
                    viewModel.StatusDisplayName = "Đang xử lý";
                    break;
                case "Pending":
                    viewModel.StatusBadgeClass = "badge-warning";
                    viewModel.StatusDisplayName = "Chờ xác nhận";
                    break;
                case "Cancelled":
                    viewModel.StatusBadgeClass = "badge-danger";
                    viewModel.StatusDisplayName = "Đã hủy";
                    break;
                default:
                    viewModel.StatusBadgeClass = "badge-secondary";
                    viewModel.StatusDisplayName = "Không xác định";
                    break;
            }

            return View(viewModel);
        }

        private void SetViewBagStatus()
        {
            ViewBag.Status = new[]
            {
                new { StatusName = "Chờ xác nhận", StatusValue = OrderStatus.Pending.ToString() },
                new { StatusName = "Đang xử lý",   StatusValue = OrderStatus.Processing.ToString() },
                new { StatusName = "Đã giao hàng", StatusValue = OrderStatus.Shipped.ToString() },
                new { StatusName = "Hoàn thành",   StatusValue = OrderStatus.Completed.ToString() },
                new { StatusName = "Đã hủy",       StatusValue = OrderStatus.Cancelled.ToString() }
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditStatus(int id, string status, string note = null)
        {
            var order = _entities.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefault(o => o.OrderID == id);

            if (order == null)
            {
                TempData["Error"] = "Đơn hàng không tồn tại.";
                return RedirectToAction("Index");
            }

            var orderService = new OrderService(_entities);
            var oldStatus = order.Status;

            // Validate status transition
            if (!orderService.CanChangeStatus(oldStatus, status))
            {
                TempData["Error"] = $"Không thể chuyển đổi trạng thái từ '{GetStatusDisplayName(oldStatus)}' sang '{GetStatusDisplayName(status)}'.";
                return RedirectToAction("Details", new { id });
            }

            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    // Update status
                    order.Status = status;

                    // Handle business logic based on new status
                    if (status == OrderStatus.Cancelled.ToString())
                    {
                        orderService.OnOrderCancelled(order);
                    }
                    else if (status == OrderStatus.Completed.ToString())
                    {
                        orderService.OnOrderCompleted(order);
                    }

                    _entities.SaveChanges();

                    // Log status change
                    var changedBy = User?.Identity?.Name ?? "Admin";
                    orderService.LogStatusChange(id, oldStatus, status, changedBy, note);
                    _entities.SaveChanges();

                    transaction.Commit();

                    TempData["Success"] = "Cập nhật trạng thái đơn hàng thành công.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}";
                }
            }

            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditManyStatus(string id, string status)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(status))
            {
                TempData["Error"] = "Dữ liệu không hợp lệ.";
                return RedirectToAction("Index");
            }

            var ids = id.Split(',').Select(int.Parse).ToList();
            var orders = _entities.Orders
                .Include(o => o.OrderDetails)
                .Where(o => ids.Contains(o.OrderID))
                .ToList();

            var orderService = new OrderService(_entities);
            var changedBy = User?.Identity?.Name ?? "Admin";
            var successCount = 0;
            var failCount = 0;

            foreach (var order in orders)
            {
                var oldStatus = order.Status;

                if (!orderService.CanChangeStatus(oldStatus, status))
                {
                    failCount++;
                    continue;
                }

                using (var transaction = _entities.Database.BeginTransaction())
                {
                    try
                    {
                        order.Status = status;

                        if (status == OrderStatus.Cancelled.ToString())
                        {
                            orderService.OnOrderCancelled(order);
                        }
                        else if (status == OrderStatus.Completed.ToString())
                        {
                            orderService.OnOrderCompleted(order);
                        }

                        _entities.SaveChanges();

                        orderService.LogStatusChange(order.OrderID, oldStatus, status, changedBy);
                        _entities.SaveChanges();

                        transaction.Commit();
                        successCount++;
                    }
                    catch
                    {
                        transaction.Rollback();
                        failCount++;
                    }
                }
            }

            if (successCount > 0)
                TempData["Success"] = $"Đã cập nhật {successCount} đơn hàng.";
            if (failCount > 0)
                TempData["Error"] = $"Không thể cập nhật {failCount} đơn hàng.";

            return RedirectToAction("Index");
        }

        private string GetStatusDisplayName(string status)
        {
            switch (status)
            {
                case "Pending": return "Chờ xác nhận";
                case "Processing": return "Đang xử lý";
                case "Shipped": return "Đã giao hàng";
                case "Completed": return "Hoàn thành";
                case "Cancelled": return "Đã hủy";
                default: return status;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _entities.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}