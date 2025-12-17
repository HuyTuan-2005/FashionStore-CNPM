using System;
using System.Web;
using System.Web.Mvc;

namespace FashionStore
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }

    public class CustomAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            // Kiểm tra user đã login chưa
            if (httpContext.Session["Customer"] == null)
            {
                return false;
            }

            return true;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            // Redirect về trang login nếu chưa đăng nhập
            filterContext.Result = new RedirectResult("~/Account/Login");
        }

    }
}