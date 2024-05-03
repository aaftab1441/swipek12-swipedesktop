using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwipeDesktop.Models
{
    public class VisitExit : RedisObject
    {
        public string VisitId { get; set; }

        public string VisitNumber { get; set; }

        public int SchoolId { get; set; }

        public DateTime DateExited { get; set; }

        public string Source { get; set; }
    }
}
