using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAgregation.Models
{
    public class StageStart
    {
        public int Id { get; set; }
        public int Stage { get; set; }

        public int EventId { get; set; }
        public Event Event { get; set; }
    }
}
