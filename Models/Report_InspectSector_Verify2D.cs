﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V725_REST_lib.Models
{
    public class Report_InspectSector_Verify2D
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
            public string symbolType { get; set; }
            public string decodeText { get; set; }
            public string lengthUnit { get; set; }
            public float xDimension { get; set; }
            public float aperture { get; set; }
            public string lightSource { get; set; }
            public int artifactId { get; set; }

            public Report_InspectSector_Common.Overallgrade overallGrade { get; set; }
            public Report_InspectSector_Common.Decode decode { get; set; }

            public Report_InspectSector_Common.GradeValue symbolContrast { get; set; }
            public Report_InspectSector_Common.GradeValue modulation { get; set; }
            public Report_InspectSector_Common.GradeValue reflectanceMargin { get; set; }
            public Report_InspectSector_Common.GradeValue axialNonUniformity { get; set; }
            public Report_InspectSector_Common.GradeValue gridNonUniformity { get; set; }
            public Report_InspectSector_Common.GradeValue unusedErrorCorrection { get; set; }
            public Report_InspectSector_Common.GradeValue fixedPatternDamage { get; set; }

            public Report_InspectSector_Common.Value minimumReflectance { get; set; }
            public Report_InspectSector_Common.Value maximumReflectance { get; set; }

            public Gs1symbolquality gs1SymbolQuality { get; set; }
            public Gs1results gs1Results { get; set; }

            public ModuleData extendedData { get; set; }

        }

        public class ModuleData
        {
            public int[] ModuleModulation { get; set; }
            public int[] ModuleReflectance { get; set; }

            public int QuietZone { get; set; }

            public int NumRows { get; set; }
            public int NumColumns { get; set; }

            public double CosAngle0 { get; set; }
            public double CosAngle1 { get; set; }

            public double SinAngle0 { get; set; }
            public double SinAngle1 { get; set; }

            public double DeltaX { get; set; }
            public double DeltaY { get; set; }

            public double Xne { get; set; }
            public double Yne { get; set; }

            public double Xnw { get; set; }
            public double Ynw { get; set; }

            public double Xsw { get; set; }
            public double Ysw { get; set; }
        }

        public class Gs1symbolquality
        {
            public Report_InspectSector_Common.ValueResult symbolWidth { get; set; }
            public Report_InspectSector_Common.ValueResult symbolHeight { get; set; }
            public Report_InspectSector_Common.ValueResult cellSizeX { get; set; }
            public Report_InspectSector_Common.ValueResult cellSizeY { get; set; }

            public Report_InspectSector_Common.Grade L1 { get; set; }
            public Report_InspectSector_Common.Grade L2 { get; set; }
            public Report_InspectSector_Common.Grade QZL1 { get; set; }
            public Report_InspectSector_Common.Grade QZL2 { get; set; }
            public Report_InspectSector_Common.Grade OCTASA { get; set; }

            public int growthX { get; set; }
            public int growthY { get; set; }
            public int formatInfo { get; set; }
            public int versionInfo { get; set; }
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
