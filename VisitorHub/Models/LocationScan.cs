using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SwipeK12.NextGen.ReadServices.Messages;

namespace SwipeDesktop.Models
{
    public class LocationScan : RedisObject
    {
        public string RoomName { get; set; }

        public DateTime SwipeTime { get; set; }

        public string StudentNumber { get; set; }

        public bool SwipedOut { get; set; }

        public bool MarkAllPresent { get; set; }

        public bool IsKiosk { get; set; }

    }
}
