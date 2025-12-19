using FashionStore.Helpers;
using FashionStore.Models;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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
            Random rnd = new Random();
            return rnd.Next(100000, 999999).ToString();
        }

        // ================== 2. Chuẩn hóa SĐT để gửi SMS ==================
        // DB lưu: 0xxxxxxxxx
        // SpeedSMS cần: 84xxxxxxxxx
        private string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return null;

            phone = phone.Trim();

            if (phone.StartsWith("+"))
                phone = phone.Substring(1);

            if (phone.StartsWith("0"))
                phone = "84" + phone.Substring(1);

            if (!phone.StartsWith("84"))
                phone = "84" + phone;

            return phone;
        }

        // ================== 3. Gửi OTP qua SpeedSMS ==================
        private async Task<bool> SendOTPToPhone(string phone, string otp)
        {
            string apiToken = "cQsdrW74Bzsr3kOpGeJCB1ynr6PQMNrA"; // API access token

            string content = $"Ma OTP cua ban la {otp}. Hieu luc 5 phut.";

            using (var client = new HttpClient())
            {
                // BASIC AUTH: token:x
                var authValue = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes($"{apiToken}:x")
                );

                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", authValue);

                var data = new
                {
                    to = phone,
                    content = content,
                    sms_type = 3
                };

                var json = JsonConvert.SerializeObject(data);

                var response = await client.PostAsync(
                    "https://api.speedsms.vn/index.php/sms/send",
                    new StringContent(json, Encoding.UTF8, "application/json")
                );

                string result = await response.Content.ReadAsStringAsync();

                // DEBUG khi test
                System.Diagnostics.Debug.WriteLine(result);

                return response.IsSuccessStatusCode;
            }
        }


        // ================== 4. Gửi OTP ==================
        [HttpPost]
        public async Task<ActionResult> SendOTP(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return Json(new { success = false, message = "Vui lòng nhập số điện thoại" });
            }

            // 🔎 Tìm trong DB bằng số gốc (0xxxxxxxx)
            var profile = db.CustomerProfiles.FirstOrDefault(x => x.PhoneNumber == phone);
            if (profile == null)
            {
                return Json(new { success = false, message = "Số điện thoại không tồn tại" });
            }

            string otp = GenerateOTP();
            string phone84 = NormalizePhone(phone);

            if (phone84 == null)
            {
                return Json(new { success = false, message = "Số điện thoại không hợp lệ" });
            }

            // Lưu session
            Session["OTP"] = otp;
            Session["OTP_EXPIRE"] = DateTime.Now.AddMinutes(5);
            Session["OTP_CUSTOMER_ID"] = profile.ProfileID;

            bool sent = await SendOTPToPhone(phone84, otp);
            if (!sent)
            {
                return Json(new { success = false, message = "Gửi OTP thất bại" });
            }

            return Json(new { success = true });
        }

        // ================== 5. Xác thực OTP ==================
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

        // ================== 6. Đặt lại mật khẩu ==================
        [HttpPost]
        public ActionResult ResetPassword(string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                return Json(new { success = false, message = "Mật khẩu không được để trống" });
            }

            if (Session["OTP_CUSTOMER_ID"] == null)
            {
                return Json(new { success = false, message = "Phiên làm việc đã hết hạn" });
            }

            int customerId = (int)Session["OTP_CUSTOMER_ID"];

            var customer = db.Customers.FirstOrDefault(x => x.CustomerID == customerId);
            if (customer == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tài khoản" });
            }

            customer.PasswordHash = PasswordHasher.HashPassword(newPassword);
            db.SaveChanges();

            // Clear session OTP
            Session.Remove("OTP");
            Session.Remove("OTP_EXPIRE");
            Session.Remove("OTP_CUSTOMER_ID");

            return Json(new { success = true });
        }
    }
}
