using System.Web.Mvc;

namespace TestAplikasi.Controllers
{
    public class PimpinanController : Controller
    {
        // =============================================
        // Cek login
        // =============================================
        private bool IsLoggedIn()
        {
            return Session["Id"] != null;
        }

        // =============================================
        // Cek role Pimpinan
        // =============================================
        private bool IsPimpinan()
        {
            return Session["Role"] != null &&
                   Session["Role"].ToString() == "Pimpinan";
        }

        // =============================================
        // Validasi akses
        // =============================================
        private ActionResult CheckAccess()
        {
            // Belum login
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            // Jika Admin masuk ke area pimpinan
            if (Session["Role"] != null &&
                Session["Role"].ToString() == "Admin")
            {
                return RedirectToAction("Dashboard", "Admin");
            }

            // Jika bukan Pimpinan
            if (!IsPimpinan())
                return RedirectToAction("Login", "Account");

            return null;
        }

        // =============================================
        // GET: Pimpinan/Dashboard
        // =============================================
        public ActionResult Dashboard()
        {
            var access = CheckAccess();

            if (access != null)
                return access;

            return View();
        }

        // =============================================
        // GET: Pimpinan/Laporan
        // =============================================
        public ActionResult Laporan()
        {
            var access = CheckAccess();

            if (access != null)
                return access;

            return View();
        }
    }
}