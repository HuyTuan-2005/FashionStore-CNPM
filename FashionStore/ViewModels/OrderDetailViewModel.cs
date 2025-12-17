using FashionStore.Models;

namespace FashionStore.ViewModels
{
    public class OrderDetailViewModel
    {
        public Order Order { get; set; }
    
        public decimal SubTotal { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal GrandTotal { get; set; }
    
        // Hiển thị status với màu sắc
        public string StatusBadgeClass { get; set; }
        public string StatusDisplayName { get; set; }
    }

}