using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V275_REST_Lib.Models
{
    public class SimulationTrigger
    {
        public byte[]? image { get; set; }
        public long size => image.Length;
        public uint dpi { get; set; }
    }
}
