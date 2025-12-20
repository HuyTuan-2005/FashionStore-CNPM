using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using FashionStore.Models;
using FashionStore.Services;
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
        public ActionResult Index()
        {
            var customer = Session["Customer"] as Customer;
            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var currentCustomerID = customer.CustomerID;
            var orders = db.Orders
                .Where(x => x.CustomerID.HasValue && x.CustomerID.Value == currentCustomerID)
                .Include(o => o.Customer)
                .OrderByDescending(x => x.OrderID)
                .ToList();
            
            return View(orders);
        }

        // POST: Order/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Cancel(int id)
        {
            try
            {
                // Kiểm tra Session trước
                var customer = Session["Customer"] as Customer;
                if (customer == null)
                {
                    TempData["Error"] = "Bạn cần đăng nhập để thực hiện thao tác này.";
                    return RedirectToAction("Login", "Account");
                }

                var currentCustomerID = customer.CustomerID;

                // Load order với OrderDetails
                var order = db.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefault(o => o.OrderID == id);

                if (order == null)
                {
                    TempData["Error"] = "Đơn hàng không tồn tại.";
                    return RedirectToAction("Index");
                }

                // Kiểm tra quyền sở hữu đơn hàng - xử lý nullable CustomerID
                if (!order.CustomerID.HasValue || order.CustomerID.Value != currentCustomerID)
                {
                    TempData["Error"] = "Bạn không có quyền hủy đơn hàng này.";
                    return RedirectToAction("Index");
                }

                // Kiểm tra status - chỉ cho phép hủy khi Pending hoặc Processing
                var currentStatus = order.Status;
                if (currentStatus != OrderStatus.Pending.ToString() && 
                    currentStatus != OrderStatus.Processing.ToString())
                {
                    TempData["Error"] = "Chỉ có thể hủy đơn hàng khi đơn hàng đang ở trạng thái 'Chờ xác nhận' hoặc 'Đang xử lý'. Đơn hàng đã được giao không thể hủy.";
                    return RedirectToAction("Index");
                }

                // Validate status transition
                var orderService = new OrderService(db);
                if (!orderService.CanChangeStatus(currentStatus, OrderStatus.Cancelled.ToString()))
                {
                    TempData["Error"] = "Không thể hủy đơn hàng ở trạng thái hiện tại.";
                    return RedirectToAction("Index");
                }

                // Thực hiện hủy đơn hàng trong transaction
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // Update status
                        order.Status = OrderStatus.Cancelled.ToString();

                        // Restore stock và xử lý business logic
                        orderService.OnOrderCancelled(order);

                        // Save changes
                        db.SaveChanges();

                        // Log status change
                        orderService.LogStatusChange(
                            order.OrderID,
                            currentStatus,
                            OrderStatus.Cancelled.ToString(),
                            customer.UserName ?? "Customer",
                            "Khách hàng tự hủy đơn hàng"
                        );
                        db.SaveChanges();

                        // Commit transaction
                        transaction.Commit();

                        TempData["Success"] = "Đã hủy đơn hàng thành công. Số lượng sản phẩm đã được hoàn trả vào kho.";
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        TempData["Error"] = $"Có lỗi xảy ra khi hủy đơn hàng: {ex.Message}";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        //// GET: Order/Details/5
        //public ActionResult Details(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    Order order = db.Orders.Find(id);
        //    if (order == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    return View(order);
        //}

        //// GET: Order/Create
        //public ActionResult Create()
        //{
        //    ViewBag.CustomerID = new SelectList(db.Customers, "CustomerID", "UserName");
        //    return View();
        //}

        //// POST: Order/Create
        //// To protect from overposting attacks, enable the specific properties you want to bind to, for 
        //// more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Create([Bind(Include = "OrderID,CustomerID,OrderDate,Status,ShippingAddress,TotalAmount,PaymentMethod")] Order order)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        db.Orders.Add(order);
        //        db.SaveChanges();
        //        return RedirectToAction("Index");
        //    }

        //    ViewBag.CustomerID = new SelectList(db.Customers, "CustomerID", "UserName", order.CustomerID);
        //    return View(order);
        //}

        //// GET: Order/Edit/5
        //public ActionResult Edit(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    Order order = db.Orders.Find(id);
        //    if (order == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    ViewBag.CustomerID = new SelectList(db.Customers, "CustomerID", "UserName", order.CustomerID);
        //    return View(order);
        //}

        //// POST: Order/Edit/5
        //// To protect from overposting attacks, enable the specific properties you want to bind to, for 
        //// more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Edit([Bind(Include = "OrderID,CustomerID,OrderDate,Status,ShippingAddress,TotalAmount,PaymentMethod")] Order order)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        db.Entry(order).State = EntityState.Modified;
        //        db.SaveChanges();
        //        return RedirectToAction("Index");
        //    }
        //    ViewBag.CustomerID = new SelectList(db.Customers, "CustomerID", "UserName", order.CustomerID);
        //    return View(order);
        //}

        //// GET: Order/Delete/5
        //public ActionResult Delete(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    Order order = db.Orders.Find(id);
        //    if (order == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    return View(order);
        //}

        //// POST: Order/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public ActionResult DeleteConfirmed(int id)
        //{
        //    Order order = db.Orders.Find(id);
        //    db.Orders.Remove(order);
        //    db.SaveChanges();
        //    return RedirectToAction("Index");
        //}

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
