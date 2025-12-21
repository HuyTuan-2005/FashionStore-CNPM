using System;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using System.Web.Configuration;
using System.Data.SqlClient;

namespace FashionStore.Areas.Admin.Controllers
{
    public class BackupController : Controller
    {
        private Models.FashionStoreEntities db = new Models.FashionStoreEntities();

        public ActionResult Index()
        {
            // Lấy cấu hình nhanh
            ViewBag.Time = ConfigurationManager.AppSettings["AutoBackupTime"];
            ViewBag.Status = ConfigurationManager.AppSettings["AutoBackupEnabled"] == "true";

            // Tự động kiểm tra backup khi vào trang
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (ViewBag.Status && ConfigurationManager.AppSettings["LastBackupDate"] != today
                && DateTime.Now.ToString("HH:mm") == ViewBag.Time)
            {
                if (DoBackup()) UpdateConfig("LastBackupDate", today);
            }
            return View();
        }

        [HttpPost]
        public ActionResult Action(string type, string time, string status, HttpPostedFileBase file)
        {
            if (type == "save")
            {
                UpdateConfig("AutoBackupTime", time);
                UpdateConfig("AutoBackupEnabled", status);
                TempData["S"] = "Đã lưu cài đặt!";
            }
            else if (type == "now")
            {
                if (DoBackup()) TempData["S"] = "Đã sao lưu vào D:\\Backups";
            }
            else if (type == "restore" && file != null)
            {
                if (DoRestore(file)) TempData["S"] = "Phục hồi thành công!";
            }
            return RedirectToAction("Index");
        }

        private bool DoBackup()
        {
            try
            {
                string dbName = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["FashionStoreConnection"].ConnectionString).InitialCatalog;
                string path = $@"D:\Backups\{dbName}_{DateTime.Now:yyyyMMddHHmm}.bak";
                System.IO.Directory.CreateDirectory(@"D:\Backups\");
                db.Database.ExecuteSqlCommand(System.Data.Entity.TransactionalBehavior.DoNotEnsureTransaction, $"BACKUP DATABASE [{dbName}] TO DISK='{path}'");
                return true;
            }
            catch (Exception ex) { TempData["E"] = ex.Message; return false; }
        }

        private bool DoRestore(HttpPostedFileBase f)
        {
            try
            {
                string dbName = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["FashionStoreConnection"].ConnectionString).InitialCatalog;
                string path = Server.MapPath("~/App_Data/temp.bak");
                f.SaveAs(path);
                string connStr = ConfigurationManager.ConnectionStrings["FashionStoreConnection"].ConnectionString.Replace(dbName, "master");
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string sql = $@"ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                                   RESTORE DATABASE [{dbName}] FROM DISK='{path}' WITH REPLACE;
                                   ALTER DATABASE [{dbName}] SET MULTI_USER;";
                    new SqlCommand(sql, conn).ExecuteNonQuery();
                }
                return true;
            }
            catch (Exception ex) { TempData["E"] = ex.Message; return false; }
        }

        private void UpdateConfig(string k, string v)
        {
            var c = WebConfigurationManager.OpenWebConfiguration("~");
            c.AppSettings.Settings[k].Value = v;
            c.Save();
        }
    }
}