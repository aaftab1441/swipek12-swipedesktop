using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwipeDesktop.Models
{
    public class CardTemplate
    {
        public int Id { get; set; }

        public string TemplateName { get; set; }

        public bool IsForStudent { get; set; }

        public bool IsForStaff { get; set; }

        public bool Active { get; set; }
    }
}
