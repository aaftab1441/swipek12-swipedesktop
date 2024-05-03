using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwipeDesktop.Models
{
    public class VisitModel : EntityBase<long>
    {
        public VisitModel()
        {
            //Id = Guid.NewGuid();
        }

        public string Street1 { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string Zip { get; set; }

        public string Identification { get; set; }

        public DateTime VisitEntryDate { get; set; }

        public DateTime DateOfBirth { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string ReasonForVisit { get; set; }

        public int School { get; set; }

        public string Source { get; set; }

        public string VisitEntryNumber { get; set; }

    }


    public class VisitResponse
    {
        public bool FlaggedForOffender { get; set; }

        public int status { get; set; }

        public string responseText { get; set; }

        public string where { get; set; }

        public int total { get; set; }
    }
}
