using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwipeDesktop.Common
{
    public class CheckedItem
    {
        public string   Item { get; set; }
        public string ItemNumber { get; set; }

        public int ItemId { get; set; }

        public bool     IsChecked { get; set; }
    }
}
