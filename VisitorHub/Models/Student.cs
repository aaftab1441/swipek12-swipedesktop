using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwipeDesktop.Models
{
    public class Student
    {
        public Guid Guid { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Middle { get; set; }

        public string StudentNumber { get; set; }

        public bool Active { get; set; }

        public string Grade { get; set; }

        public string Homeroom { get; set; }
    }
}
