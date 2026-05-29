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

        public decimal Harga { get; set; }

        public string Deskripsi { get; set; }
    }
}