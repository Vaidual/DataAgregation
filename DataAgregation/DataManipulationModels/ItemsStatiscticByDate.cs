using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAgregation.DataManipulationModels
{
    internal class ItemsStatiscticByDate
    {
        public DateOnly Date { get; set; }
        public int SoldAmount { get; set; }
        public decimal Income { get; set; }
    }
}
