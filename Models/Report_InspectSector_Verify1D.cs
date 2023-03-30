using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V725_REST_lib.Models
{
    public class Report_InspectSector_Verify1D
    {
        [Newtonsoft.Json.JsonIgnore]
        public virtual int id { get; protected set; }
        [Newtonsoft.Json.JsonIgnore]
        public virtual int report_id { get; set; }

        public virtual string name { get; set; }
        public virtual string type { get; set; }
        public virtual int top { get; set; }
        public virtual int left { get; set; }
        public virtual int width { get; set; }
        public virtual int height { get; set; }
        public virtual Data data { get; set; }

        public class Data
        {
            public virtual Report_InspectSector_Common.Alarm[] alarms { get; set; }
            public virtual string symbolType { get; set; }
            public virtual string decodeText { get; set; }
            public virtual string lengthUnit { get; set; }
            public virtual float xDimension { get; set; }
            public virtual float aperture { get; set; }
            public virtual string lightSource { get; set; }
            public virtual int artifactId { get; set; }

            public virtual Report_InspectSector_Common.Overallgrade overallGrade { get; set; }
            public virtual Report_InspectSector_Common.Decode decode { get; set; }

            public virtual Report_InspectSector_Common.GradeValue symbolContrast { get; set; }
            public virtual Report_InspectSector_Common.GradeValue edgeContrast { get; set; }
            public virtual Report_InspectSector_Common.GradeValue modulation { get; set; }
            public virtual Report_InspectSector_Common.GradeValue defects { get; set; }
            public virtual Report_InspectSector_Common.GradeValue decodability { get; set; }

            public virtual Report_InspectSector_Common.ValueResult quietZoneLeft { get; set; }
            public virtual Report_InspectSector_Common.ValueResult quietZoneRight { get; set; }

            public virtual Report_InspectSector_Common.GradeValue unusedErrorCorrection { get; set; }

            public virtual Report_InspectSector_Common.GradeValue cwYeild { get; set; }
            public virtual Report_InspectSector_Common.GradeValue cwPrintQuality { get; set; }

            public virtual Report_InspectSector_Common.GradeValue minimumReflectance { get; set; }
            public virtual Report_InspectSector_Common.Value maximumReflectance { get; set; }
            public virtual Gs1symbolquality gs1SymbolQuality { get; set; }
            public virtual Gs1results gs1Results { get; set; }
        }

        public class Gs1symbolquality
        {
            public virtual Report_InspectSector_Common.ValueResult symbolXdim { get; set; }
            public virtual Report_InspectSector_Common.ValueResult symbolBarHeight { get; set; }
        }

        public class Gs1results
        {
            public virtual bool validated { get; set; }
            public virtual string input { get; set; }
            public virtual string formattedOut { get; set; }
            public virtual Fields fields { get; set; }
            public virtual string error { get; set; }
        }

        public class Fields
        {
            [JsonProperty("01")]
            public virtual string _01 { get; set; }
            [JsonProperty("90")]
            public virtual string _90 { get; set; }
            [JsonProperty("10")]
            public virtual string _10 { get; set; }
        }



    }
}
