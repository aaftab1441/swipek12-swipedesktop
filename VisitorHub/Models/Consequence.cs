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
    public class Consequence : RedisObject
    {
        public Guid StudentGuid { get; set; }

        public string StudentNumber { get; set; }

        public string StudentName { get; set; }

        public string Grade { get; set; }

        public int OutcomeType { get; set; }
     
        public string Homeroom { get; set; }

        public string InfractionCode { get; set; }

        public string Details { get; set; }

        public int Units { get; set; }

        public DateTime InfractionDate { get; set; }

        public DateTime ServeBy { get; set; }

        public BitmapSource StudentImage { get; set; }

    }
}
