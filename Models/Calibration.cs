using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V725_REST_lib.Models
{
    public class Calibration
    {
        [Newtonsoft.Json.JsonIgnore]
        public virtual int id { get; protected set; }

        public virtual bool cancel { get; set; }
        public virtual float rmin { get; set; }
        public virtual float rmax { get; set; }
        public virtual string state { get; set; }
        public virtual bool videoBalanced { get; set; }
        public virtual bool normalizationError { get; set; }
        public virtual bool calibrationReady { get; set; }
        public virtual bool hasCalFile { get; set; }
    }
}
