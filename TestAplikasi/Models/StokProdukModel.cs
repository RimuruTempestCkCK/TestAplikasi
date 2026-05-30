using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TestAplikasi.Models
{
    public class StokProdukModel
    {
        public int Id { get; set; }
        public int ProdukId { get; set; }
        public string NamaProduk { get; set; }
        public int Jumlah { get; set; }
        public string Keterangan { get; set; }
        public DateTime TanggalMasuk { get; set; }
    }
}