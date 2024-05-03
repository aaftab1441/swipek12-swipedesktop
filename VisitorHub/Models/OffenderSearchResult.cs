using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SwipeDesktop.Models
{
    public class OffenderSearchResult
    {
        public OffenderSearchResult()
        {
            DemographicData = new DemographicInformation();
        }
        public string OffenderSearchId { get; set; }

        public string Name { get; set; }
        public string BirthDate { get; set; }
        public BitmapSource Image { get; set; }
        public List<string> Aliases { get; set; } 
        public List<string> Offenses { get; set; }
        public string HtmlLink { get; set; }
        public DemographicInformation DemographicData { get; set; }

    }
}
