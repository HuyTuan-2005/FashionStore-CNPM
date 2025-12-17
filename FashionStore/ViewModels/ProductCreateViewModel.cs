using System;
using System.Collections.Generic;
using System.Web;
using FashionStore.Models;

namespace FashionStore.ViewModels
{
    public class ProductCreateViewModel
    {
        public string ProductName { get; set; }
        public string Description { get; set; }
        public decimal BasePrice { get; set; }
        public Nullable<decimal> DiscountPercent { get; set; }
        public Nullable<int> CategoryID { get; set; }
        public Nullable<bool> IsActive { get; set; }
        public Nullable<System.DateTime> CreatedAt { get; set; }
        public List<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
        public HttpPostedFileBase MainImage { get; set; }
        public List<HttpPostedFileBase> AdditionalImages { get; set; }
        
        
    }
}