using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAgregation.ClusterModels
{
    public class DateListIntervalEnters<T>
    {
        public DateOnly Date { get; set; }
        public List<T> IntervalEnters { get; set; }
    }
}
