using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAgregation.Models
{
    public class Event
    {
        public int EventId { get; set; }
        public int EventType { get; set; }
        public DateTime DateTime { get; set; }

        public string UserId { get; set; }
        public User User { get; set; }

        public List<CurrencyPurchase> CurrencyPurchases { get; set; }
        public List<IngamePurchase> IngamePurchases { get; set; }
        public List<StageStart> StageStarts { get; set; }
        public List<StageEnd> StageEnds { get; set; }
    }

}
