using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SwipeDesktop.Common;

namespace SwipeDesktop.Models
{
    public class StaffRecord : RedisObject
    {
        public StaffRecord()
        {
           
        }

        public Lane SwipeLane { get; set; }

        public int PersonId { get; set; }

        public string Barcode { get; set; }

        public string Location { get; set; }

        public string StatusCode { get; set; }

        public string PersonName { get; set; }

        public bool SwipedOut { get; set; }

        public DateTime EntryTime { get; set; }

        public SwipeMode SwipeMode { get; set; }

        public bool IsManual { get; set; }
        public bool IsKiosk { get; set; }

        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3},{4},{5},{6}", SwipeMode, IsManual, SwipeMode, PersonId, Location, Barcode, EntryTime);
        }
    }
}
