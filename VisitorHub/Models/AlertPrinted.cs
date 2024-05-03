using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SwipeDesktop.Models
{
    public class AlertPrinted : RedisObject
    {

        public AlertPrinted()
        {
            InfractionDate = DateTime.Now;
            DatePrinted = DateTime.Now;
        }

        public int AlertId { get; set; }

        public Guid StudentGuid { get; set; }

        public string StudentNumber { get; set; }

        public DateTime DatePrinted { get; set; }

        public int CorrelationId { get; set; }

        public string StudentName { get; set; }

        public string Grade { get; set; }

        public string Homeroom { get; set; }

        public string Details { get; set; }

        public DateTime InfractionDate { get; set; }

        [JsonIgnore]
        public BitmapSource StudentImage { get; set; }
    }
}
