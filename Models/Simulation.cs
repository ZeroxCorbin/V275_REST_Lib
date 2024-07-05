using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V275_REST_Lib.Models
{
    public class Simulation
    {

        public string? loadPath { get; set; }
        public string? mode { get; set; }
        public int dwellMs { get; set; }
        public bool sectorROI { get; set; }


    }
}
