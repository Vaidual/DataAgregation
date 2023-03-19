using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAgregation.DataManipulationModels
{
    public class ItemStatistic
    {
        public string Item { get; set; }
        public int Amount { get; set; }
        public int Income { get; set; }
        public decimal USD { get; set; }
    }
}
