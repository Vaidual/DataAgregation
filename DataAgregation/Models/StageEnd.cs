using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAgregation.Models
{
    public class StageEnd
    {
        public int StageEndId { get; set; }
        public int Stage { get; set; }
        public bool IsWon { get; set; }
        public int Time { get; set; }
        public int? Income { get; set; }

        public int EventId { get; set; }
        public Event Event { get; set; }
    }
}
