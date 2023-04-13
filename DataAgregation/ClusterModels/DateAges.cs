using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAgregation.ClusterModels
{
    public class DateAges
    {
        public DateOnly Date { get; set; }
        public IEnumerable<int> Ages { get; set; }
    }
}
