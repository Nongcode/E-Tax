using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E_Tax.Models
{
    public class InvoiceDetail
    {
        public string nbmst { get; set; }
        public string khhdon { get; set; }
        public int shdon { get; set; }
        public int khmshdon { get; set; }
        public string mhdon { get; set; }
        public string nmten { get; set; }
        public string nbten { get; set; }
        public string nmmst { get; set; }
        public string nbdchi { get; set; }
        public string nmdchi { get; set; }
        public double? tgtcthue { get; set; }
        public double? tgtthue { get; set; }
        public double? tgtttbso { get; set; }
        public string tgtttbchu { get; set; }
        public string tdlap { get; set; }
        public string tthai { get; set; }
        public string thdon { get; set; }

        // Mở rộng thêm nếu có
        public List<ItemDetail>? hhdv { get; set; }
    }

    public class ItemDetail
    {
        public string ten { get; set; }
        public double? sluong { get; set; }
        public double? dgia { get; set; }
        public double? ttiendmuc { get; set; }
        public string tsuat { get; set; }
        public double? tthue { get; set; }
    }

}
