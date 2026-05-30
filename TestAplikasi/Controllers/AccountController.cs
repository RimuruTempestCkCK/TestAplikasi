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
            if (Session["Id"] != null)
            {
                string role = Session["Role"]?.ToString();
                if (role == "Admin") return RedirectToAction("Dashboard", "Admin");
                if (role == "Pimpinan") return RedirectToAction("Dashboard", "Pimpinan");
                if (role == "Kasir") return RedirectToAction("Dashboard", "Kasir");
            }
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
                        SELECT Id, NamaLengkap, Username, Role, FotoProfil
                        FROM dbo.Users
                        WHERE Username = @Username
                          AND Password = @Password
                          AND Status   = 'Aktif'";

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
                        Session["FotoProfil"] = reader["FotoProfil"]?.ToString();

                        string role = reader["Role"].ToString();
                        reader.Close();

                        if (role == "Admin")
                            return RedirectToAction("Dashboard", "Admin");
                        else if (role == "Pimpinan")
                            return RedirectToAction("Dashboard", "Pimpinan");
                        else if (role == "Kasir")
                            return RedirectToAction("Dashboard", "Kasir");
                        else
                            return RedirectToAction("Login", "Account");
                    }

                    ViewBag.Error =
                        "Username atau Password salah, atau akun tidak aktif.";
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

        // GET: Account/Profile
        public ActionResult Profile()
        {
            if (Session["Id"] == null)
                return RedirectToAction("Login", "Account");

            var model = new TestAplikasi.Models.UserModel();

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                        SELECT Id, NamaLengkap, Username,
                               Email, FotoProfil
                        FROM dbo.Users
                        WHERE Id = @Id";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", Session["Id"]);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                model.Id = Convert.ToInt32(reader["Id"]);
                                model.Username = reader["Username"]?.ToString();
                                model.Email = reader["Email"]?.ToString();
                                model.NamaLengkap = reader["NamaLengkap"]?.ToString();
                                model.FotoProfil = reader["FotoProfil"]?.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Gagal memuat profil: " + ex.Message;
            }

            return View(model);
        }

        // POST: Account/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Profile(
            string NamaLengkap,
            string Email,
            string PasswordLama,
            string PasswordBaru,
            System.Web.HttpPostedFileBase FotoFile)
        {
            if (Session["Id"] == null)
                return RedirectToAction("Login", "Account");

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    // Ambil password dan foto lama
                    string fotoLama = null;
                    string passLama = null;

                    using (SqlCommand sel = new SqlCommand(
                        "SELECT Password, FotoProfil FROM dbo.Users WHERE Id = @Id",
                        conn))
                    {
                        sel.Parameters.AddWithValue("@Id", Session["Id"]);
                        using (SqlDataReader r = sel.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                passLama = r["Password"]?.ToString();
                                fotoLama = r["FotoProfil"]?.ToString();
                            }
                        }
                    }

                    // Validasi password lama
                    if (!string.IsNullOrEmpty(PasswordBaru))
                    {
                        if (passLama != PasswordLama)
                        {
                            TempData["ErrorMessage"] =
                                "Password lama tidak sesuai.";
                            return RedirectToAction("Profile");
                        }
                    }

                    // Proses upload foto
                    string namaFoto = fotoLama;
                    if (FotoFile != null && FotoFile.ContentLength > 0)
                    {
                        string[] allowedExt =
                            { ".jpg", ".jpeg", ".png", ".gif" };
                        string ext = System.IO.Path
                            .GetExtension(FotoFile.FileName).ToLower();

                        if (!System.Array.Exists(allowedExt, e => e == ext))
                        {
                            TempData["ErrorMessage"] =
                                "Format tidak didukung. Gunakan jpg, jpeg, png, gif.";
                            return RedirectToAction("Profile");
                        }

                        // Hapus foto lama
                        if (!string.IsNullOrEmpty(fotoLama))
                        {
                            string pathLama = Server.MapPath(
                                "~/Content/uploads/profil/" + fotoLama);
                            if (System.IO.File.Exists(pathLama))
                                System.IO.File.Delete(pathLama);
                        }

                        // Buat folder jika belum ada
                        string folder = Server.MapPath(
                            "~/Content/uploads/profil/");
                        if (!System.IO.Directory.Exists(folder))
                            System.IO.Directory.CreateDirectory(folder);

                        namaFoto = Convert.ToInt32(Session["Id"])
                                   + "_" + DateTime.Now.Ticks + ext;

                        FotoFile.SaveAs(folder + namaFoto);
                    }

                    // Update database
                    string updateQuery = !string.IsNullOrEmpty(PasswordBaru)
                        ? @"UPDATE dbo.Users
                            SET NamaLengkap = @NamaLengkap,
                                Email       = @Email,
                                Password    = @Password,
                                FotoProfil  = @FotoProfil
                            WHERE Id = @Id"
                        : @"UPDATE dbo.Users
                            SET NamaLengkap = @NamaLengkap,
                                Email       = @Email,
                                FotoProfil  = @FotoProfil
                            WHERE Id = @Id";

                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", Session["Id"]);
                        cmd.Parameters.AddWithValue("@NamaLengkap", NamaLengkap ?? "");
                        cmd.Parameters.AddWithValue("@Email", Email ?? "");
                        cmd.Parameters.AddWithValue("@FotoProfil",
                            (object)namaFoto ?? DBNull.Value);

                        if (!string.IsNullOrEmpty(PasswordBaru))
                            cmd.Parameters.AddWithValue("@Password", PasswordBaru);

                        cmd.ExecuteNonQuery();
                    }

                    Session["Nama"] = NamaLengkap;
                    Session["FotoProfil"] = namaFoto;

                    TempData["SuccessMessage"] = "Profil berhasil diperbarui.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Gagal memperbarui profil: " + ex.Message;
            }

            return RedirectToAction("Profile");
        }
    }
}