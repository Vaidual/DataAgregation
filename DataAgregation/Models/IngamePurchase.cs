using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAgregation.Models
{
    public class IngamePurchase
    {
        public int IngamePurchaseId { get; set; }
        public string ItemName { get; set; }
        public int Price { get; set; }

        public int EventId { get; set; }
        public Event Event { get; set; }
    }
}
