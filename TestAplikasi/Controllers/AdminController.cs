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

        private bool IsLoggedIn()
        {
            return Session["Id"] != null;
        }

        //private bool IsAdmin()
        //{
        //    return Session["Role"] != null &&
        //           Session["Role"].ToString() == "Admin";
        //}

        private bool CanAccessReport()
        {
            if (Session["Role"] == null)
                return false;

            string role = Session["Role"].ToString();

            return string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) || 
                   string.Equals(role, "Pimpinan", StringComparison.OrdinalIgnoreCase);
        }

        //private ActionResult CheckAccess()
        //{
        //    if (!IsLoggedIn())
        //        return RedirectToAction("Login", "Account");

        //    if (!IsAdmin())
        //        return RedirectToAction("Dashboard", "Pimpinan");

        //    return null;
        //}

        private ActionResult CheckAccess()
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            if (!CanAccessReport())
                return RedirectToAction("Dashboard", "Kasir");

            return null;
        }

        // =============================================
        // Dashboard
        // =============================================
        public ActionResult Dashboard()
        {
            var access = CheckAccess();
            if (access != null) return access;

            // Initialize default values
            ViewBag.TxHariIni = 0;
            ViewBag.PendapatanHariIni = 0m;
            ViewBag.TotalTx = 0;
            ViewBag.TotalPendapatan = 0m;
            ViewBag.TotalProduk = 0;
            ViewBag.TotalStok = 0;
            ViewBag.JumlahKasir = 0;
            ViewBag.LabelGrafik = "[]";
            ViewBag.DataGrafik = "[]";
            ViewBag.TopProduk = new List<dynamic>();
            ViewBag.PerformaKasir = new List<dynamic>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    // Total Transaksi Hari Ini
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT COUNT(*) FROM dbo.Transaksi
                        WHERE CAST(TanggalTransaksi AS DATE) = CAST(GETDATE() AS DATE)", conn))
                    {
                        ViewBag.TxHariIni = (int)cmd.ExecuteScalar();
                    }

                    // Total Pendapatan Hari Ini
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT ISNULL(SUM(TotalHarga),0) FROM dbo.Transaksi
                        WHERE CAST(TanggalTransaksi AS DATE) = CAST(GETDATE() AS DATE)", conn))
                    {
                        ViewBag.PendapatanHariIni = (decimal)cmd.ExecuteScalar();
                    }

                    // Total Transaksi Semua Waktu
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT COUNT(*) FROM dbo.Transaksi", conn))
                    {
                        ViewBag.TotalTx = (int)cmd.ExecuteScalar();
                    }

                    // Total Pendapatan Semua Waktu
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT ISNULL(SUM(TotalHarga),0) FROM dbo.Transaksi", conn))
                    {
                        ViewBag.TotalPendapatan = (decimal)cmd.ExecuteScalar();
                    }

                    // Total Produk
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT COUNT(*) FROM dbo.Produk", conn))
                    {
                        ViewBag.TotalProduk = (int)cmd.ExecuteScalar();
                    }

                    // Total Stok
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT ISNULL(SUM(Jumlah),0) FROM dbo.StokProduk", conn))
                    {
                        ViewBag.TotalStok = (int)cmd.ExecuteScalar();
                    }

                    // Jumlah Kasir
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT COUNT(*) FROM dbo.Users WHERE Role = 'Kasir'", conn))
                    {
                        ViewBag.JumlahKasir = (int)cmd.ExecuteScalar();
                    }

                    // Stok Menipis (Kurang dari 10)
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT COUNT(*) FROM dbo.StokProduk WHERE Jumlah < 10", conn))
                    {
                        ViewBag.StokMenipis = (int)cmd.ExecuteScalar();
                    }

                    // Data grafik: Transaksi 7 hari terakhir (semua kasir)
                    var labelGrafik = new List<string>();
                    var dataGrafik = new List<decimal>();
                    var dataTxGrafik = new List<int>();

                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT CAST(TanggalTransaksi AS DATE) AS Tgl,
                               ISNULL(SUM(TotalHarga),0) AS Total,
                               COUNT(*) AS JmlTx
                        FROM dbo.Transaksi
                        WHERE TanggalTransaksi >= DATEADD(DAY, -6, CAST(GETDATE() AS DATE))
                        GROUP BY CAST(TanggalTransaksi AS DATE)
                        ORDER BY Tgl", conn))
                    {
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                labelGrafik.Add(Convert.ToDateTime(r["Tgl"]).ToString("dd MMM"));
                                dataGrafik.Add(Convert.ToDecimal(r["Total"]));
                                dataTxGrafik.Add(Convert.ToInt32(r["JmlTx"]));
                            }
                        }
                    }

                    ViewBag.LabelGrafik = Newtonsoft.Json.JsonConvert.SerializeObject(labelGrafik);
                    ViewBag.DataGrafik = Newtonsoft.Json.JsonConvert.SerializeObject(dataGrafik);
                    ViewBag.DataTxGrafik = Newtonsoft.Json.JsonConvert.SerializeObject(dataTxGrafik);

                    // Top 5 Produk Terjual (berdasarkan jumlah penjualan)
                    var topProduk = new List<DashboardProdukModel>();
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT TOP 5 td.NamaProduk, 
                               SUM(td.Jumlah) AS TotalTerjual,
                               SUM(td.Subtotal) AS TotalPendapatan
                        FROM dbo.TransaksiDetail td
                        GROUP BY td.NamaProduk
                        ORDER BY TotalTerjual DESC", conn))
                    {
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                topProduk.Add(new DashboardProdukModel
                                {
                                    NamaProduk = r["NamaProduk"].ToString(),
                                    TotalTerjual = Convert.ToInt32(r["TotalTerjual"]),
                                    TotalPendapatan = Convert.ToDecimal(r["TotalPendapatan"])
                                });
                            }
                        }
                    }
                    ViewBag.TopProduk = topProduk;

                    // Performa Kasir (Top 5 kasir berdasarkan transaksi)
                    var performaKasir = new List<DashboardKasirModel>();
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT TOP 5 u.NamaLengkap,
                               COUNT(t.Id) AS JumlahTransaksi,
                               ISNULL(SUM(t.TotalHarga),0) AS TotalPendapatan
                        FROM dbo.Transaksi t
                        JOIN dbo.Users u ON u.Id = t.KasirId
                        GROUP BY u.NamaLengkap
                        ORDER BY JumlahTransaksi DESC", conn))
                    {
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                performaKasir.Add(new DashboardKasirModel
                                {
                                    NamaKasir = r["NamaLengkap"].ToString(),
                                    JumlahTransaksi = Convert.ToInt32(r["JumlahTransaksi"]),
                                    TotalPendapatan = Convert.ToDecimal(r["TotalPendapatan"])
                                });
                            }
                        }
                    }
                    ViewBag.PerformaKasir = performaKasir;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal memuat dashboard: " + ex.Message;
            }

            return View();
        }

        public ActionResult Debug()
        {
            return Content("Admin Controller is working!");
        }

        // =============================================
        // USER - GET
        // =============================================
        public ActionResult ManageUser()
        {
            var access = CheckAccess();
            if (access != null) return access;

            var listUser = new List<UserModel>();

            try
            {
                using (SqlConnection conn =
                    new SqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                        SELECT Id, NamaLengkap, Username,
                               Email, Role, Status
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

        // =============================================
        // USER - CREATE
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
            if (access != null) return access;

            try
            {
                using (SqlConnection conn =
                    new SqlConnection(connStr))
                {
                    conn.Open();

                    string cekQuery =
                        "SELECT COUNT(*) FROM dbo.Users WHERE Username = @Username";

                    using (SqlCommand cekCmd =
                        new SqlCommand(cekQuery, conn))
                    {
                        cekCmd.Parameters.AddWithValue("@Username", Username);
                        int jumlah = (int)cekCmd.ExecuteScalar();

                        if (jumlah > 0)
                        {
                            TempData["ErrorMessage"] =
                                "Username sudah digunakan.";
                            return RedirectToAction("ManageUser");
                        }
                    }

                    string query = @"
                        INSERT INTO dbo.Users
                        (NamaLengkap, Username, Email, Password, Role, Status)
                        VALUES
                        (@NamaLengkap, @Username, @Email, @Password, @Role, @Status)";

                    using (SqlCommand cmd =
                        new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@NamaLengkap", NamaLengkap);
                        cmd.Parameters.AddWithValue("@Username", Username);
                        cmd.Parameters.AddWithValue("@Email", Email ?? "");
                        cmd.Parameters.AddWithValue("@Password", Password);
                        cmd.Parameters.AddWithValue("@Role", Role);
                        cmd.Parameters.AddWithValue("@Status", Status);
                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["SuccessMessage"] = "User berhasil ditambahkan.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Gagal menambahkan user: " + ex.Message;
            }

            return RedirectToAction("ManageUser");
        }

        // =============================================
        // USER - EDIT
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
            if (access != null) return access;

            try
            {
                using (SqlConnection conn =
                    new SqlConnection(connStr))
                {
                    conn.Open();

                    // Cek username dipakai user lain
                    string cekQuery = @"
                        SELECT COUNT(*) FROM dbo.Users
                        WHERE Username = @Username AND Id <> @Id";

                    using (SqlCommand cekCmd =
                        new SqlCommand(cekQuery, conn))
                    {
                        cekCmd.Parameters.AddWithValue("@Username", Username);
                        cekCmd.Parameters.AddWithValue("@Id", Id);
                        int jumlah = (int)cekCmd.ExecuteScalar();

                        if (jumlah > 0)
                        {
                            TempData["ErrorMessage"] =
                                "Username sudah digunakan user lain.";
                            return RedirectToAction("ManageUser");
                        }
                    }

                    string query;

                    if (!string.IsNullOrEmpty(Password))
                    {
                        query = @"
                            UPDATE dbo.Users
                            SET NamaLengkap = @NamaLengkap,
                                Username    = @Username,
                                Email       = @Email,
                                Password    = @Password,
                                Role        = @Role,
                                Status      = @Status
                            WHERE Id = @Id";
                    }
                    else
                    {
                        query = @"
                            UPDATE dbo.Users
                            SET NamaLengkap = @NamaLengkap,
                                Username    = @Username,
                                Email       = @Email,
                                Role        = @Role,
                                Status      = @Status
                            WHERE Id = @Id";
                    }

                    using (SqlCommand cmd =
                        new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", Id);
                        cmd.Parameters.AddWithValue("@NamaLengkap", NamaLengkap);
                        cmd.Parameters.AddWithValue("@Username", Username);
                        cmd.Parameters.AddWithValue("@Email", Email ?? "");
                        cmd.Parameters.AddWithValue("@Role", Role);
                        cmd.Parameters.AddWithValue("@Status", Status);

                        if (!string.IsNullOrEmpty(Password))
                            cmd.Parameters.AddWithValue("@Password", Password);

                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["SuccessMessage"] = "User berhasil diperbarui.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Gagal memperbarui user: " + ex.Message;
            }

            return RedirectToAction("ManageUser");
        }

        // =============================================
        // USER - DELETE
        // =============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int Id)
        {
            var access = CheckAccess();
            if (access != null) return access;

            try
            {
                if (Session["Id"] != null &&
                    Convert.ToInt32(Session["Id"]) == Id)
                {
                    TempData["ErrorMessage"] =
                        "Tidak bisa menghapus akun sendiri.";
                    return RedirectToAction("ManageUser");
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

                TempData["SuccessMessage"] = "User berhasil dihapus.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Gagal menghapus user: " + ex.Message;
            }

            return RedirectToAction("ManageUser");
        }

        // =============================================
        // PRODUK - GET
        // =============================================
        public ActionResult Produk()
        {
            var access = CheckAccess();
            if (access != null) return access;

            var listProduk = new List<ProdukModel>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                SELECT p.Id, p.NamaProduk, p.HargaModal, p.HargaJual, p.Deskripsi,
                       ISNULL(SUM(s.Jumlah), 0) AS JumlahTersedia
                FROM dbo.Produk p
                LEFT JOIN dbo.StokProduk s ON s.ProdukId = p.Id
                GROUP BY p.Id, p.NamaProduk, p.HargaModal, p.HargaJual, p.Deskripsi
                ORDER BY p.Id DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            listProduk.Add(new ProdukModel
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                NamaProduk = reader["NamaProduk"]?.ToString(),
                                HargaModal = Convert.ToDecimal(reader["HargaModal"]),
                                HargaJual = Convert.ToDecimal(reader["HargaJual"]),
                                Deskripsi = reader["Deskripsi"]?.ToString(),
                                JumlahTersedia = Convert.ToInt32(reader["JumlahTersedia"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal memuat produk: " + ex.Message;
            }

            return View(listProduk);
        }

        // =============================================
        // PRODUK - CREATE
        // =============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateProduk(string NamaProduk, decimal HargaModal, decimal HargaJual, string Deskripsi)
        {
            var access = CheckAccess();
            if (access != null) return access;

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"
                INSERT INTO dbo.Produk (NamaProduk, HargaModal, HargaJual, Harga, Deskripsi)
                VALUES (@NamaProduk, @HargaModal, @HargaJual, @HargaJual, @Deskripsi)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@NamaProduk", NamaProduk);
                        cmd.Parameters.AddWithValue("@HargaModal", HargaModal);
                        cmd.Parameters.AddWithValue("@HargaJual", HargaJual);
                        cmd.Parameters.AddWithValue("@Deskripsi", Deskripsi ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }
                TempData["SuccessMessage"] = "Produk berhasil ditambahkan.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal tambah produk: " + ex.Message;
            }

            return RedirectToAction("Produk");
        }

        // =============================================
        // PRODUK - EDIT
        // =============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProduk(int Id, string NamaProduk, decimal HargaModal, decimal HargaJual, string Deskripsi)
        {
            var access = CheckAccess();
            if (access != null) return access;

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"
                UPDATE dbo.Produk
                SET NamaProduk = @NamaProduk,
                    HargaModal = @HargaModal,
                    HargaJual  = @HargaJual,
                    Harga      = @HargaJual,
                    Deskripsi  = @Deskripsi
                WHERE Id = @Id";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", Id);
                        cmd.Parameters.AddWithValue("@NamaProduk", NamaProduk);
                        cmd.Parameters.AddWithValue("@HargaModal", HargaModal);
                        cmd.Parameters.AddWithValue("@HargaJual", HargaJual);
                        cmd.Parameters.AddWithValue("@Deskripsi", Deskripsi ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }
                TempData["SuccessMessage"] = "Produk berhasil diperbarui.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal update produk: " + ex.Message;
            }

            return RedirectToAction("Produk");
        }

        // =============================================
        // PRODUK - DELETE
        // =============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteProduk(int Id)
        {
            var access = CheckAccess();
            if (access != null) return access;

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

                TempData["SuccessMessage"] = "Produk berhasil dihapus.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Gagal hapus produk: " + ex.Message;
            }

            return RedirectToAction("Produk");
        }

        // =============================================
        // STOK - GET
        // =============================================
        public ActionResult Stok(int? produkId)
        {
            var access = CheckAccess();
            if (access != null) return access;

            // Dropdown list produk
            var listProduk = new List<ProdukModel>();
            var listStok = new List<StokProdukModel>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    // Ambil semua produk untuk dropdown
                    string qProduk = "SELECT Id, NamaProduk FROM dbo.Produk ORDER BY NamaProduk";
                    using (SqlCommand cmd = new SqlCommand(qProduk, conn))
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            listProduk.Add(new ProdukModel
                            {
                                Id = Convert.ToInt32(r["Id"]),
                                NamaProduk = r["NamaProduk"]?.ToString()
                            });
                        }
                    }

                    // Riwayat stok — semua atau filter per produk
                    string qStok = @"
                SELECT s.Id, s.ProdukId, p.NamaProduk, s.Jumlah, s.Keterangan, s.TanggalMasuk
                FROM dbo.StokProduk s
                INNER JOIN dbo.Produk p ON p.Id = s.ProdukId
                " + (produkId.HasValue ? "WHERE s.ProdukId = @ProdukId " : "") + @"
                ORDER BY s.TanggalMasuk DESC";

                    using (SqlCommand cmd = new SqlCommand(qStok, conn))
                    {
                        if (produkId.HasValue)
                            cmd.Parameters.AddWithValue("@ProdukId", produkId.Value);

                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                listStok.Add(new StokProdukModel
                                {
                                    Id = Convert.ToInt32(r["Id"]),
                                    ProdukId = Convert.ToInt32(r["ProdukId"]),
                                    NamaProduk = r["NamaProduk"]?.ToString(),
                                    Jumlah = Convert.ToInt32(r["Jumlah"]),
                                    Keterangan = r["Keterangan"]?.ToString(),
                                    TanggalMasuk = Convert.ToDateTime(r["TanggalMasuk"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal memuat stok: " + ex.Message;
            }

            ViewBag.ListProduk = listProduk;
            ViewBag.FilterProdukId = produkId;
            return View(listStok);
        }

        // =============================================
        // STOK - TAMBAH
        // =============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TambahStok(int ProdukId, int Jumlah, string Keterangan)
        {
            var access = CheckAccess();
            if (access != null) return access;

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"
                INSERT INTO dbo.StokProduk (ProdukId, Jumlah, Keterangan, TanggalMasuk)
                VALUES (@ProdukId, @Jumlah, @Keterangan, GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ProdukId", ProdukId);
                        cmd.Parameters.AddWithValue("@Jumlah", Jumlah);
                        cmd.Parameters.AddWithValue("@Keterangan", Keterangan ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }
                TempData["SuccessMessage"] = "Stok berhasil ditambahkan.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal tambah stok: " + ex.Message;
            }

            return RedirectToAction("Stok", new { produkId = ProdukId });
        }




        // =============================================
        // LAPORAN TRANSAKSI
        // =============================================
        [HttpGet]
        public ActionResult LaporanTransaksi()
        {
            var access = CheckAccess();
            if (access != null) return access;

            var listTransaksi = new List<LaporanTransaksiModel>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                        SELECT t.Id, t.NomorTransaksi, t.TanggalTransaksi, 
                               u.NamaLengkap AS NamaKasir, t.TotalHarga, t.Bayar, t.Kembalian
                        FROM dbo.Transaksi t
                        JOIN dbo.Users u ON u.Id = t.KasirId
                        ORDER BY t.TanggalTransaksi DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                listTransaksi.Add(new LaporanTransaksiModel
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    NomorTransaksi = reader["NomorTransaksi"]?.ToString(),
                                    TanggalTransaksi = Convert.ToDateTime(reader["TanggalTransaksi"]),
                                    NamaKasir = reader["NamaKasir"]?.ToString(),
                                    TotalHarga = Convert.ToDecimal(reader["TotalHarga"]),
                                    Bayar = Convert.ToDecimal(reader["Bayar"]),
                                    Kembalian = Convert.ToDecimal(reader["Kembalian"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal memuat laporan transaksi: " + ex.Message;
            }

            ViewBag.DataLaporan = listTransaksi;
            return View("LaporanTransaksi");
        }

        // =============================================
        // LAPORAN STOK TERKINI
        // =============================================
        public ActionResult LaporanStok()
        {
            var access = CheckAccess();
            if (access != null) return access;

            var listStok = new List<LaporanStokModel>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                        SELECT p.Id, p.NamaProduk, p.Harga, p.HargaModal, p.HargaJual,
                               ISNULL(SUM(s.Jumlah), 0) AS TotalStok
                        FROM dbo.Produk p
                        LEFT JOIN dbo.StokProduk s ON s.ProdukId = p.Id
                        GROUP BY p.Id, p.NamaProduk, p.Harga, p.HargaModal, p.HargaJual
                        ORDER BY p.NamaProduk";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var totalStok = Convert.ToInt32(reader["TotalStok"]);
                                var harga = Convert.ToDecimal(reader["Harga"]);
                                
                                listStok.Add(new LaporanStokModel
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    NamaProduk = reader["NamaProduk"]?.ToString(),
                                    Harga = harga,
                                    HargaModal = Convert.ToDecimal(reader["HargaModal"]),
                                    HargaJual = Convert.ToDecimal(reader["HargaJual"]),
                                    TotalStok = totalStok,
                                    NilaiStok = harga * totalStok
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal memuat laporan stok: " + ex.Message;
            }

            ViewBag.DataLaporan = listStok;
            return View();
        }

        // =============================================
        // LAPORAN PENAMBAHAN STOK
        // =============================================
        public ActionResult LaporanPenambahStok()
        {
            var access = CheckAccess();
            if (access != null) return access;

            var listPenambahan = new List<LaporanPenambahanStokModel>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                        SELECT s.Id, p.NamaProduk, s.Jumlah, s.Keterangan, 
                               s.TanggalMasuk, p.Harga
                        FROM dbo.StokProduk s
                        JOIN dbo.Produk p ON p.Id = s.ProdukId
                        ORDER BY s.TanggalMasuk DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var jumlah = Convert.ToInt32(reader["Jumlah"]);
                                var harga = Convert.ToDecimal(reader["Harga"]);

                                listPenambahan.Add(new LaporanPenambahanStokModel
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    NamaProduk = reader["NamaProduk"]?.ToString(),
                                    Jumlah = jumlah,
                                    Keterangan = reader["Keterangan"]?.ToString(),
                                    TanggalMasuk = Convert.ToDateTime(reader["TanggalMasuk"]),
                                    Harga = harga,
                                    NilaiStok = harga * jumlah
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal memuat laporan penambahan stok: " + ex.Message;
            }

            ViewBag.DataLaporan = listPenambahan;
            return View();
        }
    }
}