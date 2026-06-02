using System;
using System.Collections.Generic;

namespace TestAplikasi.Models
{
    public class TopProdukModel
    {
        public string NamaProduk { get; set; }
        public int TotalTerjual { get; set; }
        public decimal TotalPendapatan { get; set; }
    }

    public class PerformaKasirModel
    {
        public string NamaKasir { get; set; }
        public int JumlahTransaksi { get; set; }
        public decimal TotalPendapatan { get; set; }
    }
}
