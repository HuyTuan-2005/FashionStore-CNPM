using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using FashionStore.Models;

namespace FashionStore.Areas.Admin.Controllers
{
    public class CategoryController : Controller
    {
        private readonly FashionStoreEntities _entities = new FashionStoreEntities();
        // GET: Admin/Category
        public ActionResult Index()
        {
            ViewBag.CategoryGroups = _entities.CategoryGroups.ToList();
            var model = _entities.Categories.Include(pro => pro.Products).ToList();
            return View(model);
        }

        public ActionResult Update(Category category)
        {
            var existingCategory = _entities.Categories.Find(category.CategoryID);
            if (existingCategory == null)
            {
                TempData["Error"] = "Cập nhật danh mục thất bại.";
                return HttpNotFound();
            }
            existingCategory.CategoryName = category.CategoryName;
            existingCategory.GroupID = category.GroupID;
            _entities.Entry(existingCategory).State = EntityState.Modified;
            _entities.SaveChanges();
            
            TempData["Success"] = "Cập nhật danh mục thành công.";
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int id)
        {
            var category = _entities.Categories.Find(id);
            if (category == null)
            {
                TempData["Error"] = "Xóa danh mục thất bại.";
                return HttpNotFound();
            }

            if (category.Products.Any())
            {
                TempData["Error"] = "Không thể xóa danh mục vì có sản phẩm liên kết.";
                return RedirectToAction("Index");
            }

            _entities.Categories.Remove(category);
            _entities.SaveChanges();

            TempData["Success"] = "Xóa danh mục thành công.";
            return RedirectToAction("Index");
        }

        public ActionResult Create(Category category)
        {
            if (string.IsNullOrEmpty(category.CategoryName))
            {
                TempData["Error"] = "Tên danh mục không được để trống.";
                return RedirectToAction("Index");
            }
            
            if(category.GroupID == 0 || !_entities.CategoryGroups.Any(g => g.GroupID == category.GroupID))
            {
                TempData["Error"] = "Nhóm danh mục không hợp lệ.";
                return RedirectToAction("Index");
            }
            
            _entities.Categories.Add(category);
            _entities.SaveChanges();
            TempData["Success"] = "Thêm danh mục thành công.";
            return RedirectToAction("Index");
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