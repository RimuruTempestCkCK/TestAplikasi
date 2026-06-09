using System;

namespace TestAplikasi.Models
{
    public class LaporanTransaksiModel
    {
        public int Id { get; set; }
        public string NomorTransaksi { get; set; }
        public DateTime TanggalTransaksi { get; set; }
        public string NamaKasir { get; set; }
        public decimal TotalHarga { get; set; }
        public decimal Bayar { get; set; }
        public decimal Kembalian { get; set; }
    }

    public class LaporanStokModel
    {
        public int Id { get; set; }
        public string NamaProduk { get; set; }
        public decimal Harga { get; set; }
        public decimal HargaModal { get; set; }
        public decimal HargaJual { get; set; }
        public int TotalStok { get; set; }
        public decimal NilaiStok { get; set; }
    }

    public class LaporanPenambahanStokModel
    {
        public int Id { get; set; }
        public string NamaProduk { get; set; }
        public int Jumlah { get; set; }
        public string Keterangan { get; set; }
        public DateTime TanggalMasuk { get; set; }
        public decimal Harga { get; set; }
        public decimal NilaiStok { get; set; }
    }

    public class DashboardProdukModel
    {
        public string NamaProduk { get; set; }
        public int TotalTerjual { get; set; }
        public decimal TotalPendapatan { get; set; }
    }

    public class DashboardKasirModel
    {
        public string NamaKasir { get; set; }
        public int JumlahTransaksi { get; set; }
        public decimal TotalPendapatan { get; set; }
    }
}
