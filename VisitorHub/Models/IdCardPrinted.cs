using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwipeDesktop.Models
{
    public class NewIdCard : RedisObject
    {
        public long Id { get; set; }

        public string StudentNumber { get; set; }

        public Guid StudentGuid { get; set; }

        public int StudentId { get; set; }

        public string Type { get; set; }

        public int SchoolId { get; set; }

        public DateTime PrintDate { get; set; }

        public string RecordedBy { get; set; }
    }
}
