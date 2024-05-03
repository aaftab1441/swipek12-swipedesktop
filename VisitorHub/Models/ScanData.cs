using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwipeDesktop.Models
{
    public class ScanData
    {
        public ScanData(string data)
        {
            Data = data;
        }

        public string Data { get; private set; }
      
    }
}
