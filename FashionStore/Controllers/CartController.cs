using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Web;
using System.Web.Mvc;
using FashionStore.Models;
using FashionStore.ViewModels;

namespace FashionStore.Controllers
{

    public class CartController : Controller
    {
        private readonly FashionStoreEntities db = new FashionStoreEntities();

  
        private int CustomerID
        {
            get
            {
                var customer = Session["Customer"] as Customer;
                return customer?.CustomerID ?? 0;
            }
        }

     
        private Cart GetOrCreateCart()
        {
            var customer = Session["Customer"] as Customer;

            if (customer != null)
            {
                // USER ĐÃ LOGIN → Tìm theo CustomerID
                var cart = db.Carts.FirstOrDefault(c => c.CustomerID == customer.CustomerID);

                if (cart == null)
                {
                    cart = new Cart
                    {
                        CustomerID = customer.CustomerID,
                        CartToken = Guid.NewGuid().ToString(),
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    db.Carts.Add(cart);
                    db.SaveChanges();
                }

                return cart;
            }
            else
            {
                // GUEST → Tìm theo CartToken trong Cookie
                var cartToken = Request.Cookies["CartToken"]?.Value;

                if (!string.IsNullOrEmpty(cartToken))
                {
                    var cart = db.Carts.FirstOrDefault(c =>
                        c.CartToken == cartToken &&
                        c.CustomerID == null);

                    if (cart != null)
                    {
                        return cart;
                    }
                }

                // TẠO CART MỚI CHO GUEST
                var newToken = Guid.NewGuid().ToString();
                var newCart = new Cart
                {
                    CustomerID = null,  // NULL cho guest
                    CartToken = newToken,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                db.Carts.Add(newCart);
                db.SaveChanges();

                // LƯU TOKEN VÀO COOKIE (30 ngày)
                var cookie = new HttpCookie("CartToken", newToken)
                {
                    Expires = DateTime.Now.AddDays(30),
                    HttpOnly = true, // Bảo mật
                    Secure = false   // Đổi thành true nếu dùng HTTPS
                };
                Response.Cookies.Add(cookie);

                return newCart;
            }
        }

      
        public ActionResult Index()
        {
            var cart = GetOrCreateCart();

            var cartItems = db.CartItems
                .Where(ci => ci.CartID == cart.CartID)
                .Include(ci => ci.ProductVariant.Product)
                .Include(ci => ci.ProductVariant.Product.ProductImages)
                .Include(ci => ci.ProductVariant.Color)
                .Include(ci => ci.ProductVariant.Size)
                .ToList();

            // CẬP NHẬT SESSION ĐỂ NAVBAR HIỂN THỊ BADGE
            UpdateCartSession();

            // TRẢ VỀ LIST PRODUCTVARIANT ĐỂ TƯƠNG THÍCH VỚI VIEW HIỆN TẠI
            return View(cartItems.Select(ci => ci.ProductVariant).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddToCart(CartItemViewModel cartModel, int ColorId, int SizeId)
        {
            if (cartModel == null || cartModel.ProductID <= 0 || cartModel.Quantity <= 0)
            {
                TempData["Error"] = "Dữ liệu giỏ hàng không hợp lệ.";
                return RedirectToAction("Details", "Product", new { id = cartModel?.ProductID });
            }

            // LẤY SẢN PHẨM
            var product = db.Products
                .FirstOrDefault(p => p.ProductID == cartModel.ProductID && p.IsActive == true);

            if (product == null)
            {
                return HttpNotFound();
            }

            // TÌM VARIANT
            var variant = product.ProductVariants
                .FirstOrDefault(v => v.ColorID == ColorId && v.SizeID == SizeId);

            if (variant == null)
            {
                TempData["Error"] = "Biến thể sản phẩm không tồn tại.";
                return RedirectToAction("Details", "Product", new { id = cartModel.ProductID });
            }

            if (variant.Stock < cartModel.Quantity)
            {
                TempData["Error"] = $"Chỉ còn {variant.Stock} sản phẩm trong kho.";
                return RedirectToAction("Details", "Product", new { id = cartModel.ProductID });
            }

            // LẤY HOẶC TẠO CART (GUEST HOẶC USER)
            var cart = GetOrCreateCart();

            // TÌM ITEM ĐÃ CÓ TRONG GIỎ
            var existingItem = db.CartItems.FirstOrDefault(ci =>
                ci.CartID == cart.CartID &&
                ci.VariantID == variant.VariantID);

            if (existingItem != null)
            {
                // CẬP NHẬT SỐ LƯỢNG
                var newQuantity = existingItem.Quantity + cartModel.Quantity;

                if (newQuantity > variant.Stock)
                {
                    TempData["Error"] = $"Bạn chỉ có thể mua tối đa {variant.Stock} sản phẩm này.";
                    return RedirectToAction("Details", "Product", new { id = cartModel.ProductID });
                }

                existingItem.Quantity = newQuantity;
            }
            else
            {
                // THÊM MỚI
                db.CartItems.Add(new CartItem
                {
                    CartID = cart.CartID,
                    VariantID = variant.VariantID,
                    Quantity = cartModel.Quantity,
                    AddedAt = DateTime.Now
                });
            }

            cart.UpdatedAt = DateTime.Now;
            db.SaveChanges();

            // CẬP NHẬT SESSION
            UpdateCartSession();

            TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng.";
            return RedirectToAction("Index", "Product");
        }

        //Xoá sản phẩm khỏi giỏ hàng
        public ActionResult Remove(int? id)
        {
            if (!id.HasValue)
            {
                return RedirectToAction("Index");
            }

            var cart = GetOrCreateCart();

            var cartItem = db.CartItems.FirstOrDefault(ci =>
                ci.CartID == cart.CartID &&
                ci.VariantID == id.Value);

            if (cartItem != null)
            {
                db.CartItems.Remove(cartItem);
                cart.UpdatedAt = DateTime.Now;
                db.SaveChanges();

                UpdateCartSession();
                TempData["Success"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
            }

            return RedirectToAction("Index");
        }

      //Update số lượng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateQuantity(int variantId, int quantity)
        {
            try
            {
                if (quantity <= 0)
                {
                    return Json(new { success = false, message = "Số lượng phải lớn hơn 0." });
                }

                var cart = GetOrCreateCart();

                var cartItem = db.CartItems.FirstOrDefault(ci =>
                    ci.CartID == cart.CartID &&
                    ci.VariantID == variantId);

                if (cartItem == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không có trong giỏ hàng." });
                }

                var variant = db.ProductVariants
                    .Include(v => v.Product)
                    .FirstOrDefault(v => v.VariantID == variantId);

                if (variant == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại." });
                }

                if (quantity > variant.Stock)
                {
                    return Json(new { success = false, message = $"Chỉ còn {variant.Stock} sản phẩm trong kho." });
                }

                cartItem.Quantity = quantity;
                cart.UpdatedAt = DateTime.Now;
                db.SaveChanges();

                var price = variant.Product.BasePrice;
                var itemTotal = price * quantity;

                // TÍNH TỔNG TIỀN TOÀN BỘ GIỎ
                decimal grandTotal = 0;
                var allItems = db.CartItems
                    .Where(ci => ci.CartID == cart.CartID)
                    .Include(ci => ci.ProductVariant.Product)
                    .ToList();

                foreach (var item in allItems)
                {
                    grandTotal += item.ProductVariant.Product.BasePrice * item.Quantity;
                }

                UpdateCartSession();

                return Json(new
                {
                    success = true,
                    itemTotal = itemTotal.ToString("N0"),
                    grandTotal = grandTotal.ToString("N0")
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại." });
            }
        }

        [HttpGet]
        [CustomAuthorize] // ← CHỈ CHECKOUT MỚI CẦN LOGIN
        public ActionResult Checkout()
        {
            var cart = GetOrCreateCart();

            var cartItemCount = db.CartItems.Count(ci => ci.CartID == cart.CartID);

            if (cartItemCount == 0)
            {
                TempData["Error"] = "Bạn chưa có sản phẩm nào trong giỏ hàng";
                return RedirectToAction("Index");
            }

            var cus = Session["Customer"] as Customer;
            var profile = db.CustomerProfiles.Find(cus.CustomerID);

            return View(profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize] // ← CHỈ CHECKOUT MỚI CẦN LOGIN
        public ActionResult Checkout(CustomerProfile model)
        {
            var cart = GetOrCreateCart();
            var cartItems = db.CartItems
                .Where(ci => ci.CartID == cart.CartID)
                .Include(ci => ci.ProductVariant.Product)
                .ToList();

            if (cartItems.Count == 0)
            {
                TempData["Error"] = "Bạn chưa có sản phẩm nào trong giỏ hàng";
                return RedirectToAction("Index");
            }

            var cus = Session["Customer"] as Customer;

            var order = new Order
            {
                PaymentMethod = "COD",
                ShippingAddress = string.Join(", ", model.Address, model.District, model.City),
                OrderDate = DateTime.Now,
                Status = "Pending",
                CustomerID = cus.CustomerID,
                PhoneNumber = model.PhoneNumber,
                FullName = model.FullName
            };

            foreach (var cartItem in cartItems)
            {
                var variant = cartItem.ProductVariant;

                if (cartItem.Quantity > variant.Stock)
                {
                    TempData["Error"] = $"Sản phẩm {variant.Product.ProductName} không đủ số lượng.";
                    return RedirectToAction("Index", "Cart");
                }

                order.OrderDetails.Add(new OrderDetail
                {
                    Price = variant.Product.BasePrice,
                    VariantID = variant.VariantID,
                    Quantity = cartItem.Quantity,
                    DiscountPercent = DateTime.Now.Day == DateTime.Now.Month ? 30 : 0
                });

                variant.Stock -= cartItem.Quantity;
            }

            order.TotalAmount = order.OrderDetails.Sum(x => (x.Price * (1 - x.DiscountPercent.Value / 100)) * x.Quantity);

            db.Orders.Add(order);

            // XÓA GIỎ HÀNG SAU KHI ĐẶT HÀNG THÀNH CÔNG
            db.CartItems.RemoveRange(cartItems);
            db.SaveChanges();

            Session["Cart"] = null;
            TempData["Success"] = "Đặt hàng thành công.";
            return RedirectToAction("Index", "Order");
        }

        // ============================================
        // XÓA TOÀN BỘ GIỎ HÀNG
        // ============================================
        public ActionResult Clear()
        {
            var cart = GetOrCreateCart();
            var cartItems = db.CartItems.Where(ci => ci.CartID == cart.CartID).ToList();
            db.CartItems.RemoveRange(cartItems);
            db.SaveChanges();

            Session["Cart"] = null;
            return RedirectToAction("Index");
        }

        // ============================================
        // HELPER: CẬP NHẬT SESSION (ĐỂ NAVBAR HIỂN THỊ BADGE)
        // ============================================
        private void UpdateCartSession()
        {
            var cart = GetOrCreateCart();

            var cartItems = db.CartItems
                .Where(ci => ci.CartID == cart.CartID)
                .Include(ci => ci.ProductVariant)
                .ToList();

            var viewModel = cartItems.Select(ci => new CartItemViewModel
            {
                ProductID = ci.ProductVariant.ProductID,
                VariantID = ci.VariantID,
                Quantity = ci.Quantity
            }).ToList();

            Session["Cart"] = viewModel;
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
