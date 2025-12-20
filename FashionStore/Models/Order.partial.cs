using System.ComponentModel.DataAnnotations.Schema;

namespace FashionStore.Models
{
    public partial class Order
    {
        [NotMapped]
        public PaymentMethod? PaymentMethodEnum
        {
            get
            {
                if (string.IsNullOrEmpty(PaymentMethod))
                    return null;
                if (System.Enum.TryParse<PaymentMethod>(PaymentMethod, true, out var result))
                    return result;
                return null;
            }
            set
            {
                PaymentMethod = value?.ToString();
            }
        }

        [NotMapped]
        public OrderStatus? StatusEnum
        {
            get
            {
                if (string.IsNullOrEmpty(Status))
                    return null;
                if (System.Enum.TryParse<OrderStatus>(Status, true, out var result))
                    return result;
                return null;
            }
            set
            {
                Status = value?.ToString();
            }
        }

        // PaymentStatus property - will be available after running migration and updating EDMX
        // For now, using reflection or direct SQL if needed
        // After EDMX update, this can be removed as it will be in the generated model
    }
}

