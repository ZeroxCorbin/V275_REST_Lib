using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V725_REST_lib.Models
{
    public class Product
    {
        public string name { get; set; }
        public string part { get; set; }
        public Version version { get; set; }
        public string compileDate { get; set; }

        public class Version
        {
            public int major { get; set; }
            public int minor { get; set; }
            public int service { get; set; }
            public int build { get; set; }
        }

    }
}
