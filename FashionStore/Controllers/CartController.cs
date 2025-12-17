using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using FashionStore.Models;
using FashionStore.ViewModels;


namespace FashionStore.Controllers
{
    [CustomAuthorize]
    public class CartController : Controller
    {
        private readonly FashionStoreEntities db = new FashionStoreEntities();

        private List<CartItemViewModel> Cart
        {
            get
            {
                var cart = Session["Cart"] as List<CartItemViewModel>;
                if (cart == null)
                {
                    cart = new List<CartItemViewModel>();
                    Session["Cart"] = cart;
                }

                return cart;
            }
        }


        private List<ProductVariant> ProductsInCart
        {
            get
            {
                var ids = Cart.Select(p => p.VariantID).ToList();
                return db.ProductVariants.Where(x => ids.Contains(x.VariantID)).ToList();
            }
        }

        
        private bool RemoveProductInCart(int? id)
        {
            if (!id.HasValue) return false;
            var item = Cart.FirstOrDefault(x => x.VariantID == id.Value);

            if (item != null)
            {
                Cart.Remove(item);
                return true;
            }
            else
            {
                return false;
            }
        }
        // SHOW CART
        public ActionResult Index()
        {
            return View(ProductsInCart);
        }

        // ADD TO CART
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddToCart(CartItemViewModel cart, int ColorId, int SizeId)
        {
            if (cart == null || cart.ProductID <= 0 || cart.Quantity <= 0)
            {
                TempData["Error"] = "Dữ liệu giỏ hàng không hợp lệ.";
                return RedirectToAction("Details", "Product", new { id = cart?.ProductID });
            }

            
            // 2. Lấy sản phẩm
            var product = db.Products
                .FirstOrDefault(p => p.ProductID == cart.ProductID && p.IsActive == true);

            if (product == null)
            {
                return HttpNotFound();
            }

            // 3. Tìm biến thể (variant) theo màu + size
            var variant = product.ProductVariants
                .FirstOrDefault(v => v.ColorID == ColorId && v.SizeID == SizeId);

            if (variant == null)
            {
                TempData["Error"] = "Biến thể sản phẩm không tồn tại hoặc đã ngừng bán.";
                return RedirectToAction("Details", "Product", new { id = cart.ProductID });
            }

            if (variant.Stock < cart.Quantity)
            {
                TempData["Error"] = $"Chỉ còn {variant.Stock} sản phẩm trong kho.";
                return RedirectToAction("Details", "Product", new { id = cart.ProductID });
            }

            var cartSession = Session["Cart"] as List<CartItemViewModel> ?? new List<CartItemViewModel>();

            var existing = cartSession.FirstOrDefault(c => c.VariantID == variant.VariantID);

            if (existing != null)
            {
                // Cộng dồn số lượng, có thể check lại tồn kho
                var newQuantity = existing.Quantity + cart.Quantity;
                if (newQuantity > variant.Stock)
                {
                    TempData["Error"] = $"Bạn chỉ có thể mua tối đa {variant.Stock} sản phẩm này.";
                    return RedirectToAction("Details", "Product", new { id = cart.ProductID });
                }

                existing.Quantity = newQuantity;
            }
            else
            {
                // Thêm mới
                cartSession.Add(new CartItemViewModel
                {
                    ProductID = cart.ProductID,
                    VariantID = variant.VariantID,
                    Quantity = cart.Quantity
                });
            }

            // 7. Lưu lại vào Session
            Session["Cart"] = cartSession;

            TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng.";
            return RedirectToAction("Index", "Product");
        }


        // REMOVE ITEM
        public ActionResult Remove(int? id)
        {
            var result = RemoveProductInCart(id);

            if (result)
            {
                TempData["Success"] = $"Đã xóa sản phẩm khỏi giỏ hàng.";
            }
            
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Checkout()
        {
            if (Cart.Count <= 0)
            {
                TempData["Error"] = "Bạn chưa có sản phẩm nào trong giỏ hàng";
                return RedirectToAction("Index");
            }
            
            var cus = Session["Customer"] as Customer ?? new Customer();
            var profile = db.CustomerProfiles.Find(cus.CustomerID);
            
            return View(profile);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(CustomerProfile model)
        {
            var cart = Cart;
            if (cart.Count <= 0)
            {
                TempData["Error"] = "Bạn chưa có sản phẩm nào trong giỏ hàng";
                return RedirectToAction("Index");
            }
            var lstProduct = ProductsInCart;
            
            var cus = Session["Customer"] as Customer;
            
            var order = new Order();
            
            order.PaymentMethod = "COD";
            order.ShippingAddress = string.Join(", ", model.Address, model.District, model.City);
            order.OrderDate = DateTime.Now;
            order.Status = "Pending";
            order.CustomerID = cus != null ? cus.CustomerID : throw new Exception("Thông tin người dùng không hợp lệ");
            order.PhoneNumber = model.PhoneNumber;
            order.FullName = model.FullName;


            foreach (var product in lstProduct)
            {
                var cartProduct = cart.FirstOrDefault(c => c.VariantID == product.VariantID);

                if (cartProduct.Quantity > product.Stock)
                {
                    TempData["Error"] = $"Sản phẩm {product.Product.ProductName} trong giỏ hàng của bạn đã hết.";
                    RemoveProductInCart(cartProduct.VariantID);
                    return RedirectToAction("Index", "Cart");
                }
                
                order.OrderDetails.Add(new OrderDetail()
                {
                    Price = product.Product.BasePrice,
                    VariantID = product.VariantID,
                    Quantity = cartProduct != null ? cartProduct.Quantity : throw new Exception("Số lượng đặt hàng không hợp lệ"),
                    DiscountPercent = DateTime.Now.Day == DateTime.Now.Month ? 30 : 0
                });
                product.Stock -= cartProduct.Quantity;
            }
            
            order.TotalAmount = order.OrderDetails.Sum(x => (x.Price * (1 - x.DiscountPercent.Value / 100)) * x.Quantity);
            
            db.Orders.Add(order);
            db.SaveChanges();
            
            Session["Cart"] = null;
            TempData["Success"] = $"Đặt hàng thành công.";
            return RedirectToAction("Index", "Order");
        }
        
        // CLEAR CART
        public ActionResult Clear()
        {
            Session["Cart"] = null;
            return RedirectToAction("Index");
        }
    }
}