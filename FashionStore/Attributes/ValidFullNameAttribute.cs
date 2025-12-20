using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace FashionStore.Attributes
{
    /// <summary>
    /// Custom validation attribute cho họ và tên
    /// - Độ dài từ 2 đến 100 ký tự
    /// - Chỉ chứa chữ cái (có dấu), khoảng trắng, dấu '-' và '.'
    /// </summary>
    public class ValidFullNameAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return false; // Required validation sẽ xử lý
            }

            var fullName = value.ToString().Trim();

            if (fullName.Length < 2 || fullName.Length > 100)
            {
                return false;
            }

            // Chỉ chứa chữ cái (có dấu), khoảng trắng, dấu '-' và '.'
            var pattern = @"^[\p{L}\s\-\.]+$";
            return Regex.IsMatch(fullName, pattern);
        }

        public override string FormatErrorMessage(string name)
        {
            return "Họ và tên chỉ được chứa chữ cái, khoảng trắng, dấu '-' và '.' (2-100 ký tự)";
        }
    }
}

