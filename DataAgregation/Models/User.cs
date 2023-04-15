using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAgregation.Models
{
    public class User
    {
        public string UserId { get; set; }
        public string Gender { get; set; }
        public int Age { get; set; }
        public string? Country { get; set; }

        public List<Event> Events { get; set; }
    }
}
