using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TVWB
{
    public class Absence
    {
        public string Date { get; set; }
        public string Period { get; set; }
        public string Class { get; set; }
        public string Code { get; set; }
        public string Reason { get; set; }
    }

    public class AbsentsInfo
    {
        public List<Absence> Absences { get; set; }
        public string Absents { get; set; }
        public string Lates { get; set; }
    }
}
