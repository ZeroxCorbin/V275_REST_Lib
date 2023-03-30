﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V725_REST_lib.Models
{
    public class Report_InspectSector_Blemish
    {
        public string name { get; set; }
        public string type { get; set; }
        public int top { get; set; }
        public int left { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public Data data { get; set; }

        public class Data
        {
            public Report_InspectSector_Common.Alarm[] alarms { get; set; }
            public int blemishCount { get; set; }
            public int reportCount { get; set; }
            public Blemish[] blemishList { get; set; }
        }

        public class Blemish
        {
            public string type { get; set; }
            public int top { get; set; }
            public int left { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public float maximumDimension { get; set; }
            public float residualArea { get; set; }
            public int maxTolerancePercent { get; set; }
            public int artifactId { get; set; }
        }

    }
}
