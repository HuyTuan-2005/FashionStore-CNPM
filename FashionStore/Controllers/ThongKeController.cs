using FashionStore.Models;
using System.Linq;
using System.Web.Mvc;

namespace FashionStore.Controllers
{
    public class ThongKeController : Controller
    {
        private FashionStoreEntities db = new FashionStoreEntities();

        public ActionResult Index()
        {
            // 1. Lấy thông tin khách hàng từ Session
            var user = Session["Customer"] as Customer;
            if (user == null) return RedirectToAction("Login", "Account");

            // 2. Lấy danh sách đơn hàng của khách này
            var orders = db.Orders.Where(o => o.CustomerID == user.CustomerID).ToList();

            // 3. Tính toán dựa trên giá trị String trong database
            ViewBag.TotalOrders = orders.Count;

            // Cộng tiền những đơn đã hoàn thành (Completed)
            ViewBag.TotalSpent = orders.Where(o => o.Status == "Completed")
                           .Sum(o => (decimal?)o.TotalAmount) ?? 0;

            ViewBag.Completed = orders.Count(o => o.Status == "Completed");

            // Đếm các đơn đang xử lý hoặc đang giao (trừ Completed và Cancelled)
            ViewBag.Shipping = orders.Count(o => o.Status != "Completed" && o.Status != "Cancelled");

            return View(orders.OrderByDescending(o => o.OrderID).ToList());
        }
        public ActionResult Details(int id)
        {
            var user = Session["Customer"] as Customer;
            var order = db.Orders
                          .Include("OrderDetails.ProductVariant.Product")
                          .Include("OrderDetails.ProductVariant.Size")  // Phải có dòng này
                          .Include("OrderDetails.ProductVariant.Color") // Phải có dòng này
                          .FirstOrDefault(o => o.OrderID == id && o.CustomerID == user.CustomerID);

            if (order == null) return HttpNotFound();
            return View(order);
        }
    }
}