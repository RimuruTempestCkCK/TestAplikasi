using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TestAplikasi.Models
{
    public class TransaksiModel
    {
        public int Id { get; set; }
        public string NomorTransaksi { get; set; }
        public int KasirId { get; set; }
        public string NamaKasir { get; set; }
        public decimal TotalHarga { get; set; }
        public decimal Bayar { get; set; }
        public decimal Kembalian { get; set; }
        public DateTime TanggalTransaksi { get; set; }
        public List<TransaksiDetailModel> Details { get; set; }
    }

    public class TransaksiDetailModel
    {
        public int Id { get; set; }
        public int TransaksiId { get; set; }
        public int ProdukId { get; set; }
        public string NamaProduk { get; set; }
        public decimal HargaJual { get; set; }
        public int Jumlah { get; set; }
        public decimal Subtotal { get; set; }
    }
}