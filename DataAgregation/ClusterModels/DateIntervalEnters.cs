using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAgregation.ClusterModels
{
    public class DateIntervalEnters<T>
    {
        public DateOnly Date { get; set; }
        public T IntervalEnters1 { get; set; }
        public T IntervalEnters2 { get; set; }
        public T IntervalEnters3 { get; set; }
        public T IntervalEnters4 { get; set; }
    }
}
