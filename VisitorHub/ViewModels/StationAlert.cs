using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwipeDesktop.ViewModels
{
    public class StationAlert
    {
        public int AlertId { get; set; }

        public string AlertText { get; set; }

        public DateTime Expires { get; set; }

        public bool Active { get; set; }

        public int CorrelationId { get; set; }

        public int AlertColor { get; set; }

        public string AlertSound { get; set; }

        public string AlertType { get; set; }

        public int SchoolId { get; set; }

        public int StudentId { get; set; }

        public int Rank { get; set; }
    }
}