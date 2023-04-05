﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V275_REST_lib.Models
{
    public class Events_System
    {


        public string time { get; set; }
        public string source { get; set; }
        public int item { get; set; }
        public string name { get; set; }
        public Data data { get; set; }

        public class Data
        {
            public string token { get; set; }
            public string id { get; set; }
            public string accessLevel { get; set; }
            public string state { get; set; }

            public int position { get; set; }
            public int repeat { get; set; }
            public int repeatWidth { get; set; }
            public int repeatHeight { get; set; }
            public int sectorCount { get; set; }

            public string fromState { get; set; }
            public string toState { get; set; }

            public Detection[] detections { get; set; }
        }

        public class Detection
        {
            public string symbology { get; set; }
            public Region region { get; set; }
            public int orientation { get; set; }

            public class Region
            {
                public int x { get; set; }
                public int y { get; set; }
                public int width { get; set; }
                public int height { get; set; }
            }
        }


        public class Rootobject
        {
            public Event _event { get; set; }
        }

        public class Event
        {
            public string time { get; set; }
            public string source { get; set; }
            public int item { get; set; }
            public string name { get; set; }
            public Data data { get; set; }
        }

    }
}
