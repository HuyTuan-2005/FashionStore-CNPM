using System.Collections.Generic;
using FashionStore.Models;

namespace FashionStore.ViewModels
{
    public class ProductDetailsViewModel
    {
        public Product Product { get; set; }
        public List<Product> RelatedProducts { get; set; }
    }
}