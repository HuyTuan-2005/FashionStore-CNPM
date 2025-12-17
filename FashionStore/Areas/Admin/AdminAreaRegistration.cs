using System.Web.Mvc;

namespace FashionStore.Areas.Admin
{
    public class AdminAreaRegistration : AreaRegistration 
    {
        public override string AreaName => "Admin";

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "Admin_default",
                "Admin/{controller}/{action}/{id}",
                new {controller="dashboard", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "FashionStore.Areas.Admin.Controllers" }
            );
        }
    }
}