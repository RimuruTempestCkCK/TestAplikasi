using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using TestAplikasi.Models;

namespace TestAplikasi.Controllers
{
    public class AdminController : Controller
    {
        private readonly string connStr =
            ConfigurationManager
            .ConnectionStrings["DBConnection"]
            .ConnectionString;

        // =============================================
        // Cek Login
        // =============================================
        private bool IsLoggedIn()
        {
            return Session["Id"] != null;
        }

        // =============================================
        // Cek Role Admin
        // =============================================
        private bool IsAdmin()
        {
            return Session["Role"] != null &&
                   Session["Role"].ToString() == "Admin";
        }

        // =============================================
        // Validasi Akses
        // =============================================
        private ActionResult CheckAccess()
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            if (!IsAdmin())
                return RedirectToAction("Dashboard", "Pimpinan");

            return null;
        }

        // =============================================
        // Dashboard
        // =============================================
        public ActionResult Dashboard()
        {
            var access = CheckAccess();
            if (access != null)
                return access;

            return View();
        }

        // =============================================
        // USER
        // =============================================
        public ActionResult User()
        {
            var access = CheckAccess();
            if (access != null)
                return access;

            var listUser = new List<UserModel>();

            try
            {
                using (SqlConnection conn =
                    new SqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                        SELECT
                            Id,
                            NamaLengkap,
                            Username,
                            Email,
                            Role,
                            Status
                        FROM dbo.Users
                        ORDER BY Id";

                    using (SqlCommand cmd =
                        new SqlCommand(query, conn))
                    using (SqlDataReader reader =
                        cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            listUser.Add(new UserModel
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                NamaLengkap = reader["NamaLengkap"]?.ToString(),
                                Username = reader["Username"]?.ToString(),
                                Email = reader["Email"]?.ToString(),
                                Role = reader["Role"]?.ToString(),
                                Status = reader["Status"]?.ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Gagal memuat data user: " + ex.Message;
            }

            return View(listUser);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            string NamaLengkap,
            string Username,
            string Email,
            string Password,
            string Role,
            string Status)
        {
            var access = CheckAccess();
            if (access != null)
                return access;

            try
            {
                using (SqlConnection conn =
                    new SqlConnection(connStr))
                {
                    conn.Open();

                    string cekQuery = @"
                        SELECT COUNT(*)
                        FROM dbo.Users
                        WHERE Username = @Username";

                    using (SqlCommand cekCmd =
                        new SqlCommand(cekQuery, conn))
                    {
                        cekCmd.Parameters.AddWithValue(
                            "@Username",
                            Username);

                        int jumlah =
                            (int)cekCmd.ExecuteScalar();

                        if (jumlah > 0)
                        {
                            TempData["ErrorMessage"] =
                                "Username sudah digunakan.";

                            return RedirectToAction("User");
                        }
                    }

                    string query = @"
                        INSERT INTO dbo.Users
                        (
                            NamaLengkap,
                            Username,
                            Email,
                            Password,
                            Role,
                            Status
                        )
                        VALUES
                        (
                            @NamaLengkap,
                            @Username,
                            @Email,
                            @Password,
                            @Role,
                            @Status
                        )";

                    using (SqlCommand cmd =
                        new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@NamaLengkap", NamaLengkap);
                        cmd.Parameters.AddWithValue("@Username", Username);
                        cmd.Parameters.AddWithValue("@Email", Email);
                        cmd.Parameters.AddWithValue("@Password", Password);
                        cmd.Parameters.AddWithValue("@Role", Role);
                        cmd.Parameters.AddWithValue("@Status", Status);

                        cmd.ExecuteNonQuery();
                    }

                    TempData["SuccessMessage"] =
                        "User berhasil ditambahkan.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Gagal menambahkan user: " +
                    ex.Message;
            }

            return RedirectToAction("User");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int Id)
        {
            var access = CheckAccess();
            if (access != null)
                return access;

            try
            {
                if (Session["Id"] != null &&
                    Convert.ToInt32(Session["Id"]) == Id)
                {
                    TempData["ErrorMessage"] =
                        "Tidak bisa menghapus akun sendiri.";

                    return RedirectToAction("User");
                }

                using (SqlConnection conn =
                    new SqlConnection(connStr))
                {
                    conn.Open();

                    string query =
                        "DELETE FROM dbo.Users WHERE Id = @Id";

                    using (SqlCommand cmd =
                        new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", Id);
                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["SuccessMessage"] =
                    "User berhasil dihapus.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Gagal menghapus user: " +
                    ex.Message;
            }

            return RedirectToAction("User");
        }

        // =============================================
        // PRODUK
        // =============================================
        public ActionResult Produk()
        {
            var access = CheckAccess();
            if (access != null)
                return access;

            var listProduk =
                new List<ProdukModel>();

            try
            {
                using (SqlConnection conn =
                    new SqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                        SELECT
                            Id,
                            NamaProduk,
                            Harga,
                            Deskripsi
                        FROM dbo.Produk
                        ORDER BY Id DESC";

                    using (SqlCommand cmd =
                        new SqlCommand(query, conn))
                    using (SqlDataReader reader =
                        cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            listProduk.Add(
                                new ProdukModel
                                {
                                    Id =
                                        Convert.ToInt32(
                                            reader["Id"]),

                                    NamaProduk =
                                        reader["NamaProduk"]
                                        ?.ToString(),

                                    Harga =
                                        Convert.ToDecimal(
                                            reader["Harga"]),

                                    Deskripsi =
                                        reader["Deskripsi"]
                                        ?.ToString()
                                });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Gagal memuat produk: "
                    + ex.Message;
            }

            return View(listProduk);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateProduk(
            string NamaProduk,
            decimal Harga,
            string Deskripsi)
        {
            var access = CheckAccess();
            if (access != null)
                return access;

            try
            {
                using (SqlConnection conn =
                    new SqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                        INSERT INTO dbo.Produk
                        (
                            NamaProduk,
                            Harga,
                            Deskripsi
                        )
                        VALUES
                        (
                            @NamaProduk,
                            @Harga,
                            @Deskripsi
                        )";

                    using (SqlCommand cmd =
                        new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@NamaProduk", NamaProduk);
                        cmd.Parameters.AddWithValue("@Harga", Harga);
                        cmd.Parameters.AddWithValue("@Deskripsi", Deskripsi);

                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["SuccessMessage"] =
                    "Produk berhasil ditambahkan.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Gagal tambah produk: "
                    + ex.Message;
            }

            return RedirectToAction("Produk");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProduk(
    int Id,
    string NamaProduk,
    decimal Harga,
    string Deskripsi)
        {
            var access = CheckAccess();
            if (access != null)
                return access;

            try
            {
                using (SqlConnection conn =
                    new SqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                UPDATE dbo.Produk
                SET NamaProduk = @NamaProduk,
                    Harga = @Harga,
                    Deskripsi = @Deskripsi
                WHERE Id = @Id";

                    using (SqlCommand cmd =
                        new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue(
                            "@Id",
                            Id);

                        cmd.Parameters.AddWithValue(
                            "@NamaProduk",
                            NamaProduk);

                        cmd.Parameters.AddWithValue(
                            "@Harga",
                            Harga);

                        cmd.Parameters.AddWithValue(
                            "@Deskripsi",
                            Deskripsi);

                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["SuccessMessage"] =
                    "Produk berhasil diperbarui.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Gagal update produk: "
                    + ex.Message;
            }

            return RedirectToAction("Produk");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteProduk(int Id)
        {
            var access = CheckAccess();
            if (access != null)
                return access;

            try
            {
                using (SqlConnection conn =
                    new SqlConnection(connStr))
                {
                    conn.Open();

                    string query =
                        "DELETE FROM dbo.Produk WHERE Id = @Id";

                    using (SqlCommand cmd =
                        new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", Id);
                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["SuccessMessage"] =
                    "Produk berhasil dihapus.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Gagal hapus produk: "
                    + ex.Message;
            }

            return RedirectToAction("Produk");
        }

        // =============================================
        // LAPORAN
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