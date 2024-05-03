using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SwipeDesktop.Interfaces;

namespace SwipeDesktop.Models
{
    public class StaffDetails : IViewModel
    {
        public string OfficeLocation { get; set; }

        public string JobTitle { get; set; }
    }
}
