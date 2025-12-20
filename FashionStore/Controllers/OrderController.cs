using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using FashionStore.Models;
using EntityState = System.Data.Entity.EntityState;

namespace FashionStore.Controllers
{
    [CustomAuthorize]
    public class OrderController : Controller
    {
        private FashionStoreEntities db = new FashionStoreEntities();

        private int CustomerID
        {
            get
            {
                return Session["Customer"] == null ? 0 : (Session["Customer"] as Customer).CustomerID;
            }
        }

        // GET: Order
        public ActionResult Index(string status)
        {
           
            //var orders = db.Orders.Where(x => x.CustomerID == CustomerID).Include(o => o.Customer);
            //return View(orders.OrderByDescending(x=>x.OrderID).ToList());

            var orders = db.Orders
              .Where(x => x.CustomerID == CustomerID);
            //Lọc theo status
            if (!string.IsNullOrEmpty(status))
            {
                orders = orders.Where(x => x.Status == status);
            }
            //Làm đầy đủ để tránh N + 1 query 
            var result = orders
               .Include(o => o.Customer)
               .Include(o => o.OrderDetails.Select(od => od.ProductVariant))
               .Include(o => o.OrderDetails.Select(od => od.ProductVariant.Product))
               .Include(o => o.OrderDetails.Select(od => od.ProductVariant.Product.ProductImages))
               .Include(o => o.OrderDetails.Select(od => od.ProductVariant.Color))
               .Include(o => o.OrderDetails.Select(od => od.ProductVariant.Size))
               .OrderByDescending(x => x.OrderID)
               .ToList();
            ViewBag.CurrentStatus = status;
            return View(result);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
