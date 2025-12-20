using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FashionStore.Models;
using FashionStore.ViewModels;

namespace FashionStore.Services
{
    public class CheckoutValidationService
    {
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> Errors { get; set; }

            public ValidationResult()
            {
                Errors = new List<string>();
                IsValid = true;
            }

            public void AddError(string error)
            {
                Errors.Add(error);
                IsValid = false;
            }
        }

        /// <summary>
        /// Validate toàn bộ thông tin checkout
        /// </summary>
        public ValidationResult ValidateCheckout(CheckoutViewModel viewModel, List<CartItem> cartItems)
        {
            var result = new ValidationResult();

            // 1. Validate CustomerProfile
            if (viewModel.CustomerProfile == null)
            {
                result.AddError("Thông tin người nhận không được để trống.");
                return result; // Dừng lại nếu không có profile
            }

            var profile = viewModel.CustomerProfile;

            // 1.1. Validate FullName
            ValidateFullName(profile.FullName, result);

            // 1.2. Validate PhoneNumber
            ValidatePhoneNumber(profile.PhoneNumber, result);

            // 1.3. Validate ShippingAddress
            ValidateShippingAddress(profile, result);

            // 2. Validate ShippingMethod
            ValidateShippingMethod(viewModel.ShippingMethod, result);

            // 3. Validate ShippingFee
            ValidateShippingFee(viewModel.ShippingFee, result);

            // 4. Validate PaymentMethod
            ValidatePaymentMethod(viewModel.PaymentMethod, result);

            // 5. Validate Order (cart items, total amount)
            ValidateOrder(cartItems, viewModel, result);

            return result;
        }

        /// <summary>
        /// Validate FullName
        /// - Trim trước khi validate
        /// - Không được rỗng sau khi trim
        /// - Độ dài từ 2 đến 100 ký tự
        /// - Chỉ chứa chữ cái (có dấu), khoảng trắng, dấu '-' và '.'
        /// </summary>
        private void ValidateFullName(string fullName, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                result.AddError("Họ và tên không được để trống.");
                return;
            }

            var trimmed = fullName.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                result.AddError("Họ và tên không được để trống.");
                return;
            }

            if (trimmed.Length < 2)
            {
                result.AddError("Họ và tên phải có ít nhất 2 ký tự.");
                return;
            }

            if (trimmed.Length > 100)
            {
                result.AddError("Họ và tên không được vượt quá 100 ký tự.");
                return;
            }

            // Chỉ chứa chữ cái (có dấu), khoảng trắng, dấu '-' và '.'
            // Pattern: Cho phép chữ cái tiếng Việt (Unicode), khoảng trắng, dấu '-' và '.'
            var pattern = @"^[\p{L}\s\-\.]+$";
            if (!Regex.IsMatch(trimmed, pattern))
            {
                result.AddError("Họ và tên chỉ được chứa chữ cái, khoảng trắng, dấu '-' và '.'");
            }
        }

        /// <summary>
        /// Validate PhoneNumber - Định dạng số điện thoại Việt Nam
        /// - Bắt đầu bằng 0 hoặc +84
        /// - Có 10 hoặc 11 chữ số
        /// </summary>
        private void ValidatePhoneNumber(string phoneNumber, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                result.AddError("Số điện thoại không được để trống.");
                return;
            }

            var trimmed = phoneNumber.Trim();

            // Loại bỏ khoảng trắng, dấu gạch ngang, dấu ngoặc
            trimmed = Regex.Replace(trimmed, @"[\s\-\(\)]", "");

            // Pattern: Bắt đầu bằng 0 hoặc +84, sau đó là 9 chữ số
            // Hoặc: 10 chữ số bắt đầu bằng 0
            var pattern = @"^(0|\+84)[1-9][0-9]{8,9}$";
            if (!Regex.IsMatch(trimmed, pattern))
            {
                result.AddError("Số điện thoại không đúng định dạng. Ví dụ: 0912345678 hoặc +84912345678");
            }
        }

        /// <summary>
        /// Validate ShippingAddress
        /// - Phải có ít nhất 1 trong: Address, District, City
        /// </summary>
        private void ValidateShippingAddress(CustomerProfile profile, ValidationResult result)
        {
            var hasAddress = !string.IsNullOrWhiteSpace(profile.Address);
            var hasDistrict = !string.IsNullOrWhiteSpace(profile.District);
            var hasCity = !string.IsNullOrWhiteSpace(profile.City);

            if (!hasAddress && !hasDistrict && !hasCity)
            {
                result.AddError("Vui lòng nhập ít nhất một trong các thông tin: Địa chỉ, Quận/Huyện, hoặc Tỉnh/Thành phố.");
            }
        }

        /// <summary>
        /// Validate ShippingMethod
        /// - BẮT BUỘC phải có
        /// - Hiện tại chỉ chấp nhận "Standard"
        /// </summary>
        private void ValidateShippingMethod(string shippingMethod, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(shippingMethod))
            {
                result.AddError("Phương thức giao hàng không được để trống.");
                return;
            }

            if (shippingMethod != ShippingMethod.Standard.ToString())
            {
                result.AddError("Phương thức giao hàng không hợp lệ.");
            }
        }

        /// <summary>
        /// Validate ShippingFee
        /// - Bắt buộc
        /// - >= 0
        /// </summary>
        private void ValidateShippingFee(decimal shippingFee, ValidationResult result)
        {
            if (shippingFee < 0)
            {
                result.AddError("Phí vận chuyển không được âm.");
            }
        }

        /// <summary>
        /// Validate PaymentMethod
        /// - BẮT BUỘC
        /// - Hiện tại chỉ cho phép "COD"
        /// </summary>
        private void ValidatePaymentMethod(string paymentMethod, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                result.AddError("Phương thức thanh toán không được để trống.");
                return;
            }

            if (paymentMethod != PaymentMethod.COD.ToString())
            {
                result.AddError("Phương thức thanh toán không hợp lệ.");
            }
        }

        /// <summary>
        /// Validate Order
        /// - Đơn hàng phải có ít nhất 1 sản phẩm
        /// </summary>
        private void ValidateOrder(List<CartItem> cartItems, CheckoutViewModel viewModel, ValidationResult result)
        {
            if (cartItems == null || cartItems.Count == 0)
            {
                result.AddError("Giỏ hàng không có sản phẩm nào.");
                return;
            }

            // Validate TotalAmount = SubTotal - TotalDiscount + ShippingFee
            var expectedTotal = viewModel.SubTotal - viewModel.TotalDiscount + viewModel.ShippingFee;
            var tolerance = 0.01m; // Cho phép sai số 1 xu do làm tròn
            if (Math.Abs(viewModel.GrandTotal - expectedTotal) > tolerance)
            {
                result.AddError("Tổng tiền đơn hàng không khớp. Vui lòng làm mới trang và thử lại.");
            }
        }
    }
}

