using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace TestAplikasi.Controllers
{
    public class AccountController : Controller
    {
        string connStr = ConfigurationManager
            .ConnectionStrings["DBConnection"]
            .ConnectionString;

        // GET: Account/Login
        public ActionResult Login()
        {
            // Jika sudah login, redirect ke dashboard
            if (Session["Id"] != null)
                return RedirectToAction("Dashboard", "Admin");

            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string Username, string Password)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                        SELECT Id, NamaLengkap, Username, Role
                        FROM Users
                        WHERE Username = @Username
                          AND Password = @Password
                          AND Status   = 'Aktif'
                    ";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Username", Username);
                    cmd.Parameters.AddWithValue("@Password", Password);

                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        Session["Id"] = reader["Id"];
                        Session["Nama"] = reader["NamaLengkap"].ToString();
                        Session["Role"] = reader["Role"].ToString();
                        Session["Username"] = reader["Username"].ToString();

                        string role = reader["Role"].ToString();
                        reader.Close();

                        if (role == "Admin")
                            return RedirectToAction("Dashboard", "Admin");
                        else if (role == "Pimpinan")
                            return RedirectToAction("Dashboard", "Pimpinan");
                        else
                            return RedirectToAction("Dashboard", "Admin");
                    }

                    ViewBag.Error = "Username atau Password salah, atau akun tidak aktif.";
                    return View();
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Terjadi kesalahan: " + ex.Message;
                return View();
            }
        }

        // GET: Account/Logout
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login", "Account");
        }
    }
}