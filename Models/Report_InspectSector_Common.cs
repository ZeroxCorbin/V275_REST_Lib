using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V275_REST_lib.Models
{
    public class Report_InspectSector_Common
    { 

        public class Grade
        {
            public float value { get; set; }
            public string letter { get; set; }
        }

        public class Overallgrade
        {
            public Grade grade { get; set; }
            [JsonProperty ("string")]
            public string _string { get; set; }
        }

        public class GradeValue
        {
            public Grade grade { get; set; }
            public int value { get; set; } = -1;
        }

        public class ValueResult
        {
            public float value { get; set; }
            public string result { get; set; }
        }

        public class Value
        {
            public int value { get; set; }
        }

        public class Decode
        {
            public Grade grade { get; set; }
            //Verify2D only
            public int value { get; set; } = -1;

            //Verify1D only
            public ValueResult edgeDetermination { get; set; }
        }

        public class Alarm
        {
            public string name { get; set; }
            public int category { get; set; }
            public SubAlarm data { get; set; }
            public Useraction userAction { get; set; }
        }

        public class SubAlarm
        {
            public string text { get; set; }
            public int index { get; set; }
            public string subAlarm { get; set; }
            public string expected { get; set; }
        }

        public class Useraction
        {
            public string action { get; set; }
            public string user { get; set; }
            public string note { get; set; }
        }

        public class Gs1results
        {
            public bool validated { get; set; }
            public string input { get; set; }
            public string formattedOut { get; set; }
            public Fields fields { get; set; }
            public string error { get; set; }
        }

        public class Fields
        {
            [JsonProperty("01")]
            public string _01 { get; set; }
            [JsonProperty("90")]
            public string _90 { get; set; }
            [JsonProperty("10")]
            public string _10 { get; set; }
        }

    }
}
