using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace TestAplikasi.Controllers
{
    public class PimpinanController : Controller
    {
        private readonly string connStr =
            ConfigurationManager.ConnectionStrings["DBConnection"].ConnectionString;

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

            // Initialize default values
            ViewBag.TxHariIni = 0;
            ViewBag.PendapatanHariIni = 0m;
            ViewBag.TxBulanIni = 0;
            ViewBag.PendapatanBulanIni = 0m;
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

                    // Total Transaksi Bulan Ini
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT COUNT(*) FROM dbo.Transaksi
                        WHERE MONTH(TanggalTransaksi) = MONTH(GETDATE())
                          AND YEAR(TanggalTransaksi) = YEAR(GETDATE())", conn))
                    {
                        ViewBag.TxBulanIni = (int)cmd.ExecuteScalar();
                    }

                    // Total Pendapatan Bulan Ini
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT ISNULL(SUM(TotalHarga),0) FROM dbo.Transaksi
                        WHERE MONTH(TanggalTransaksi) = MONTH(GETDATE())
                          AND YEAR(TanggalTransaksi) = YEAR(GETDATE())", conn))
                    {
                        ViewBag.PendapatanBulanIni = (decimal)cmd.ExecuteScalar();
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

                    // Jumlah Kasir Aktif
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT COUNT(*) FROM dbo.Users WHERE Role = 'Kasir' AND Status = 'Aktif'", conn))
                    {
                        ViewBag.JumlahKasir = (int)cmd.ExecuteScalar();
                    }

                    // Data grafik: Transaksi 7 hari terakhir
                    var labelGrafik = new List<string>();
                    var dataGrafik = new List<decimal>();

                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT CAST(TanggalTransaksi AS DATE) AS Tgl,
                               ISNULL(SUM(TotalHarga),0) AS Total
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
                            }
                        }
                    }

                    ViewBag.LabelGrafik = Newtonsoft.Json.JsonConvert.SerializeObject(labelGrafik);
                    ViewBag.DataGrafik = Newtonsoft.Json.JsonConvert.SerializeObject(dataGrafik);

                    // Top 5 Produk Terjual
                    var topProduk = new List<dynamic>();
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
                                topProduk.Add(new
                                {
                                    NamaProduk = r["NamaProduk"].ToString(),
                                    TotalTerjual = Convert.ToInt32(r["TotalTerjual"]),
                                    TotalPendapatan = Convert.ToDecimal(r["TotalPendapatan"])
                                });
                            }
                        }
                    }
                    ViewBag.TopProduk = topProduk;

                    // Performa Kasir
                    var performaKasir = new List<dynamic>();
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT TOP 10 u.NamaLengkap,
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
                                performaKasir.Add(new
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