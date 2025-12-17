using FashionStore.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FashionStore.ViewModels;

namespace FashionStore.Controllers
{
    public class ProductController : Controller
    {
        // GET: Product
        private readonly FashionStoreEntities _entities = new FashionStoreEntities();

        public ActionResult DanhMuc()
        {
            return PartialView(_entities.CategoryGroups.ToList());
        }

        public ActionResult Index(int? catID, string kw, int page = 1, int sort = 0, int? price = 0)
        {
            var model = _entities.Products.AsQueryable().Where(t => t.IsActive == true);
            // Lọc theo loại
            if (catID.HasValue)
            {
                model = model.Where(cat => cat.Category.CategoryID == catID.Value);
                var category = _entities.Categories.Find(catID.Value); // gửi tên cat lên view
                if (category != null)
                {
                    ViewBag.CategoryName = category.CategoryName;
                }
            }

            // Tìm kiếm theo kw
            if (!string.IsNullOrWhiteSpace(kw))
            {
                model = model.Where(sp => sp.ProductName.ToLower().Contains(kw.ToLower()));
            }

            // Sắp xếp 
            switch (sort)
            {
                case 1: // Tên A-Z
                    model = model.OrderBy(p => p.ProductName);
                    break;
                case 2: // Tên Z-A
                    model = model.OrderByDescending(p => p.ProductName);
                    break;
                case 3: // Giá tăng dần
                    model = model.OrderBy(p => p.BasePrice);
                    break;
                case 4: // Giá giảm dần
                    model = model.OrderByDescending(p => p.BasePrice);
                    break;
                default: // Case 0 hoặc số lạ
                    model = model.OrderBy(p => p.ProductID);
                    break;
            }

            // lọc theo giá 
            if (price.HasValue)
            {
                switch (price)
                {
                    case 1: // 0 - 300k
                        model = model.Where(p => p.BasePrice < 300000);
                        break;
                    case 2: // 300k - 500k
                        model = model.Where(p => p.BasePrice >= 300000 && p.BasePrice < 500000);
                        break;
                    case 3: // 500k - 1 triệu
                        model = model.Where(p => p.BasePrice >= 500000 && p.BasePrice < 1000000);
                        break;
                    case 4: // Trên 1 triệu
                        model = model.Where(p => p.BasePrice >= 1000000);
                        break;
                    // Case 0 hoặc null thì không làm gì (lấy tất cả)
                }
            }

            // Phân trang
            int totalProducts = model.Count();
            int pageamount = (page - 1) * 9;
            var productsOnPage = model
                .Skip((page - 1) * 9)
                .Take(9)
                .ToList();
            int totalPages = (int)Math.Ceiling((double)totalProducts / 9);

            // Dùng ViewBag ?? truy?n thông tin phân trang sang View
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalPrd = totalProducts;
            ViewBag.CatID = catID;
            ViewBag.Kw = kw;
            ViewBag.Sort = sort;
            ViewBag.DM = _entities.CategoryGroups.ToList();
            return View(productsOnPage);
        }

        public ActionResult Details(int? id)
        {
            if (id.HasValue)
            {
                var product = _entities.Products.FirstOrDefault(p => p.ProductID == id);

                if (product == null)
                    return HttpNotFound();

                var viewmodel = new ProductDetailsViewModel()
                {
                    Product = product,
                    RelatedProducts = _entities.Products.Where(x => x.ProductID != product.ProductID && x.CategoryID == product.CategoryID).ToList()
                };

                return View(viewmodel);
            }

            return HttpNotFound();
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