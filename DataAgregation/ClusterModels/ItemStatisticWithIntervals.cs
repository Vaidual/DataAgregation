using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAgregation.ClusterModels
{
    public class ItemStatisticWithIntervals
    {
        public string ItemName { get; set; }
        public List<int> Amount { get; set; }
        public List<int> Income { get; set; }
        public List<decimal> USD { get; set; }
    }
}
