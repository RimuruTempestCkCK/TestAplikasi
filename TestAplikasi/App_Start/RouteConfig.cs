using System.Web.Mvc;
using System.Web.Routing;

namespace TestAplikasi
{
    public class RouteConfig
    {
        public static void RegisterRoutes(
            RouteCollection routes)
        {
            routes.IgnoreRoute(
                "{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "AdminDashboard",
                url: "Admin/Dashboard",
                defaults: new { controller = "Admin", action = "Dashboard" },
                namespaces: new[] { "TestAplikasi.Controllers" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new
                {
                    controller = "Account",
                    action = "Login",
                    id = UrlParameter.Optional
                },
                namespaces: new[] { "TestAplikasi.Controllers" }
            );
        }
    }
}