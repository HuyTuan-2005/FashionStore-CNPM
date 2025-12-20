using FashionStore.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace FashionStore.Areas.Admin.Controllers
{
    public class DashboardController : Controller
    {
        private readonly FashionStoreEntities _entities = new FashionStoreEntities();


        public ActionResult Index(DateTime? fromDate, DateTime? toDate)
        {
            // Mặc định xem dữ liệu 7 ngày gần nhất nếu không chọn ngày
            DateTime startDate = fromDate ?? DateTime.Today.AddDays(-7);
            DateTime endDate = (toDate ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

            ViewBag.FromDate = startDate.ToString("yyyy-MM-dd");
            ViewBag.ToDate = (toDate ?? DateTime.Today).ToString("yyyy-MM-dd");

            // Thống kê Doanh thu (Chỉ tính đơn đã hoàn thành)
            ViewBag.TotalRevenue = _entities.Orders
                .Where(o => o.Status == "Completed" && o.OrderDate >= startDate && o.OrderDate <= endDate)
                .Sum(o => (decimal?)o.TotalAmount) ?? 0m;

            // Tổng số đơn hàng trong khoảng thời gian
            ViewBag.TotalOrders = _entities.Orders
                .Count(o => o.OrderDate >= startDate && o.OrderDate <= endDate);

            // Tổng số khách hàng (Toàn bộ hệ thống)
            ViewBag.TotalCustomers = _entities.Customers.Count();

            // Tổng đơn hàng đang chờ (Toàn bộ hệ thống hoặc theo ngày tùy nhu cầu)
            ViewBag.PendingOrders = _entities.Orders.Count(o => o.Status == "Pending");

            return View();
        }
        public ActionResult DonHangReport(DateTime? fromDate, DateTime? toDate)
        {
            DateTime start = fromDate ?? DateTime.Today.AddDays(-30);
            DateTime end = (toDate ?? DateTime.Today).AddDays(1).AddTicks(-1);

            var orders = _entities.Orders.Include(o => o.Customer)   // ⭐ BẮT BUỘC
                                            .Where(o => o.OrderDate >= start && o.OrderDate <= end)
                                            .OrderByDescending(o => o.OrderDate)
                                            .ToList();


            ViewBag.FromDate = start.ToString("yyyy-MM-dd");
            ViewBag.ToDate = end.ToString("yyyy-MM-dd");

            return View(orders);
        }
        public ActionResult CustomersReport(DateTime? fromDate, DateTime? toDate)
        {
            // 1. Thiết lập khoảng thời gian (mặc định 30 ngày qua nếu không nhập)
            DateTime start = fromDate ?? DateTime.Today.AddDays(-30);
            DateTime end = (toDate ?? DateTime.Today).AddDays(1).AddTicks(-1);

            // 2. Truy vấn dữ liệu từ database
            var customers = _entities.Customers
                .Where(c => c.RoleID == 3 && // THÊM ĐIỀU KIỆN NÀY: Chỉ lấy RoleID là 3
                            c.CreatedAt >= start &&
                            c.CreatedAt <= end)
                .OrderByDescending(c => c.CreatedAt)
                .ToList();

            // 3. Gửi dữ liệu ngày tháng ra View để hiển thị trong ô chọn ngày
            ViewBag.FromDate = start.ToString("yyyy-MM-dd");
            ViewBag.ToDate = end.ToString("yyyy-MM-dd");

            return View(customers);
        }
        public ActionResult RevenueReport(DateTime? fromDate, DateTime? toDate)
        {
            DateTime start = fromDate ?? DateTime.Today.AddDays(-30);
            DateTime end = (toDate ?? DateTime.Today).AddDays(1).AddTicks(-1);

            var orders = _entities.Orders
      .Include(o => o.Customer)   // ⭐ THÊM DÒNG NÀY
      .Where(o => o.Status == "Completed" &&
                  o.OrderDate >= start && o.OrderDate <= end)
      .OrderByDescending(o => o.OrderDate)
      .ToList();


            ViewBag.FromDate = start.ToString("yyyy-MM-dd");
            ViewBag.ToDate = end.ToString("yyyy-MM-dd");

            return View(orders);
        }
        public ActionResult SanPhamReport(DateTime? fromDate, DateTime? toDate)
        {
            DateTime startDate = fromDate ?? DateTime.Today.AddDays(-30);
            DateTime endDate = (toDate ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

            ViewBag.FromDate = startDate.ToString("yyyy-MM-dd");
            ViewBag.ToDate = (toDate ?? DateTime.Today).ToString("yyyy-MM-dd");

            return View();
        }

        // API: Lấy Top 5 sản phẩm kèm % để làm thanh tiến độ
        public JsonResult GetTopProductsReport()
        {
            var totalQty = _entities.OrderDetails
                .Where(od => od.Order.Status == "Completed")
                .Sum(x => (int?)x.Quantity) ?? 1; // Tránh chia cho 0

            var data = _entities.OrderDetails
                .Where(od => od.Order.Status == "Completed")
                .GroupBy(od => od.ProductVariant.Product.ProductName)
                .Select(g => new {
                    Name = g.Key,
                    Qty = g.Sum(x => x.Quantity),
                    // Tính % đóng góp của sản phẩm này trên tổng số lượng bán ra
                    Percentage = (g.Sum(x => x.Quantity) * 100) / totalQty
                })
                .OrderByDescending(x => x.Qty)
                .Take(5).ToList();

            return Json(data, JsonRequestBehavior.AllowGet);
        }

   
        // 1. API cho biểu đồ ĐƯỜNG: Chỉ hiện 1 đường tổng số lượng bán ra
        public JsonResult GetTotalQtyTrend(DateTime fromDate, DateTime toDate)
        {
            DateTime end = toDate.AddDays(1).AddTicks(-1);
            var data = _entities.OrderDetails
                .Where(od => od.Order.Status == "Completed" && od.Order.OrderDate >= fromDate && od.Order.OrderDate <= end)
                .GroupBy(od => DbFunctions.TruncateTime(od.Order.OrderDate))
                .Select(g => new
                {
                    Date = g.Key.Value,
                    Qty = g.Sum(x => (int?)x.Quantity) ?? 0
                })
                .OrderBy(x => x.Date).ToList()
                .Select(x => new { Date = x.Date.ToString("dd/MM"), Qty = x.Qty });

            return Json(data, JsonRequestBehavior.AllowGet);
        }
        // API: Lấy danh sách Top 10 khách hàng chi tiêu nhiều nhất (CLV)
        public JsonResult GetTop10SpendingCustomers()
        {
            var data = _entities.Orders
                .Where(o => o.Status == "Completed")
                .GroupBy(o => new { o.CustomerID, o.Customer.UserName })
                .Select(g => new {
                    Name = g.Key.UserName,
                    TotalSpent = g.Sum(x => x.TotalAmount)
                })
                .OrderByDescending(x => x.TotalSpent)
                .Take(10).ToList();

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        // API: Thống kê tỷ lệ quay lại (Retention Rate)
        public JsonResult GetCustomerRetention()
        {
            var customers = _entities.Customers.Where(c => c.RoleID == 3).ToList();

            // 1. Nhóm Xanh: Có >= 2 đơn Completed (Khách trung thành)
            var loyalCustomers = customers.Count(c => c.Orders.Count(o => o.Status == "Completed") >= 2);

            // 2. Nhóm Vàng: Có >= 2 đơn tổng nhưng chỉ có đúng 1 đơn Completed (Khách tiềm năng)
            var potentialCustomers = customers.Count(c =>
                c.Orders.Count() >= 2 &&
                c.Orders.Count(o => o.Status == "Completed") == 1
            );

            // 3. Nhóm Mới (Chưa mua gì): Không có bất kỳ đơn hàng nào
            var neverPurchased = customers.Count(c => !c.Orders.Any());

            // 4. Nhóm còn lại (Mua 1 lần hoặc các trường hợp khác)
            var others = customers.Count - loyalCustomers - potentialCustomers - neverPurchased;

            var data = new[] {
        new { label = "Trung thành (>=2 đơn xong)", value = loyalCustomers, color = "#1cc88a" }, // Xanh lá
        new { label = "Tiềm năng (Đặt >2, xong 1)", value = potentialCustomers, color = "#f6c23e" }, // Vàng
        new { label = "Chưa mua gì", value = neverPurchased, color = "#e74a3b" }, // Đỏ
        new { label = "Đã mua 1 lần", value = others, color = "#858796" } // Xám
    };

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCustomerSpendingByDay(DateTime fromDate, DateTime toDate)
        {
            DateTime end = toDate.AddDays(1).AddTicks(-1);

            var data = _entities.Orders
                .Where(o => o.Status == "Completed" && o.OrderDate >= fromDate && o.OrderDate <= end)
                .GroupBy(o => DbFunctions.TruncateTime(o.OrderDate))
                .Select(g => new
                {
                    Date = g.Key.Value,
                    TotalSpending = g.Sum(x => x.TotalAmount)
                })
                .OrderBy(x => x.Date)
                .ToList()
                .Select(x => new {
                    Date = x.Date.ToString("dd/MM/yyyy"),
                    Total = x.TotalSpending
                });

            return Json(data, JsonRequestBehavior.AllowGet);
        }
        // 2. API cho biểu đồ CỘT: Chia nhỏ từng sản phẩm (Mỗi sản phẩm 1 màu)
        public JsonResult GetProductBreakdown(DateTime fromDate, DateTime toDate)
        {
            DateTime end = toDate.AddDays(1).AddTicks(-1);

            // Lấy danh sách sản phẩm có bán
            var productNames = _entities.OrderDetails
                .Where(od => od.Order.Status == "Completed" && od.Order.OrderDate >= fromDate && od.Order.OrderDate <= end)
                .Select(od => od.ProductVariant.Product.ProductName).Distinct().ToList();

            var rawData = _entities.OrderDetails
                .Where(od => od.Order.Status == "Completed" && od.Order.OrderDate >= fromDate && od.Order.OrderDate <= end)
                .GroupBy(od => new { Date = DbFunctions.TruncateTime(od.Order.OrderDate), Name = od.ProductVariant.Product.ProductName })
                .Select(g => new { Date = g.Key.Date.Value, Name = g.Key.Name, Qty = g.Sum(x => x.Quantity) }).ToList();

            var allDates = rawData.Select(x => x.Date).Distinct().OrderBy(d => d).ToList();

            var datasets = productNames.Select(name => new {
                label = name,
                data = allDates.Select(d => rawData.FirstOrDefault(r => r.Date == d && r.Name == name)?.Qty ?? 0).ToList()
            }).ToList();

            return Json(new { labels = allDates.Select(d => d.ToString("dd/MM")).ToList(), datasets = datasets }, JsonRequestBehavior.AllowGet);
        }

        // 3. API cho BẢNG: Lấy toàn bộ sản phẩm (để dùng cho nút Xem thêm)
        public JsonResult GetAllProductsReport(DateTime? fromDate, DateTime? toDate)
        {
            // 1. Tính tổng số lượng bán theo khoảng thời gian để tính % chính xác
            var totalQty = _entities.OrderDetails
                .Where(od => od.Order.Status == "Completed" &&
                            (!fromDate.HasValue || od.Order.OrderDate >= fromDate) &&
                            (!toDate.HasValue || od.Order.OrderDate <= toDate))
                .Sum(x => (int?)x.Quantity) ?? 0;

            // 2. Lấy dữ liệu sản phẩm
            var data = _entities.Products.Select(p => new
            {
                Name = p.ProductName,
                // Chỉ tính số lượng bán trong khoảng thời gian lọc
                Qty = p.ProductVariants
                        .SelectMany(pv => pv.OrderDetails)
                        .Where(od => od.Order.Status == "Completed" &&
                                    (!fromDate.HasValue || od.Order.OrderDate >= fromDate) &&
                                    (!toDate.HasValue || od.Order.OrderDate <= toDate))
                        .Sum(od => (int?)od.Quantity) ?? 0,

                // Tồn kho là con số hiện tại (không phụ thuộc bộ lọc ngày)
                Stock = p.ProductVariants.Sum(pv => (int?)pv.Stock) ?? 0
            })
            .ToList()
            .Select(x => new {
                x.Name,
                x.Qty,
                x.Stock,
                Percentage = totalQty > 0 ? (x.Qty * 100) / totalQty : 0
            })
            .OrderByDescending(x => x.Qty)
            .ToList();

            return Json(data, JsonRequestBehavior.AllowGet);
        }
        // ================= CHART DATA =================
        public JsonResult RevenueByDay(DateTime fromDate, DateTime toDate)
        {
            DateTime end = toDate.AddDays(1).AddTicks(-1);
            var data = _entities.Orders.Where(o => o.Status == "Completed" && o.OrderDate >= fromDate && o.OrderDate <= end)
                .GroupBy(o => DbFunctions.TruncateTime(o.OrderDate))
                .Select(g => new { Date = g.Key.Value, Total = g.Sum(x => x.TotalAmount) })
                .OrderBy(x => x.Date)
                .ToList()
                .Select(x => new { Date = x.Date.ToString("dd/MM/yyyy"), Total = x.Total });
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public JsonResult CustomersByDay(DateTime fromDate, DateTime toDate)
        {
            DateTime end = toDate.AddDays(1).AddTicks(-1);
            var data = _entities.Customers.Where(c => c.CreatedAt >= fromDate && c.CreatedAt <= end)
                .GroupBy(c => DbFunctions.TruncateTime(c.CreatedAt))
                .Select(g => new { Date = g.Key.Value, Total = g.Count() })
                .OrderBy(x => x.Date)
                .ToList()
                .Select(x => new { Date = x.Date.ToString("dd/MM/yyyy"), Total = x.Total });
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public JsonResult OrdersByDay(DateTime fromDate, DateTime toDate)
        {
            DateTime end = toDate.AddDays(1).AddTicks(-1);

            // 1. Lấy tất cả đơn hàng trong khoảng thời gian vào bộ nhớ (để xử lý chuỗi tiếng Việt dễ hơn)
            var orders = _entities.Orders
                .Where(o => o.OrderDate >= fromDate && o.OrderDate <= end)
                .ToList();

            // 2. Nhóm theo ngày và đếm từng trạng thái cụ thể
            var data = orders
                .GroupBy(o => o.OrderDate.Value.Date)
                .Select(g => new {
                    Date = g.Key.ToString("dd/MM/yyyy"),
                    Pending = g.Count(x => x.Status == "Pending"),
                    Processing = g.Count(x => x.Status == "Processing"),
                    Shipped = g.Count(x => x.Status == "Shipped"),
                    Completed = g.Count(x => x.Status == "Completed"),
                    Cancelled = g.Count(x => x.Status == "Cancelled"),
                    // Lưu ý: Kiểm tra chính xác chuỗi "Chờ thanh toán" trong DB của bạn
                    Chothanhtoan = g.Count(x => x.Status == "Chờ thanh toán")
                })
                .OrderBy(x => DateTime.ParseExact(x.Date, "dd/MM/yyyy", null))
                .ToList();

            return Json(data, JsonRequestBehavior.AllowGet);
        }


        public JsonResult TopProducts()
        {
            var data = _entities.OrderDetails.Where(od => od.Order.Status == "Completed")
                .GroupBy(od => od.ProductVariant.Product.ProductName)
                .Select(g => new {
                    Name = g.Key,
                    Qty = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.Quantity * x.Price * (1 - (x.DiscountPercent ?? 0) / 100))
                })
                .OrderByDescending(x => x.Qty).Take(5).ToList();
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public JsonResult TopCustomers()
        {
            var data = _entities.Orders.Where(o => o.Status == "Completed")
                .GroupBy(o => o.Customer.UserName)
                .Select(g => new {
                    Name = g.Key,
                    Total = g.Sum(x => x.TotalAmount)
                })
                .OrderByDescending(x => x.Total).Take(5).ToList();
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _entities.Dispose();
            base.Dispose(disposing);
        }

    }
}