﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V275_REST_lib.Models
{
    public class Calibration
    { 
        public bool cancel { get; set; }
        public float rmin { get; set; }
        public float rmax { get; set; }
        public string? state { get; set; }
        public bool videoBalanced { get; set; }
        public bool normalizationError { get; set; }
        public bool calibrationReady { get; set; }
        public bool hasCalFile { get; set; }
    }
}
