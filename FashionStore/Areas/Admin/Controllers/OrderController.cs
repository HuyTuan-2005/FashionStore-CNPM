using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using FashionStore.Models;
using FashionStore.ViewModels;

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
                    (x.Quantity) * (x.Price) * ((x.DiscountPercent) / 100)).Value,
                
                ShippingFee = 0, 
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
                new { StatusName = "Chờ xác nhận", StatusValue = "Pending" },
                new { StatusName = "Đang xử lý",   StatusValue = "Processing" },
                new { StatusName = "Đã giao hàng", StatusValue = "Shipped" },
                new { StatusName = "Hoàn thành",   StatusValue = "Completed" },
                new { StatusName = "Đã hủy",       StatusValue = "Cancelled" }
            };
        }

        public ActionResult EditStatus(int id, string status)
        {
            var order = _entities.Orders.Find(id);
            if (order != null)
            {
                order.Status = status;
                _entities.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        public ActionResult EditManyStatus(string id, string status)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(status))
            {
                return HttpNotFound();
            }

            var ids = id.Split(',').Select(int.Parse).ToList();

            List<Order> orders = _entities.Orders
                .Where(o => ids.Contains(o.OrderID)).ToList();

            foreach (var item in orders)
            {
                item.Status = status;
            }

            _entities.SaveChanges();
            return RedirectToAction("Index");
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