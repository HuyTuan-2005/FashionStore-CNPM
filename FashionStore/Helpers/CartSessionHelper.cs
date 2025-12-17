using FashionStore.ViewModels;
using System.Collections.Generic;
using System.Linq;
using FashionStore.Models;
using System.Web;

namespace FashionStore.Helpers
{
    public class CartSessionHelper
    {
        private readonly FashionStoreEntities db = new FashionStoreEntities();
        public List<CartItemViewModel> Cart
        {
            get
            {
                var cart = HttpContext.Current.Session["Cart"] as List<CartItemViewModel>;
                if (cart == null)
                {
                    cart = new List<CartItemViewModel>();
                    HttpContext.Current.Session["Cart"] = cart;
                }

                return cart;
            }
        }

        public List<ProductVariant> ProductsInCart
        {
            get
            {
                var ids = Cart.Select(p => p.VariantID).ToList();
                return db.ProductVariants.Where(x => ids.Contains(x.VariantID)).ToList();
            }
        }
    }
}