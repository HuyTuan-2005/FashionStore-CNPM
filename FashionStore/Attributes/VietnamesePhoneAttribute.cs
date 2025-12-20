using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace FashionStore.Attributes
{
    /// <summary>
    /// Custom validation attribute cho số điện thoại Việt Nam
    /// </summary>
    public class VietnamesePhoneAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return false; // Required validation sẽ xử lý
            }

            var phoneNumber = value.ToString().Trim();
            
            // Loại bỏ khoảng trắng, dấu gạch ngang, dấu ngoặc
            phoneNumber = Regex.Replace(phoneNumber, @"[\s\-\(\)]", "");

            // Pattern: Bắt đầu bằng 0 hoặc +84, sau đó là 9 chữ số
            var pattern = @"^(0|\+84)[1-9][0-9]{8,9}$";
            return Regex.IsMatch(phoneNumber, pattern);
        }

        public override string FormatErrorMessage(string name)
        {
            return $"Số điện thoại không đúng định dạng. Ví dụ: 0912345678 hoặc +84912345678";
        }
    }
}

