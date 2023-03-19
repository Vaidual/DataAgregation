using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAgregation.DataManipulationModels
{
    public class StageStatistic
    {
        public int Stage { get; set; }
        public int Starts { get; set; }
        public int Ends { get; set; }
        public int Wins { get; set; }
        public int Income { get; set; }
        public decimal USD { get; set; }
    }
}
