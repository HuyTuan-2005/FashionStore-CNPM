using FashionStore.Helpers;
using FashionStore.Models;
using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace FashionStore.Controllers
{
    public class ForgotPasswordController : Controller
    {
        private readonly FashionStoreEntities db = new FashionStoreEntities();

        // ================== VIEW ==================
        public ActionResult Index()
        {
            return View();
        }

        // ================== 1. Sinh OTP ==================
        private string GenerateOTP()
        {
            return new Random().Next(100000, 999999).ToString();
        }

        // ================== 2. Gửi OTP qua EMAIL ==================
        private bool SendOTPToEmail(string email, string otp)
        {
            try
            {
                var mail = new MailMessage();
                mail.To.Add(email);
                mail.Subject = "Mã OTP đặt lại mật khẩu";
                mail.Body = $@"
                            Xin chào,

                            Mã OTP của bạn là: {otp}

                            ⏰ Hiệu lực trong 5 phút.
                            Nếu bạn không yêu cầu, hãy bỏ qua email này.    

                            FashionStore
                            ";
                mail.IsBodyHtml = false;

                using (var smtp = new SmtpClient())
                {
                    smtp.Send(mail);
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return false;
            }
        }

        // ================== 3. Gửi OTP ==================
        [HttpPost]
        public ActionResult SendOTP(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new { success = false, message = "Vui lòng nhập email" });
            }

            var cust = db.Customers.FirstOrDefault(x => x.Email == email);
            if (cust == null)
            {
                return Json(new { success = false, message = "Email không tồn tại" });
            }

            string otp = GenerateOTP();

            // Lưu session
            Session["OTP"] = otp;
            Session["OTP_EXPIRE"] = DateTime.Now.AddMinutes(5);
            Session["OTP_CUSTOMER_ID"] = cust.CustomerID;
            bool sent = SendOTPToEmail(email, otp);
            if (!sent)
            {
                return Json(new { success = false, message = "Gửi email thất bại" });
            }

            return Json(new { success = true });
        }

        // ================== 4. Xác thực OTP ==================
        [HttpPost]
        public ActionResult VerifyOTP(string otp)
        {
            if (Session["OTP"] == null || Session["OTP_EXPIRE"] == null)
            {
                return Json(new { success = false, message = "OTP không tồn tại" });
            }

            if (DateTime.Now > (DateTime)Session["OTP_EXPIRE"])
            {
                return Json(new { success = false, message = "OTP đã hết hạn" });
            }

            if (Session["OTP"].ToString() != otp)
            {
                return Json(new { success = false, message = "OTP không đúng" });
            }

            return Json(new { success = true });
        }

        // ================== 5. Đặt lại mật khẩu ==================
        [HttpPost]
        public ActionResult ResetPassword(string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                return Json(new { success = false, message = "Mật khẩu không hợp lệ" });
            }

            if (Session["OTP_CUSTOMER_ID"] == null)
            {
                return Json(new { success = false, message = "Phiên làm việc hết hạn" });
            }

            int customerId = (int)Session["OTP_CUSTOMER_ID"];
            var customer = db.Customers.FirstOrDefault(x => x.CustomerID == customerId);

            if (customer == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tài khoản" });
            }

            customer.PasswordHash = PasswordHasher.HashPassword(newPassword);
            db.SaveChanges();

            Session.Clear();

            return Json(new { success = true });
        }
    }
}
