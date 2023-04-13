using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAgregation.ClusterModels
{
    public class UserCluster
    {
        public string UserId { get; set; }
        public float Value { get; set; }
        public UserCluster(string userId, float value)
        {
            UserId = userId;
            Value = value;
        }
    }
}
