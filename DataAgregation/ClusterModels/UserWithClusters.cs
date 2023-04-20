using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAgregation.ClusterModels
{
    public class UserWithClusters
    {
        public string UserId { get; set; }
        public string Gender { get; set; }
        public int Age { get; set; }
        public string? Country { get; set; }
        public bool IsCheater { get; set;}
        public int IncomeTier { get; set; }
        public string AgeInterval { get; set; }
    }
}
