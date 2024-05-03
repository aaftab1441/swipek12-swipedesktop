using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SwipeDesktop.Models
{
    public class Receipt
    {
        public string StudentNumber { get; set; }

        public string StudentName { get; set; }

        public string Grade { get; set; }

        public string Details { get; set; }

        public string Homeroom { get; set; }

        public string ReceivedBy { get; set; }
        public decimal ChargeAmt { get; set; }
        public decimal PaidAmt { get; set; }


        public DateTime PrintDate { get; set; }

        public BitmapSource StudentImage { get; set; }
    }
}
