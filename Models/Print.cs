using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace V275_REST_lib.Models
{
    public class Print
    {
        public bool enabled { get; set; }
        public bool state { get; set; }
        [JsonProperty("override")]
        public bool _override { get; set; }
    }
}
