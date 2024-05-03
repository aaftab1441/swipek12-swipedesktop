using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwipeDesktop.Models
{
    public class TardyStat 
    {
        public TardyStat()
        {
            MonthToDate = -1;
            YearToDate = -1;
        }

        public string Description { get; set; }
      
        public int MonthToDate { get; set; }

        public int YearToDate { get; set; }

    }
}
