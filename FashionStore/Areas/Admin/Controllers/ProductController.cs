using FashionStore.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FashionStore.ViewModels;

namespace FashionStore.Areas.Admin.Controllers
{
    public class ProductController : Controller
    {
        private readonly FashionStoreEntities _entities = new FashionStoreEntities();

        // GET: Admin/Product
        public ActionResult Index(int? page, string search, int? categoryId, bool? status)
        {
            const int pageSize = 10;
            int pageNumber = page ?? 1;

            IQueryable<Product> query = _entities.Products;

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.ProductName.Contains(search));

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryID == categoryId.Value);

            if (status.HasValue)
                query = query.Where(p => p.IsActive == status);

            // Sắp xếp nhất quán (ví dụ theo khóa giảm dần thay cho Reverse)
            query = query.OrderByDescending(p => p.ProductID);

            int total = query.Count();
            var lstProduct = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
            ViewBag.Categories = _entities.Categories.ToList();

            ViewBag.Status = new List<object>
            {
                new { StatusName = "Còn hàng", StatusValue = true },
                new { StatusName = "Hết hàng", StatusValue = false }
            };

            return View(lstProduct);
        }

        // GET: Admin/Product/Create
        public ActionResult Create()
        {
            ViewBag.Categories = _entities.Categories.ToList();
            ViewBag.Sizes = _entities.Sizes.ToList();
            ViewBag.Colors = _entities.Colors.ToList();
            return View();
        }

        // POST: Admin/Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ProductCreateViewModel model)
        {
            // try
            // {
            List<ProductImage> images = new List<ProductImage>();

            if (model.MainImage != null)
            {
                var path = Server.MapPath("~/Content/img/products/" + model.MainImage.FileName);
                model.MainImage.SaveAs(path);
                images.Add(new ProductImage
                {
                    ImageUrl = model.MainImage.FileName,
                    IsPrimary = true,
                });
            }

            if (model.AdditionalImages != null)
            {
                foreach (var img in model.AdditionalImages)
                {
                    if (img != null)
                    {
                        var addPath = Server.MapPath("~/Content/img/products/" + img.FileName);
                        img.SaveAs(addPath);
                        images.Add(new ProductImage
                        {
                            ImageUrl = img.FileName,
                            IsPrimary = false
                        });
                    }
                }
            }

            var product = new Product
            {
                ProductName = model.ProductName,
                CategoryID = model.CategoryID,
                Description = model.Description,
                DiscountPercent = model.DiscountPercent,
                BasePrice = model.BasePrice,
                CreatedAt = DateTime.Now,
                IsActive = model.IsActive,
                ProductImages = images,
            };

            _entities.Products.Add(product);
            _entities.SaveChanges();

            List<ProductVariant> productVariants = new List<ProductVariant>();
            if (model.Variants != null)
            {
                foreach (var v in model.Variants)
                {
                    productVariants.Add(new ProductVariant
                    {
                        ColorID = v.ColorID,
                        SizeID = v.SizeID,
                        SKU = string.IsNullOrWhiteSpace(v.SKU)
                            ? $"P{product.ProductID:D4}-{v.ColorID:D2}{v.SizeID:D2}"
                            : v.SKU.Trim(),
                        Stock = v.Stock,
                        Status = v.Status ?? "Available",
                    });
                }
            }

            // if (product.ProductVariants.Sum(x => x.Stock) == 0)
            // {
            //     product.IsActive = false;
            // }

            product.ProductVariants = productVariants;
            _entities.SaveChanges();
            return RedirectToAction("Index");
        }

        // GET: Admin/Product/Edit/5
        public ActionResult Edit(int id)
        {
            ViewBag.Categories = _entities.Categories.ToList();
            var product = _entities.Products.Find(id);
            return View(product);
        }

        // POST: Admin/Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, ProductCreateViewModel model)
        {
            try
            {
                var product = _entities.Products.Find(id);
                if (product != null)
                {
                    if (model.MainImage != null)
                    {
                        // neu như đã có ảnh chính thì thay đổi tên ảnh đó
                        ProductImage isPrimaryImage =
                            product.ProductImages.FirstOrDefault(img => img.IsPrimary != null && img.IsPrimary.Value);
                        var fileName = model.MainImage.FileName;
                        if (isPrimaryImage != null)
                        {
                            isPrimaryImage.ImageUrl = fileName;
                            product.ProductImages.Add(isPrimaryImage);
                        }
                        else
                        {
                            product.ProductImages.Add(new ProductImage
                            {
                                ImageUrl = fileName,
                                IsPrimary = true,
                            });
                        }

                        var path = Server.MapPath("~/Content/img/products/" + fileName);
                        model.MainImage.SaveAs(path);
                    }

                    product.ProductName = model.ProductName;
                    product.CategoryID = model.CategoryID;
                    product.Description = model.Description;
                    product.DiscountPercent = model.DiscountPercent;
                    product.BasePrice = model.BasePrice;
                    product.IsActive = model.IsActive;

                    if (model.Variants != null)
                    {
                        foreach (var iv in model.Variants)
                        {
                            // update hiện có
                            var ev = product.ProductVariants.FirstOrDefault(v => v.VariantID == iv.VariantID);
                            if (ev != null)
                            {
                                ev.Stock = iv.Stock;
                                ev.Status = string.IsNullOrWhiteSpace(iv.Status) ? "Available" : iv.Status.Trim();
                                ev.SKU = string.IsNullOrWhiteSpace(iv.SKU)
                                    ? $"P{product.ProductID:D4}-{iv.ColorID:D2}{iv.SizeID:D2}"
                                    : iv.SKU.Trim();
                            }
                        }
                    }

                    _entities.SaveChanges();
                }
                else
                {
                    return HttpNotFound("Loại sản phẩm không tồn tại");
                }

                return RedirectToAction("Index");
            }
            catch
            {
                return HttpNotFound("Đã có lỗi xảy ra khi cập nhật sản phẩm");
            }
        }

        // POST: Admin/Product/EditStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditStatus(int productId, bool? status)
        {
            if (productId <= 0)
            {
                return HttpNotFound();
            }

            var product = _entities.Products.Find(productId);
            if (product != null && status.HasValue)
            {
                product.IsActive = status;
            }

            _entities.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditManyStatus(string productId, bool? status)
        {
            if (string.IsNullOrEmpty(productId) || !status.HasValue)
            {
                return HttpNotFound();
            }

            var idList = productId.Split(',').Select(int.Parse).ToList();

            List<Product> product = _entities.Products
                .Where(p => idList.Contains(p.ProductID)).ToList();
            foreach (var item in product)
            {
                item.IsActive = status;
            }

            _entities.SaveChanges();
            return RedirectToAction("Index");
        }

        // GET: Admin/Product/EditImages/5
        public ActionResult EditImages(int id)
        {
            var product = _entities.Products.Find(id);
            return View(product);
        }

        // POST: Admin/Product/EditImages/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditImages(int id, ProductImage model, HttpPostedFileBase mainImage,
            List<HttpPostedFileBase> additionalImages)
        {
            try
            {
                var product = _entities.Products.Find(id);
                if (product != null)
                {
                    if (mainImage != null)
                    {
                        // neu như đã có ảnh chính thì xóa ảnh đó đi
                        var isPrimaryImage =
                            product.ProductImages.FirstOrDefault(img => img.IsPrimary != null && img.IsPrimary.Value);
                        var fileName = mainImage.FileName;
                        if (isPrimaryImage != null)
                        {
                            isPrimaryImage.ImageUrl = fileName;
                        }
                        else
                        {
                            product.ProductImages.Add(new ProductImage
                            {
                                ImageUrl = fileName,
                                IsPrimary = true,
                            });
                        }

                        var path = Server.MapPath("~/Content/img/products/" + fileName);
                        mainImage.SaveAs(path);
                    }

                    // neu chưa quá 4 ảnh thì mới được thêm
                    if (product.ProductImages.Count(x => x.IsPrimary == false) < 4)
                    {
                        if (additionalImages != null)
                        {
                            foreach (var img in additionalImages)
                            {
                                if (img != null)
                                {
                                    product.ProductImages.Add(new ProductImage()
                                    {
                                        ImageUrl = img.FileName,
                                        IsPrimary = false
                                    });
                                    var path = Server.MapPath("~/Content/img/products/" + img.FileName);
                                    img.SaveAs(path);
                                }
                            }
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", "Không thể thêm ảnh vì đã đủ số lượng ảnh phụ (4 ảnh)");
                    }

                    _entities.SaveChanges();
                }

                return RedirectToAction("Index");
            }
            catch
            {
                return RedirectToAction("Index");
            }
        }


        // GET: Admin/Product/Delete/5
        // public ActionResult Delete(int id)
        // {
        //     return View();
        // }

        // POST: Admin/Product/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            try
            {
                var product = _entities.Products.Find(id);
                if (product != null)
                {
                    _entities.Products.Remove(product);
                    _entities.SaveChanges();
                }

                return RedirectToAction("Index");
            }
            catch
            {
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteMany(string id)
        {
            try
            {
                if (!string.IsNullOrEmpty(id))
                {
                    var idList = id.Split(',').Select(int.Parse).ToList();
                    foreach (var i in idList)
                    {
                        var product = _entities.Products.Find(i);
                        if (product != null)
                        {
                            _entities.Products.Remove(product);
                        }
                    }

                    _entities.SaveChanges();
                }

                return RedirectToAction("Index");
            }
            catch
            {
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteImage(int productId, int imageId)
        {
            var image = _entities.ProductImages.FirstOrDefault(img => img.ImageID == imageId);
            if (image != null && image.ProductID == productId)
            {
                _entities.ProductImages.Remove(image);
                _entities.SaveChanges();
            }

            return RedirectToAction("EditImages", new { id = productId });
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