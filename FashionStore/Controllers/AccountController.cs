using System;
using FashionStore.Helpers;
using FashionStore.Models;
using System.Linq;
using System.Web.Mvc;

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

        public ActionResult Login2(int id = 1)
        {
            // demo đăng nhập
            Session["Customer"] = _entities.Customers.FirstOrDefault(x => x.CustomerID == id);
            return RedirectToAction("Index", "Home");
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
                    Session["Customer"] = cust;
                    TempData["Success"] = $"Đăng nhập thành công.";

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
    }
}