using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V275_REST_lib.Models
{
    public class Symbologies
    {
        public class Symbol
        {
            public string symbolType { get; set; }
            public string symbology { get; set; }
            public string regionType { get; set; }
            public bool directional { get; set; }
        }

    }
}
