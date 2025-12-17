using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FashionStore.ViewModels
{
    public class CartItemViewModel
    {
        public int ProductID { get; set; }
        public int VariantID { get; set; }
        public int Quantity { get; set; }
    }
}
