using FashionStore.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Razor.Tokenizer.Symbols;

namespace FashionStore.Areas.Admin.Controllers
{
    public class UserController : Controller
    {
        // GET: Admin/User
        FashionStoreEntities db = new FashionStoreEntities();
        public ActionResult Index(string keyword = "")
        {
            ViewBag.Roles = db.Roles.ToList();
            ViewBag.Keyword = keyword;
            return View();
        }
        public ActionResult getAllUser(string keyword, int? roleId, bool? isActive)
        {
            ViewBag.Roles = db.Roles.ToList();
            string connStr = ConfigurationManager
                .ConnectionStrings["FashionStoreConnection"].ConnectionString;

            List<Tuple<int, string, string, int, string, DateTime, Tuple<bool, string>>> users
                = new List<Tuple<int, string, string, int, string, DateTime, Tuple<bool, string>>>();

            using (SqlConnection conn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand("sp_getAllUser", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new Tuple<int, string, string, int, string, DateTime, Tuple<bool, string>>(
                            Convert.ToInt32(reader["CustomerID"]),
                            reader["FullName"] == DBNull.Value ? "N/A" : reader["FullName"].ToString(),
                            reader["Email"]?.ToString(),
                            Convert.ToInt32(reader["RoleID"]),
                            reader["RoleName"]?.ToString(),
                            Convert.ToDateTime(reader["CreatedAt"]),
                            new Tuple<bool, string>(
                                Convert.ToBoolean(reader["IsActive"]),
                                reader["PhoneNumber"] == DBNull.Value ? "N/A" : reader["PhoneNumber"].ToString()
                                )
                            ) );
                    }
                }
            }

            if (!string.IsNullOrEmpty(keyword))
            {
                users = users.Where(u =>
                    u.Item2.Contains(keyword) || 
                    u.Item3.Contains(keyword)      
                ).ToList();
            }

            if (roleId.HasValue)
            {
                users = users.Where(u => u.Item4 == roleId.Value).ToList();
            }

            if (isActive.HasValue)
            {
                users = users.Where(u => u.Item7.Item1 == isActive.Value).ToList();
            }

            return PartialView(users);
        }

        public ActionResult UpdateState(int CustomerID)
        {
            Customer c = db.Customers.FirstOrDefault(t => t.CustomerID == CustomerID);
            if (c.IsActive == true)
            {
                c.IsActive = false;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                c.IsActive = true;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
        }
        public ActionResult ChangeRole(int CustomerID, int RoleID)
        {
            var user = db.Customers.FirstOrDefault(x => x.CustomerID == CustomerID);
            if (user == null)
                return RedirectToAction("Index");

            user.RoleID = RoleID;
            db.SaveChanges();

            TempData["Success"] = "Đổi vai trò thành công";
            return RedirectToAction("Index");
        }

    }
}