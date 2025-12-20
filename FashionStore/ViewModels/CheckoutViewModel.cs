using System.ComponentModel.DataAnnotations;
using FashionStore.Models;

namespace FashionStore.ViewModels
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Thông tin người nhận không được để trống.")]
        public CustomerProfile CustomerProfile { get; set; }

        [Required(ErrorMessage = "Phương thức thanh toán không được để trống.")]
        public string PaymentMethod { get; set; }

        [Required(ErrorMessage = "Phương thức giao hàng không được để trống.")]
        public string ShippingMethod { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Phí vận chuyển không được âm.")]
        public decimal ShippingFee { get; set; }

        public decimal SubTotal { get; set; }
        public decimal TotalDiscount { get; set; }

        public decimal GrandTotal { get; set; }
    }
}

