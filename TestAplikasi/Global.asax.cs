using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using TestAplikasi.Controllers;

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
            Exception exception = Server.GetLastError();
 
            if (exception != null)
            {
                HttpException httpException = exception as HttpException;
 
                // Cek jika adalah 404 error
                if (httpException != null && httpException.GetHttpCode() == 404)
                {
                    Server.ClearError();
                    
                    Response.StatusCode = 404;
                    Response.TrySkipIisCustomErrors = true;
                    
                    RouteData routeData = new RouteData();
                    routeData.Values.Add("controller", "Error");
                    routeData.Values.Add("action", "NotFound");
 
                    IController errorController = new ErrorController();
                    errorController.Execute(
                        new RequestContext(new HttpContextWrapper(Context), routeData));
                }
            }
        }
    }
}