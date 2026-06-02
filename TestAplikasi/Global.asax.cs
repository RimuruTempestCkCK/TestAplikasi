using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace TestAplikasi
{
    public class MvcApplication
        : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            FilterConfig.RegisterGlobalFilters(
                GlobalFilters.Filters);

            RouteConfig.RegisterRoutes(
                RouteTable.Routes);

            BundleConfig.RegisterBundles(
                BundleTable.Bundles);
        }

        protected void Application_Error()
        {
            Exception exception =
                Server.GetLastError();

            HttpException httpException =
                exception as HttpException;

            if (httpException != null)
            {
                int statusCode =
                    httpException.GetHttpCode();

                // Handle halaman tidak ditemukan
                if (statusCode == 404)
                {
                    Response.Clear();

                    Server.ClearError();

                    RouteData routeData =
                        new RouteData();

                    routeData.Values["controller"] =
                        "Error";

                    routeData.Values["action"] =
                        "NotFound";

                    IController controller =
                        new Controllers.ErrorController();

                    controller.Execute(
                        new RequestContext(
                            new HttpContextWrapper(Context),
                            routeData
                        )
                    );
                }
            }
        }
    }
}