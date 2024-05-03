using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwipeDesktop.Models
{
    public class Fine : RedisObject
    {
      
        public string Name { get; set; }

        public decimal Amount { get; set; }
    }

    public class AssessedFine : Fine
    {
        public AssessedFine(){}

        public AssessedFine(Fine fine)
        {
            this.Amount = fine.Amount;
            this.Name = fine.Name;
        }

        public long Id { get; set; }

        public string StudentNumber { get; set; }

        public Guid StudentGuid { get; set; }

        public int StudentId { get; set; }

        public string Text { get; set; }

        public int SchoolId { get; set; }

        public DateTime FineDate { get; set; }

        public decimal AmountPaid { get; set; }

        public string RecordedBy { get; set; }

    }
}
