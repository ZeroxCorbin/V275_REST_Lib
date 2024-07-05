using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V275_REST_lib.Models
{
    public class Event_Base
    {
        public Event? _event { get; set; }

        public class Event
        {
            public string? time { get; set; }
            public string? source { get; set; }
            public int item { get; set; }
            public string? name { get; set; }
            public object? data { get; set; }
        }

    }
}
