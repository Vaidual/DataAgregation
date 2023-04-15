using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAgregation.Models
{
    public class CurrencyPurchase
    {
        public int CurrencyPurchaseId { get; set; }
        public string PackName { get; set; }
        public decimal Price { get; set; }
        public int Income { get; set; }

        public int EventId { get; set; }
        public Event Event { get; set; }
    }
}
