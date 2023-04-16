using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAgregation.ClusterModels
{
    public class StageStatisticWithIntervals
    {
        public int Stage { get; set; }
        public List<int> Starts { get; set; }
        public List<int> Ends { get; set; }
        public List<int> Wins { get; set; }
        public List<int> Income { get; set; }
        public List<decimal> USD { get; set; }
    }
}
