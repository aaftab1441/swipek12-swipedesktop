using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SwipeDesktop.Common;

namespace SwipeDesktop.Models
{
    public class ScanModel : RedisObject
    {
        public ScanModel()
        {
            TakeAttendance = true;
        }
        public Guid StudentGuid { get; set; }

        public int StudentId { get; set; }

        public string Barcode { get; set; }

        public string AttendanceCode { get; set; }

        public string Location { get; set; }

        public string Period { get; set; }

        public DateTime EntryTime { get; set; }

        public SwipeMode SwipeMode { get; set; }

        public bool IsManual { get; set; }

        public bool TakeAttendance { get; set; }

        public bool MarkAllPresent { get; set; }

        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}", SwipeMode, IsManual, SwipeMode, StudentId, Location, StudentGuid, Barcode, EntryTime, AttendanceCode);
        }
    }
}
