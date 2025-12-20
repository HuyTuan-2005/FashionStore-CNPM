using System;

namespace FashionStore.Services
{
    public class ShippingService
    {
        // Địa chỉ kho hàng (có thể config sau)
        private const string WAREHOUSE_CITY = "Thành phố Hồ Chí Minh";

        // Phí giao hàng (có thể config sau)
        private const decimal INTRACITY_FEE = 20000m;  // Nội tỉnh: 20.000 VNĐ
        private const decimal INTERCITY_FEE = 30000m;  // Liên tỉnh: 30.000 VNĐ

        /// <summary>
        /// Tính phí giao hàng dựa trên tỉnh/thành phố người nhận
        /// </summary>
        /// <param name="recipientCity">Tỉnh/thành phố người nhận</param>
        /// <returns>Phí giao hàng</returns>
        public decimal CalculateShippingFee(string recipientCity)
        {
            if (string.IsNullOrWhiteSpace(recipientCity))
                return INTERCITY_FEE; // Mặc định liên tỉnh nếu không có thông tin

            // Chuẩn hóa tên tỉnh/thành phố để so sánh
            var normalizedRecipient = NormalizeCityName(recipientCity);
            var normalizedWarehouse = NormalizeCityName(WAREHOUSE_CITY);

            // So sánh (case-insensitive)
            if (string.Equals(normalizedRecipient, normalizedWarehouse, StringComparison.OrdinalIgnoreCase))
            {
                return INTRACITY_FEE; // Nội tỉnh
            }

            return INTERCITY_FEE; // Liên tỉnh
        }

        /// <summary>
        /// Chuẩn hóa tên tỉnh/thành phố để so sánh
        /// Loại bỏ các từ như "Tỉnh", "Thành phố", "TP.", etc.
        /// </summary>
        private string NormalizeCityName(string cityName)
        {
            if (string.IsNullOrWhiteSpace(cityName))
                return string.Empty;

            var normalized = cityName.Trim();

            // Loại bỏ các prefix phổ biến
            var prefixes = new[] { "Tỉnh ", "Thành phố ", "TP. ", "TP ", "Tp. ", "Tp " };
            foreach (var prefix in prefixes)
            {
                if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    normalized = normalized.Substring(prefix.Length).Trim();
                    break;
                }
            }

            return normalized;
        }

        /// <summary>
        /// Lấy mô tả phương thức giao hàng
        /// </summary>
        public string GetShippingMethodDescription()
        {
            return "Giao hàng trong 3-5 ngày làm việc";
        }
    }
}

