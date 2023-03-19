using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAgregation.DataManipulationModels
{
    public class CurrencyRateData
    {
        public DateOnly Date { get; set; }
        public decimal Income { get; set; }
        public decimal BoughtCurrency { get; set; }
    }
}
