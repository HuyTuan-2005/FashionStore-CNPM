using FashionStore.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace FashionStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly FashionStoreEntities _entities = new FashionStoreEntities();
        public ActionResult Index()
        {
            return View(_entities.Products.Where(x => x.IsActive.Value).Take(6).ToList());
        }

        public ActionResult About()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _entities.Dispose();
            }
            base.Dispose(disposing);
        }

    }
}