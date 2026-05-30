using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TestAplikasi.Models
{
    public class ProdukModel
    {
        public int Id { get; set; }
        public string NamaProduk { get; set; }
        public decimal Harga { get; set; }       // lama — bisa tetap untuk kompatibilitas
        public decimal HargaModal { get; set; }
        public decimal HargaJual { get; set; }
        public string Deskripsi { get; set; }
        public int JumlahTersedia { get; set; }  // dari SUM StokProduk
    }
}