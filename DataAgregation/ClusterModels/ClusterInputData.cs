using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAgregation.ClusterModels
{
    public class ClusterInputData
    {
        public float Value { get; set; }
        public ClusterInputData(float value)
        {
            this.Value = value;
        }
    }
}
