using System;
using FashionStore.Helpers;
using FashionStore.Models;
using System.Linq;
using System.Web.Mvc;
using FashionStore.ViewModels;
using System.Data.Entity;
using System.Web;

namespace FashionStore.Controllers
{
    public class AccountController : Controller
    {
        // GET: Account
        private readonly FashionStoreEntities _entities = new FashionStoreEntities();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _entities.Dispose();
            }

            base.Dispose(disposing);
        }

        [CustomAuthorize]
        public ActionResult Index()
        {
            var cus = Session["Customer"] as Customer ?? new Customer();
            var profile = _entities.CustomerProfiles.Find(cus.CustomerID);

            return View(profile);
        }

        [CustomAuthorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(CustomerProfile model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            try
            {
                var profile = _entities.CustomerProfiles.Find(model.ProfileID);

                if (profile != null)
                {
                    profile.FullName = model.FullName;
                    profile.PhoneNumber = model.PhoneNumber;
                    profile.DateOfBirth = model.DateOfBirth;
                    profile.Gender = model.Gender;
                    profile.Address = model.Address;
                    profile.City = model.City;
                    profile.District = model.District;

                    _entities.SaveChanges();

                    TempData["Success"] = "Cập nhật thông tin thành công!";
                }
                else
                {
                    TempData["Error"] = "Không tìm thấy thông tin cá nhân!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi cập nhật: " + ex.Message;
                return View("Index", model);
            }

            return RedirectToAction("Index");
        }

        public ActionResult Login()
        {
            if (Session["Customer"] != null)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(Customer cus)
        {
            try
            {
                var cust = _entities.Customers.FirstOrDefault(us =>
                    (us.UserName == cus.UserName || us.Email == cus.UserName));
                if (cust != null && PasswordHasher.VerifyPassword(cus.PasswordHash, cust.PasswordHash))
                {
                    if (cust.IsActive == false)
                    {
                        ModelState.AddModelError("", "Tài khoản đã bị khoá");
                        return View("Login");
                    }

                    Session["CustomerUserName"] = cust.UserName;
                    Session["Customer"] = cust;
                    Session["RoleID"] = cust.RoleID.ToString();

                    MergeGuestCartToUserCart(cust.CustomerID);

                    if (cust.RoleID == 1 || cust.RoleID == 2)
                    {
                        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                    }

                    TempData["Success"] = "Đăng nhập thành công";
                    return RedirectToAction("Index", "Product");
                }


                else
                {
                    ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng");
                }

                return View(cus);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Lỗi đăng nhập: " + e.Message);
                return View(cus);
            }
        }

        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(Customer customer)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var cust = _entities.Customers.FirstOrDefault(us =>
                        us.UserName == customer.UserName || us.Email == customer.Email);
                    if (cust == null)
                    {
                        customer.PasswordHash = PasswordHasher.HashPassword(customer.PasswordHash);
                        customer.CreatedAt = DateTime.Now;
                        customer.IsActive = true;
                        customer.RoleID = 3;

                        customer.CustomerProfile = new CustomerProfile()
                        {
                            Address = "140 Lê Trọng Tấn",
                            City = "Hồ Chí Minh",
                            District = "Quận Tân Phú",
                            DateOfBirth = DateTime.Parse("1/1/1900"),
                            Gender = "Nam"
                        };

                        _entities.Customers.Add(customer);
                        _entities.SaveChanges();
                        
                        
                        TempData["Success"] = "Đăng ký tài khoản thành công.";
                        return RedirectToAction("Login");
                    }

                    if (_entities.Customers.FirstOrDefault(us => us.UserName == customer.UserName) != null)
                    {
                        ModelState.AddModelError("", "Username đã tồn tại");
                        return View(customer);
                    }

                    if (_entities.Customers.FirstOrDefault(us => us.Email == customer.Email) != null)
                    {
                        ModelState.AddModelError("", "Email đã tồn tại");
                        return View(customer);
                    }
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Đăng ký thất bại");
                ModelState.AddModelError("", "Lỗi đăng ký: " + e.Message);
                return View(customer);
            }

            return View(customer);
        }

        public ActionResult Logout()
        {
            Session.Abandon();
            return RedirectToAction("Login");
        }

        //Merge cart
        private void MergeGuestCartToUserCart(int customerID)
        {
            try
            {
                // LẤY CART TOKEN TỪ COOKIE
                var cartToken = Request.Cookies["CartToken"]?.Value;

                if (string.IsNullOrEmpty(cartToken))
                {
                    return; // Không có guest cart
                }

                // TÌM GUEST CART
                var guestCart = _entities.Carts.FirstOrDefault(c =>
                    c.CartToken == cartToken &&
                    c.CustomerID == null);

                if (guestCart == null)
                {
                    return; // Không có guest cart
                }

                // TÌM HOẶC TẠO USER CART
                var userCart = _entities.Carts.FirstOrDefault(c => c.CustomerID == customerID);

                if (userCart == null)
                {
                    // TẠO USER CART MỚI
                    userCart = new Cart
                    {
                        CustomerID = customerID,
                        CartToken = Guid.NewGuid().ToString(),
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    _entities.Carts.Add(userCart);
                    _entities.SaveChanges();
                }

                // LẤY TẤT CẢ ITEMS TỪ GUEST CART
                var guestItems = _entities.CartItems
                    .Where(ci => ci.CartID == guestCart.CartID)
                    .ToList();

                foreach (var guestItem in guestItems)
                {
                    // KIỂM TRA XEM USER CART ĐÃ CÓ VARIANT NÀY CHƯA
                    var existingItem = _entities.CartItems.FirstOrDefault(ci =>
                        ci.CartID == userCart.CartID &&
                        ci.VariantID == guestItem.VariantID);

                    if (existingItem != null)
                    {
                        // CỘNG DỒN SỐ LƯỢNG
                        existingItem.Quantity += guestItem.Quantity;
                    }
                    else
                    {
                        // THÊM MỚI VÀO USER CART
                        _entities.CartItems.Add(new CartItem
                        {
                            CartID = userCart.CartID,
                            VariantID = guestItem.VariantID,
                            Quantity = guestItem.Quantity,
                            AddedAt = DateTime.Now
                        });
                    }
                }

                // XÓA GUEST CART
                _entities.CartItems.RemoveRange(guestItems);
                _entities.Carts.Remove(guestCart);

                userCart.UpdatedAt = DateTime.Now;
                _entities.SaveChanges();

                // XÓA COOKIE
                if (Request.Cookies["CartToken"] != null)
                {
                    var cookie = new HttpCookie("CartToken")
                    {
                        Expires = DateTime.Now.AddDays(-1) // Xóa cookie
                    };
                    Response.Cookies.Add(cookie);
                }

                // CẬP NHẬT SESSION
                UpdateCartSessionInAccountController(userCart.CartID);
            }
            catch (Exception)
            {
                // Log error nhưng không làm gián đoạn login
            }
        }
        //Update Session
        private void UpdateCartSessionInAccountController(int cartID)
        {
            try
            {
                var cartItems = _entities.CartItems
                    .Where(ci => ci.CartID == cartID)
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
            catch (Exception)
            {
                // Ignore
            }
        }
    }
}