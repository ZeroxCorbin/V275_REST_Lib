using System;
using System.Collections.Generic;
using System.Text;

namespace V275_REST_Lib.LocalDatabases
{
    public class RunLogReportData
    {
        public int cycleId { get; set; }
        public int cyclePassed { get; set; }
        public string imageUrl { get; set; }
        public string reportData { get; set; }
        public string timeStamp { get; set; }
        public int voidId { get; set; }
    }
}
