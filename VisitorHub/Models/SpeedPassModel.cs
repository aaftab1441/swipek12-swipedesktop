using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwipeDesktop.Models
{
    public class SpeedPassModel
    {
        public string PassId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PassType { get; set; }

        public DateTime Expires { get; set; }
    }
}
