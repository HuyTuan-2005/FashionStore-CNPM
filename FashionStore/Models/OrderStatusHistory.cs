using System;

namespace FashionStore.Models
{
    public partial class OrderStatusHistory
    {
        public int HistoryID { get; set; }
        public int OrderID { get; set; }
        public string OldStatus { get; set; }
        public string NewStatus { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
        public string Note { get; set; }

        public virtual Order Order { get; set; }
    }
}

