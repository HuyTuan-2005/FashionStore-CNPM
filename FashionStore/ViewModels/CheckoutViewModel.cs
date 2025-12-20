using FashionStore.Models;

namespace FashionStore.ViewModels
{
    public class CheckoutViewModel
    {
        public CustomerProfile CustomerProfile { get; set; }
        public string PaymentMethod { get; set; }
    }
}

