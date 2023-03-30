using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V725_REST_lib.Models
{
    public class Configuration_Camera
    {
        [Newtonsoft.Json.JsonIgnore]
        public virtual int id { get; protected set; }

        public virtual TypeValueString flip { get; set; }
        public virtual TypeValueString peelAndPresentMode { get; set; }
        public virtual TypeValueString name { get; set; }
        public virtual TypeValueString backupVoidMode { get; set; }
        public virtual TypeValueInteger backupVoidRepeatCount { get; set; }

        public class TypeValueString
        {
            [Newtonsoft.Json.JsonIgnore]
            public virtual int id { get; protected set; }

            public virtual string type { get; set; }
            public virtual string value { get; set; }
        }

        public class TypeValueInteger
        {
            [Newtonsoft.Json.JsonIgnore]
            public virtual int id { get; protected set; }

            public virtual string type { get; set; }
            public virtual int value { get; set; } = -1;
        }

    }
}
