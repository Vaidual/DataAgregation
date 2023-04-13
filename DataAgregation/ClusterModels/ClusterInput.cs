using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAgregation.ClusterModels
{
    public class ClusterInput
    {
        public float Value { get; set; }
        public ClusterInput(float value)
        {
            Value = value;
        }
    }
}
