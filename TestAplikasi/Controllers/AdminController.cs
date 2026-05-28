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
        string connStr = ConfigurationManager
            .ConnectionStrings["DBConnection"]
            .ConnectionString;

        // =============================================
        // Cek login
        // =============================================
        private bool IsLoggedIn()
        {
            return Session["Id"] != null;
        }

        // =============================================
        // Cek role Admin
        // =============================================
        private bool IsAdmin()
        {
            return Session["Role"] != null &&
                   Session["Role"].ToString() == "Admin";
        }

        // =============================================
        // Redirect jika tidak punya akses
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
        // GET: Admin/Dashboard
        // =============================================
        public ActionResult Dashboard()
        {
            var access = CheckAccess();
            if (access != null)
                return access;

            return View();
        }

        // =============================================
        // GET: Admin/User
        // =============================================
        public ActionResult User()
        {
            var access = CheckAccess();
            if (access != null)
                return access;

            var listUser = new List<UserModel>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                        SELECT Id,
                               NamaLengkap,
                               Username,
                               Email,
                               Role,
                               Status
                        FROM Users
                        ORDER BY Id";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        listUser.Add(new UserModel
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            NamaLengkap = reader["NamaLengkap"].ToString(),
                            Username = reader["Username"].ToString(),
                            Email = reader["Email"].ToString(),
                            Role = reader["Role"].ToString(),
                            Status = reader["Status"].ToString()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Gagal memuat data: " + ex.Message;
            }

            return View(listUser);
        }

        // =============================================
        // POST: Admin/Create
        // =============================================
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

                    // Cek username sudah ada
                    string cekQuery =
                        "SELECT COUNT(*) FROM Users WHERE Username = @Username";

                    SqlCommand cekCmd =
                        new SqlCommand(cekQuery, conn);

                    cekCmd.Parameters.AddWithValue(
                        "@Username",
                        Username);

                    int jumlah =
                        (int)cekCmd.ExecuteScalar();

                    if (jumlah > 0)
                    {
                        TempData["ErrorMessage"] =
                            "Username \"" +
                            Username +
                            "\" sudah digunakan.";

                        return RedirectToAction("User");
                    }

                    string query = @"
                        INSERT INTO Users
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

                    SqlCommand cmd =
                        new SqlCommand(query, conn);

                    cmd.Parameters.AddWithValue(
                        "@NamaLengkap",
                        NamaLengkap);

                    cmd.Parameters.AddWithValue(
                        "@Username",
                        Username);

                    cmd.Parameters.AddWithValue(
                        "@Email",
                        Email);

                    cmd.Parameters.AddWithValue(
                        "@Password",
                        Password);

                    cmd.Parameters.AddWithValue(
                        "@Role",
                        Role);

                    cmd.Parameters.AddWithValue(
                        "@Status",
                        Status);

                    cmd.ExecuteNonQuery();

                    TempData["SuccessMessage"] =
                        "User \"" +
                        NamaLengkap +
                        "\" berhasil ditambahkan.";
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

        // =============================================
        // POST: Admin/Edit
        // =============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(
            int Id,
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

                    // Cek username user lain
                    string cekQuery = @"
                        SELECT COUNT(*)
                        FROM Users
                        WHERE Username = @Username
                        AND Id <> @Id";

                    SqlCommand cekCmd =
                        new SqlCommand(cekQuery, conn);

                    cekCmd.Parameters.AddWithValue(
                        "@Username",
                        Username);

                    cekCmd.Parameters.AddWithValue(
                        "@Id",
                        Id);

                    int jumlah =
                        (int)cekCmd.ExecuteScalar();

                    if (jumlah > 0)
                    {
                        TempData["ErrorMessage"] =
                            "Username \"" +
                            Username +
                            "\" sudah digunakan user lain.";

                        return RedirectToAction("User");
                    }

                    string query;

                    if (!string.IsNullOrEmpty(Password))
                    {
                        query = @"
                            UPDATE Users
                            SET NamaLengkap = @NamaLengkap,
                                Username = @Username,
                                Email = @Email,
                                Password = @Password,
                                Role = @Role,
                                Status = @Status
                            WHERE Id = @Id";
                    }
                    else
                    {
                        query = @"
                            UPDATE Users
                            SET NamaLengkap = @NamaLengkap,
                                Username = @Username,
                                Email = @Email,
                                Role = @Role,
                                Status = @Status
                            WHERE Id = @Id";
                    }

                    SqlCommand cmd =
                        new SqlCommand(query, conn);

                    cmd.Parameters.AddWithValue("@Id", Id);
                    cmd.Parameters.AddWithValue("@NamaLengkap", NamaLengkap);
                    cmd.Parameters.AddWithValue("@Username", Username);
                    cmd.Parameters.AddWithValue("@Email", Email);
                    cmd.Parameters.AddWithValue("@Role", Role);
                    cmd.Parameters.AddWithValue("@Status", Status);

                    if (!string.IsNullOrEmpty(Password))
                    {
                        cmd.Parameters.AddWithValue(
                            "@Password",
                            Password);
                    }

                    cmd.ExecuteNonQuery();

                    TempData["SuccessMessage"] =
                        "User \"" +
                        NamaLengkap +
                        "\" berhasil diperbarui.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Gagal memperbarui user: " +
                    ex.Message;
            }

            return RedirectToAction("User");
        }

        // =============================================
        // POST: Admin/Delete
        // =============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int Id)
        {
            var access = CheckAccess();
            if (access != null)
                return access;

            try
            {
                // Tidak boleh hapus akun sendiri
                if (Session["Id"] != null &&
                    Convert.ToInt32(Session["Id"]) == Id)
                {
                    TempData["ErrorMessage"] =
                        "Tidak dapat menghapus akun yang sedang digunakan.";

                    return RedirectToAction("User");
                }

                using (SqlConnection conn =
                    new SqlConnection(connStr))
                {
                    conn.Open();

                    string query =
                        "DELETE FROM Users WHERE Id = @Id";

                    SqlCommand cmd =
                        new SqlCommand(query, conn);

                    cmd.Parameters.AddWithValue("@Id", Id);
                    cmd.ExecuteNonQuery();

                    TempData["SuccessMessage"] =
                        "User berhasil dihapus.";
                }
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
        // GET: Admin/Laporan
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