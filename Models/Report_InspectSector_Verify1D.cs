using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V275_REST_Lib.Models
{
    public class Report_InspectSector_Verify1D
    {
        public string? name { get; set; }
        public string? type { get; set; }
        public int top { get; set; }
        public int left { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public Data? data { get; set; }

        public class Data
        {
            public Report_InspectSector_Common.Alarm[]? alarms { get; set; }
            public string? symbolType { get; set; }
            public string? decodeText { get; set; }
            public string? lengthUnit { get; set; }
            public float xDimension { get; set; }
            public float aperture { get; set; }
            public string? lightSource { get; set; }
            public int artifactId { get; set; }

            public Report_InspectSector_Common.Overallgrade? overallGrade { get; set; }
            public Report_InspectSector_Common.Decode? decode { get; set; }

            public Report_InspectSector_Common.GradeValue? symbolContrast { get; set; }
            public Report_InspectSector_Common.GradeValue? edgeContrast { get; set; }
            public Report_InspectSector_Common.GradeValue? modulation { get; set; }
            public Report_InspectSector_Common.GradeValue? defects { get; set; }
            public Report_InspectSector_Common.GradeValue? decodability { get; set; }

            public Report_InspectSector_Common.ValueResult? quietZoneLeft { get; set; }
            public Report_InspectSector_Common.ValueResult? quietZoneRight { get; set; }

            public Report_InspectSector_Common.GradeValue? unusedErrorCorrection { get; set; }

            public Report_InspectSector_Common.GradeValue? cwYeild { get; set; }
            public Report_InspectSector_Common.GradeValue? cwPrintQuality { get; set; }

            public Report_InspectSector_Common.GradeValue? minimumReflectance { get; set; }
            public Report_InspectSector_Common.Value? maximumReflectance { get; set; }
            public Gs1symbolquality? gs1SymbolQuality { get; set; }
            public Report_InspectSector_Common.Gs1results? gs1Results { get; set; }
        }

        public class Gs1symbolquality
        {
            public Report_InspectSector_Common.ValueResult? symbolXdim { get; set; }
            public Report_InspectSector_Common.ValueResult? symbolBarHeight { get; set; }
        }

    }
}
