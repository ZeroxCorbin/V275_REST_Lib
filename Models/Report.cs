using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V725_REST_lib.Models.Reports
{
    public class Report
    {
        [Newtonsoft.Json.JsonIgnore]
        public virtual int id { get; protected set; }

        public virtual Inspectlabel inspectLabel { get; set; }

        public class Inspectlabel
        {
            [Newtonsoft.Json.JsonIgnore]
            public virtual int id { get; protected set; }

            public virtual int repeat { get; set; }
            public virtual int voidRepeat { get; set; }
            public virtual int iteration { get; set; }
            public virtual string result { get; set; }
            public virtual int width { get; set; }
            public virtual int height { get; set; }

            public virtual Useraction userAction { get; set; }

            public virtual IList<object> inspectSector { get; set; }
            public virtual IList<object> ioLines { get; set; }
        }

        //public class SubAlarm
        //{
        //    public string text { get; set; }
        //    public int index { get; set; }
        //    public string subAlarm { get; set; }
        //    public string expected { get; set; }
        //}
        public class Useraction
        {
            [Newtonsoft.Json.JsonIgnore]
            public virtual int id { get; protected set; }

            public virtual string action { get; set; }
            public virtual string user { get; set; }
            public virtual string note { get; set; }
        }
    }
}
