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
                name: "Admin_Default",
                url: "Admin/{action}/{id}",
                defaults: new { controller = "Admin", action = "Dashboard", id = UrlParameter.Optional },
                namespaces: new[] { "TestAplikasi.Controllers" }
            );

            routes.MapRoute(
                name: "Pimpinan_Default",
                url: "Pimpinan/{action}/{id}",
                defaults: new { controller = "Pimpinan", action = "Dashboard", id = UrlParameter.Optional },
                namespaces: new[] { "TestAplikasi.Controllers" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new
                {
                    controller = "Home",
                    action = "Landing",
                    id = UrlParameter.Optional
                },
                namespaces: new[] { "TestAplikasi.Controllers" }
            );
        }
    }
}