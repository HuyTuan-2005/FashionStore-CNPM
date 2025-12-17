using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FashionStore.Models;

namespace FashionStore.Areas.Admin.Controllers
{
    public class DashboardController : Controller
    {
        private readonly FashionStoreEntities _entities = new FashionStoreEntities();

        // GET: Admin/Dashboard
        public ActionResult Index()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                _entities.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}