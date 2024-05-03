using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;
using SwipeDesktop.Interfaces;

namespace SwipeDesktop.Models
{
    public class StudentDetails : ReactiveObject, IViewModel
    {
        public StudentDetails()
        {
            LunchCodes = new List<string> { "F", "R", "N/A" };
        }

        public List<string> LunchCodes { get; set; }

        private string _grade;
        public string Grade
        {
            get { return _grade; }
            set { this.RaiseAndSetIfChanged(ref _grade, value); }
        }

        private string _lunchCode;
        public string LunchCode
        {
            get { return _lunchCode; }
            set { this.RaiseAndSetIfChanged(ref _lunchCode, value); }
        }

        private string _homeroom;
        public string Homeroom
        {
            get { return _homeroom; }
            set { this.RaiseAndSetIfChanged(ref _homeroom, value); }
        }
      
        private string _bus;
        public string Bus
        {
            get { return _bus; }
            set { this.RaiseAndSetIfChanged(ref _bus, value); }
        }

        private string _studentNumber;
        public string StudentNumber
        {
            get { return _studentNumber; }
            set { this.RaiseAndSetIfChanged(ref _studentNumber, value); }
        }

        private string _guid;
        public string GUID
        {
            get { return _guid; }
            set { this.RaiseAndSetIfChanged(ref _guid, value); }
        }


    }
}
