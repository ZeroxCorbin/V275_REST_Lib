﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V275_REST_lib.Models
{
    public class Configuration_Camera
    {

        public TypeValueString? flip { get; set; }
        public TypeValueString? peelAndPresentMode { get; set; }
        public TypeValueString? name { get; set; }
        public TypeValueString? backupVoidMode { get; set; }
        public TypeValueInteger? backupVoidRepeatCount { get; set; }

        public class TypeValueString
        {
            public string? type { get; set; }
            public string? value { get; set; }
        }

        public class TypeValueInteger
        {
            public string? type { get; set; }
            public int value { get; set; } = -1;
        }

    }
}
