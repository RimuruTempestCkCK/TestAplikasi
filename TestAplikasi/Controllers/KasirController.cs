using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using TestAplikasi.Models;

namespace TestAplikasi.Controllers
{
    public class KasirController : Controller
    {
        private readonly string connStr =
            ConfigurationManager.ConnectionStrings["DBConnection"].ConnectionString;

        private bool IsLoggedIn() => Session["Id"] != null;
        private bool IsKasir() => Session["Role"] != null && string.Equals(Session["Role"].ToString(), "Kasir", StringComparison.OrdinalIgnoreCase);

        private ActionResult CheckAccess()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!IsKasir()) return RedirectToAction("Dashboard", "Admin");
            return null;
        }

        // =============================================
        // DASHBOARD
        // =============================================
        public ActionResult Dashboard()
        {
            var access = CheckAccess();
            if (access != null) return access;

            int kasirId = Convert.ToInt32(Session["Id"]);

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    // Transaksi hari ini (kasir ini)
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT COUNT(*) FROM dbo.Transaksi
                        WHERE KasirId = @KasirId AND CAST(TanggalTransaksi AS DATE) = CAST(GETDATE() AS DATE)", conn))
                    {
                        cmd.Parameters.AddWithValue("@KasirId", kasirId);
                        ViewBag.TxHariIni = (int)cmd.ExecuteScalar();
                    }

                    // Total pendapatan hari ini (kasir ini)
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT ISNULL(SUM(TotalHarga),0) FROM dbo.Transaksi
                        WHERE KasirId = @KasirId AND CAST(TanggalTransaksi AS DATE) = CAST(GETDATE() AS DATE)", conn))
                    {
                        cmd.Parameters.AddWithValue("@KasirId", kasirId);
                        ViewBag.PendapatanHariIni = (decimal)cmd.ExecuteScalar();
                    }

                    // Total transaksi semua waktu (kasir ini)
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT COUNT(*) FROM dbo.Transaksi WHERE KasirId = @KasirId", conn))
                    {
                        cmd.Parameters.AddWithValue("@KasirId", kasirId);
                        ViewBag.TotalTx = (int)cmd.ExecuteScalar();
                    }

                    // Total pendapatan semua waktu (kasir ini)
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT ISNULL(SUM(TotalHarga),0) FROM dbo.Transaksi WHERE KasirId = @KasirId", conn))
                    {
                        cmd.Parameters.AddWithValue("@KasirId", kasirId);
                        ViewBag.TotalPendapatan = (decimal)cmd.ExecuteScalar();
                    }

                    // Data grafik: transaksi 7 hari terakhir
                    var labelGrafik = new List<string>();
                    var dataGrafik = new List<decimal>();

                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT CAST(TanggalTransaksi AS DATE) AS Tgl,
                               ISNULL(SUM(TotalHarga),0) AS Total
                        FROM dbo.Transaksi
                        WHERE KasirId = @KasirId
                          AND TanggalTransaksi >= DATEADD(DAY, -6, CAST(GETDATE() AS DATE))
                        GROUP BY CAST(TanggalTransaksi AS DATE)
                        ORDER BY Tgl", conn))
                    {
                        cmd.Parameters.AddWithValue("@KasirId", kasirId);
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

                    // 10 transaksi terakhir (kasir ini)
                    var listTx = new List<TransaksiModel>();
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT TOP 10 Id, NomorTransaksi, TotalHarga, TanggalTransaksi
                        FROM dbo.Transaksi
                        WHERE KasirId = @KasirId
                        ORDER BY TanggalTransaksi DESC", conn))
                    {
                        cmd.Parameters.AddWithValue("@KasirId", kasirId);
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                listTx.Add(new TransaksiModel
                                {
                                    Id = Convert.ToInt32(r["Id"]),
                                    NomorTransaksi = r["NomorTransaksi"].ToString(),
                                    TotalHarga = Convert.ToDecimal(r["TotalHarga"]),
                                    TanggalTransaksi = Convert.ToDateTime(r["TanggalTransaksi"])
                                });
                            }
                        }
                    }
                    ViewBag.ListTxTerakhir = listTx;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal memuat dashboard: " + ex.Message;
            }

            return View();
        }

        // =============================================
        // TRANSAKSI - GET (halaman kasir)
        // =============================================
        public ActionResult Transaksi()
        {
            var access = CheckAccess();
            if (access != null) return access;

            var listProduk = new List<ProdukModel>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string q = @"
                        SELECT p.Id, p.NamaProduk, p.HargaJual,
                               ISNULL(SUM(s.Jumlah),0) AS JumlahTersedia
                        FROM dbo.Produk p
                        LEFT JOIN dbo.StokProduk s ON s.ProdukId = p.Id
                        GROUP BY p.Id, p.NamaProduk, p.HargaJual
                        HAVING ISNULL(SUM(s.Jumlah),0) > 0
                        ORDER BY p.NamaProduk";

                    using (SqlCommand cmd = new SqlCommand(q, conn))
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            listProduk.Add(new ProdukModel
                            {
                                Id = Convert.ToInt32(r["Id"]),
                                NamaProduk = r["NamaProduk"].ToString(),
                                HargaJual = Convert.ToDecimal(r["HargaJual"]),
                                JumlahTersedia = Convert.ToInt32(r["JumlahTersedia"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal memuat produk: " + ex.Message;
            }

            ViewBag.ListProduk = listProduk;
            return View();
        }

        // =============================================
        // TRANSAKSI - POST (simpan)
        // =============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SimpanTransaksi(
            string produkIds,
            string jumlahs,
            decimal bayar)
        {
            var access = CheckAccess();
            if (access != null) return access;

            try
            {
                // Parse array produk & jumlah
                var ids = produkIds.Split(',');
                var jmls = jumlahs.Split(',');

                if (ids.Length == 0 || ids[0] == "")
                {
                    TempData["ErrorMessage"] = "Pilih minimal satu produk.";
                    return RedirectToAction("Transaksi");
                }

                int kasirId = Convert.ToInt32(Session["Id"]);
                string nomorTx = "TRX-" + DateTime.Now.ToString("yyyyMMddHHmmss");

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    SqlTransaction dbTx = conn.BeginTransaction();

                    try
                    {
                        decimal total = 0;
                        var details = new List<(int produkId, string nama, decimal harga, int jumlah)>();

                        // Validasi stok & hitung total
                        for (int i = 0; i < ids.Length; i++)
                        {
                            int produkId = Convert.ToInt32(ids[i]);
                            int jumlah = Convert.ToInt32(jmls[i]);

                            string namaProduk = "";
                            decimal hargaJual = 0;
                            int stokTersedia = 0;

                            using (SqlCommand cmd = new SqlCommand(@"
                                SELECT p.NamaProduk, p.HargaJual,
                                       ISNULL(SUM(s.Jumlah),0) AS Stok
                                FROM dbo.Produk p
                                LEFT JOIN dbo.StokProduk s ON s.ProdukId = p.Id
                                WHERE p.Id = @Id
                                GROUP BY p.NamaProduk, p.HargaJual", conn, dbTx))
                            {
                                cmd.Parameters.AddWithValue("@Id", produkId);
                                using (SqlDataReader r = cmd.ExecuteReader())
                                {
                                    if (r.Read())
                                    {
                                        namaProduk = r["NamaProduk"].ToString();
                                        hargaJual = Convert.ToDecimal(r["HargaJual"]);
                                        stokTersedia = Convert.ToInt32(r["Stok"]);
                                    }
                                }
                            }

                            if (stokTersedia < jumlah)
                            {
                                dbTx.Rollback();
                                TempData["ErrorMessage"] = $"Stok {namaProduk} tidak cukup. Tersedia: {stokTersedia}";
                                return RedirectToAction("Transaksi");
                            }

                            total += hargaJual * jumlah;
                            details.Add((produkId, namaProduk, hargaJual, jumlah));
                        }

                        decimal kembalian = bayar - total;

                        // Insert header transaksi
                        int txId;
                        using (SqlCommand cmd = new SqlCommand(@"
                            INSERT INTO dbo.Transaksi (NomorTransaksi, KasirId, TotalHarga, Bayar, Kembalian, TanggalTransaksi)
                            VALUES (@Nomor, @KasirId, @Total, @Bayar, @Kembalian, GETDATE());
                            SELECT SCOPE_IDENTITY();", conn, dbTx))
                        {
                            cmd.Parameters.AddWithValue("@Nomor", nomorTx);
                            cmd.Parameters.AddWithValue("@KasirId", kasirId);
                            cmd.Parameters.AddWithValue("@Total", total);
                            cmd.Parameters.AddWithValue("@Bayar", bayar);
                            cmd.Parameters.AddWithValue("@Kembalian", kembalian);
                            txId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // Insert detail & kurangi stok
                        foreach (var d in details)
                        {
                            // Detail transaksi
                            using (SqlCommand cmd = new SqlCommand(@"
                                INSERT INTO dbo.TransaksiDetail
                                (TransaksiId, ProdukId, NamaProduk, HargaJual, Jumlah, Subtotal)
                                VALUES (@TxId, @ProdukId, @NamaProduk, @Harga, @Jumlah, @Subtotal)", conn, dbTx))
                            {
                                cmd.Parameters.AddWithValue("@TxId", txId);
                                cmd.Parameters.AddWithValue("@ProdukId", d.produkId);
                                cmd.Parameters.AddWithValue("@NamaProduk", d.nama);
                                cmd.Parameters.AddWithValue("@Harga", d.harga);
                                cmd.Parameters.AddWithValue("@Jumlah", d.jumlah);
                                cmd.Parameters.AddWithValue("@Subtotal", d.harga * d.jumlah);
                                cmd.ExecuteNonQuery();
                            }

                            // Kurangi stok (masukkan entry negatif di StokProduk)
                            using (SqlCommand cmd = new SqlCommand(@"
                                INSERT INTO dbo.StokProduk (ProdukId, Jumlah, Keterangan, TanggalMasuk)
                                VALUES (@ProdukId, @Jumlah, @Ket, GETDATE())", conn, dbTx))
                            {
                                cmd.Parameters.AddWithValue("@ProdukId", d.produkId);
                                cmd.Parameters.AddWithValue("@Jumlah", -d.jumlah);  // negatif = keluar
                                cmd.Parameters.AddWithValue("@Ket", "Penjualan #" + nomorTx);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        dbTx.Commit();
                        TempData["SuccessMessage"] = "Transaksi berhasil disimpan.";
                        TempData["LastTxId"] = txId;
                        return RedirectToAction("StrukTransaksi", new { id = txId });
                    }
                    catch
                    {
                        dbTx.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal menyimpan transaksi: " + ex.Message;
                return RedirectToAction("Transaksi");
            }
        }

        // =============================================
        // STRUK TRANSAKSI
        // =============================================
        public ActionResult StrukTransaksi(int id)
        {
            var access = CheckAccess();
            if (access != null) return access;

            TransaksiModel model = null;

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT t.Id, t.NomorTransaksi, t.TotalHarga, t.Bayar, t.Kembalian,
                               t.TanggalTransaksi, u.NamaLengkap AS NamaKasir
                        FROM dbo.Transaksi t
                        INNER JOIN dbo.Users u ON u.Id = t.KasirId
                        WHERE t.Id = @Id", conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                model = new TransaksiModel
                                {
                                    Id = Convert.ToInt32(r["Id"]),
                                    NomorTransaksi = r["NomorTransaksi"].ToString(),
                                    TotalHarga = Convert.ToDecimal(r["TotalHarga"]),
                                    Bayar = Convert.ToDecimal(r["Bayar"]),
                                    Kembalian = Convert.ToDecimal(r["Kembalian"]),
                                    TanggalTransaksi = Convert.ToDateTime(r["TanggalTransaksi"]),
                                    NamaKasir = r["NamaKasir"].ToString(),
                                    Details = new List<TransaksiDetailModel>()
                                };
                            }
                        }
                    }

                    if (model != null)
                    {
                        using (SqlCommand cmd = new SqlCommand(@"
                            SELECT NamaProduk, HargaJual, Jumlah, Subtotal
                            FROM dbo.TransaksiDetail
                            WHERE TransaksiId = @TxId", conn))
                        {
                            cmd.Parameters.AddWithValue("@TxId", model.Id);
                            using (SqlDataReader r = cmd.ExecuteReader())
                            {
                                while (r.Read())
                                {
                                    model.Details.Add(new TransaksiDetailModel
                                    {
                                        NamaProduk = r["NamaProduk"].ToString(),
                                        HargaJual = Convert.ToDecimal(r["HargaJual"]),
                                        Jumlah = Convert.ToInt32(r["Jumlah"]),
                                        Subtotal = Convert.ToDecimal(r["Subtotal"])
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal memuat struk: " + ex.Message;
            }

            return View(model);
        }

        // =============================================
        // RIWAYAT TRANSAKSI
        // =============================================
        public ActionResult RiwayatTransaksi()
        {
            var access = CheckAccess();
            if (access != null) return access;

            var list = new List<TransaksiModel>();
            int kasirId = Convert.ToInt32(Session["Id"]);

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT Id, NomorTransaksi, TotalHarga, Bayar, Kembalian, TanggalTransaksi
                        FROM dbo.Transaksi
                        WHERE KasirId = @KasirId
                        ORDER BY TanggalTransaksi DESC", conn))
                    {
                        cmd.Parameters.AddWithValue("@KasirId", kasirId);
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                list.Add(new TransaksiModel
                                {
                                    Id = Convert.ToInt32(r["Id"]),
                                    NomorTransaksi = r["NomorTransaksi"].ToString(),
                                    TotalHarga = Convert.ToDecimal(r["TotalHarga"]),
                                    Bayar = Convert.ToDecimal(r["Bayar"]),
                                    Kembalian = Convert.ToDecimal(r["Kembalian"]),
                                    TanggalTransaksi = Convert.ToDateTime(r["TanggalTransaksi"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal memuat riwayat: " + ex.Message;
            }

            return View(list);
        }
    }
}