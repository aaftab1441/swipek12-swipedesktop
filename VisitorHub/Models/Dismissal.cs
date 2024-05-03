using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using PetaPoco;
using ReactiveUI;
using ServiceStack.DataAnnotations;
using SwipeDesktop.Common;

namespace SwipeDesktop.Models
{
    public class Dismissal : RedisObject
    {
        public Guid StudentGuid { get; set; }

        public string StudentNumber { get; set; }

        public string StudentName { get; set; }

        public string Reason { get; set; }

        public string StatusCode { get; set; }

        public DateTime DismissalTime { get; set; }

        public DateTime? ReEntryTime { get; set; }

    }
}
