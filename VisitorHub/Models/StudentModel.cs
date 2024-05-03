using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using log4net;
using ReactiveUI;
using Telerik.Windows.Media.Imaging;

namespace SwipeDesktop.Models
{
    public class StudentModel : PersonModel
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(StudentModel));

        private static readonly Uri NullPersonImageUri = new Uri("pack://siteoforigin:,,,/Resources/nullpersonblue.png");

        public StudentModel()
        {
            Grade = "N/A";
            Homeroom = "N/A";

            this.WhenAnyValue(x => x.Grade, x => x.Homeroom)
            .Select(x => String.Format("Grade: {0}    HR: {1}", x.Item1, x.Item2))
            .ToProperty(this, x => x.GradeAndHomeroom, out _gradeAndHomeroom);

            this.WhenAnyValue(x => x.StudentId)
             .Select(x => x == 0)
             .ToProperty(this, x => x.IsStaff, out base._isStaff);
        }


        public string Homeroom { get; set; }

        public int StudentId { get; set; }

        public string Grade { get; set; }


        readonly ObservableAsPropertyHelper<string> _gradeAndHomeroom;
        public string GradeAndHomeroom
        {
            get { return _gradeAndHomeroom.Value; }
        }
    }
}
