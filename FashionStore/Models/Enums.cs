namespace FashionStore.Models
{
    public enum PaymentMethod
    {
        COD = 1,
        BankTransfer = 2,
        EWallet = 3
    }

    public enum PaymentStatus
    {
        Unpaid = 1,
        Paid = 2,
        Failed = 3,
        Refunded = 4
    }

    public enum OrderStatus
    {
        Pending = 1,
        Processing = 2,
        Shipped = 3,
        Completed = 4,
        Cancelled = 5
    }

    public enum ShippingMethod
    {
        Standard = 1
    }
}

