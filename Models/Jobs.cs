using System;
using System.Collections.Generic;
using System.Text;

namespace V275_REST_Lib.Models
{
    public class Jobs
    {
        public Job[] jobs { get; set; }

        public class Job
        {
            public string name { get; set; }
            public int size { get; set; }
        }

    }
}
