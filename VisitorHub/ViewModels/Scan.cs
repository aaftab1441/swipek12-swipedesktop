using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ReactiveUI;
using Simple.Data;
using SwipeDesktop.Common;
using SwipeDesktop.Models;

namespace SwipeDesktop.ViewModels
{
    public class Scan : ReactiveObject
    {
        private static readonly StationMode StationMode = (StationMode)Enum.Parse(typeof(StationMode), ConfigurationManager.AppSettings["mode"]);
        public ScanModel DataModel { get; private set; }


        public Scan()
        {
            DataModel = new ScanModel();
            TardyStats = new ReactiveList<TardyStat>();
            Alerts = new List<StationAlert>();

            this.WhenAnyValue(x => x.StudentName)
                .Select(x => string.Format("{0}", x))
                .ToProperty(this, x => x.DisplayText, out _displayText);

            this.WhenAnyValue(x => x.Alerts)
               .Select(x => x.Any())
               .ToProperty(this, x => x.HasAlerts, out _hasAlerts); 
            
            this.WhenAnyValue(x => x.Alerts)
               .Select(x => x.Any(a => a.AlertText.ToLower().Contains("wrong group")))
               .ToProperty(this, x => x.IsWrongGroup, out _wrongGroup);
           
            this.WhenAnyValue(x => x.Alerts)
                .Select(x=>x.Exists(a=>a.AlertType == "Wrong Lunch"))
                .ToProperty(this, x => x.HasLunchAlert, out _hasLunchAlert);
            
            this.WhenAnyValue(x => x.Alerts).Where(x=>x.Any())
              .Select(x => x[0])
              .ToProperty(this, x => x.TopMostAlert, out _topMostAlert);

            this.WhenAnyValue(x => x.EntryTime, x => x.SwipeMode)
                .Select(x =>
                {
                    if (x.Item2 == SwipeMode.Location)
                    {
                        return string.Format("{0}", x.Item1.ToString("MMMM dd, yyyy"));
                    }
                    return string.Format("{0}", x.Item1.ToString("MMMM dd, yyyy hh:mm tt"));
                }).ToProperty(this, x => x.FormattedDate, out _formattedDate);

            this.WhenAnyValue(x => x.Grade)
                .Select(x => string.Format("Grade: {0}", x))
                .ToProperty(this, x => x.GradeLabel, out _gradeLabel);

            this.WhenAnyValue(x => x.Homeroom)
                .Select(x => string.Format("Homeroom: {0}", x))
                .ToProperty(this, x => x.HomeroomLabel, out _homeroomLabel);

            this.WhenAnyValue(x => x.EntryTime, x => x.ScanLocation, x => x.SwipeMode, x=>x.IsLeavingLocation)
                .Where(x => x.Item3 == SwipeMode.Location && x.Item2 != null)
                .Select(x =>
                {
                    if (!x.Item4)
                        return string.Format("Enter {1} - {0}", x.Item1.ToString("h:mm:ss tt"), x.Item2.RoomName);
                    else
                    {
                        return string.Format("Leave {1} - {0}", x.Item1.ToString("h:mm:ss tt"), x.Item2.RoomName);
                    }
                })
                .ToProperty(this, x => x.TardyModeDisplay, out _tardyModeDisplay);

            this.WhenAnyValue(x => x.EntryTime, x => x.EntryStatus, x => x.SwipeMode)
                .Where(x => x.Item3 == SwipeMode.Entry)
                .Subscribe(x =>
                {
                    var display = string.Format("{1} - {0}", x.Item1.ToString("h:mm:ss tt"), x.Item2);

                    this.EntryModeDisplay = display;
                });
                //.ToProperty(this, x => x.EntryModeDisplay, out _entryModeDisplay);

            this.WhenAnyValue(x => x.TardyModeDisplay, x => x.EntryModeDisplay).Where(x => !string.IsNullOrEmpty(x.Item1) || !string.IsNullOrEmpty(x.Item2))
                .Subscribe(x =>
                {
                    if (IsStudentDismissed)
                    {
                        DisplayEntry = DismissalText;
                        PassDescription = DismissalText;
                    }
                    else
                    {
                        
                        DisplayEntry = !string.IsNullOrEmpty(x.Item1) ? x.Item1 : x.Item2;

                        //if (SwipeMode != Common.SwipeMode.Location)
                        //{
                        //  PassDescription = DisplayEntry;
                        //}
                        /*
                        if (SwipeMode == Common.SwipeMode.Location && IsLeavingLocation)
                        {
                            var text = string.Format("To Classroom from {0}", Room);
                          
                            PassDescription = text;
                        }*/
                        
                        PassDescription = DisplayEntry;
                    }
                 
                });
            

            this.WhenAnyValue(x => x.Period, x=>x.Room, x=>x.SwipeMode, x=>x.AttendanceCode)
            .Where(x => x.Item3 == SwipeMode.ClassroomTardy)
           .Select(x => string.Format("{2} Period: {0} Room: {1}", x.Item1, x.Item2, x.Item4))
             .Subscribe(x =>
             {
                PassDescription = x;
                 
             });

            this.WhenAnyValue(x => x.Period, x => x.SwipeMode)
            .Where(x => x.Item2 == SwipeMode.CafeEntrance)
           .Select(x => string.Format("Lunch - Period {0}", x.Item1))
             .Subscribe(x =>
             {
                 DisplayEntry = PassDescription = x;

             });

            this.WhenAnyValue(x => x.Room, x => x.SwipeMode)
                .Where(x => x.Item2 == SwipeMode.Group)
                .Select(x => string.Format("{0}", x.Item1))
                .Subscribe(x =>
                {
                    DisplayEntry = PassDescription = x;

                });


            this.WhenAnyValue(x => x.Room, x => x.SwipeMode, x=>x.IsLeavingLocation).Where(x=>x.Item2 == SwipeMode.Location && x.Item3)
             .Subscribe(x =>
             {
                 var text = "To Classroom from {0} {1}";
                 
                 text = string.Format(text, x.Item1, DateTime.Now.ToString("h:mm:ss tt"));
                 

                 PassDescription = text;
             });

            this.WhenAnyValue(x => x.Room, x=>x.IsLeavingLocation)
           .Subscribe(x =>
           {
               if (StationMode == StationMode.VisitorKiosk)
               {
                   var ioText = x.Item2 ? "OUT" : "IN";
                   var text = $"{ioText} {Settings.Default.KioskLocation} {DateTime.Now.ToString("h:mm:ss tt")}";

                   PassDescription = text;
               }
           });

            this.WhenAnyValue(x => x.SwipeMode, x => x.Room, x=>x.GradeLabel, x=>x.HomeroomLabel, x=>x.IsStaffScan)
            .Select(x =>
            {
                if (x.Item5)
                    return this.Barcode;

                if (x.Item1 == SwipeMode.ClassroomTardy)
                    return string.Format("Room: {0}", x.Item2);

                return string.Format("{0} {1}",x.Item3, x.Item4);
                
            })
            .ToProperty(this, x => x.LocationText, out _locationText);

            /*
            this.WhenAnyValue(x => x.ScanLocation, x => x.SwipeMode)
                .Where(x => x.Item2 == SwipeMode.Group)
                .Subscribe(x =>
                {
                    if (x.Item1 != null)
                    {
                        Room = string.Format("Group {0}", x.Item1.RoomName);
                    }
                });*/

            this.WhenAnyValue(x => x.ScanLocation, x => x.SwipeMode, x=>x.EntryTime)
                 .Where(x => x.Item2 == SwipeMode.ClassroomTardy)
                 .Subscribe(x =>
                 {
                     if (x.Item1 != null)
                     {
                         this.Period = x.Item1.PeriodCode;
                         this.AttendanceCode = x.Item1.AttendanceCode;
                         //DisplayEntry = string.Format(x.Item1.RoomName);
                         DisplayEntry = string.Format("{0} {1}", x.Item1.RoomName, x.Item3.ToString("hh:mm:ss tt"));
                     }
                 });
                //.Select(x => x.PeriodCode)
                //.ToProperty(this, x => x.Period, out _period);

            //this.WhenAnyValue(x => x.ScanLocation)
            //    .Where(x => x != null)
            //    .Select(x => x.AttendanceCode)
            //    .ToProperty(this, x => x.AttendanceCode, out _attendanceCode);

            this.WhenAnyValue(x => x.EntryStatus, x => x.SwipeMode)
                 .Where(x => x.Item2 == SwipeMode.Entry)
                 .Subscribe((t) =>
                 {
                     this.AttendanceCode = t.Item1;
                 });
                //.Select(x => x.Item1)
                //.ToProperty(this, x => x.AttendanceCode, out _attendanceCode);


            //this.WhenAnyValue(x => x.IsStudentDismissed).Where(x => x).BindTo(this, x => x.DataModel.Location);

            this.WhenAnyValue(x => x.IsStudentDismissed, x=>x.IsLeavingLocation).Where(x => x.Item1).Subscribe(s =>
            {
                string isReturning = "";

                //second swipe at dismissal is a return...
                if (s.Item2)
                {
                    isReturning = "Returning: ";
                }
                DismissalText = string.Format("{2} Dismissal - {0} {1}", this.ScanLocation.RoomName, this.EntryTime.ToString("hh:mm:ss tt"), isReturning);
                PassDescription = DismissalText;
            });

            this.WhenAnyValue(x => x.DismissalText).Where(x => !string.IsNullOrEmpty(x)).Subscribe(x=>
            {
                DisplayEntry = DismissalText;
            });
            

            this.WhenAnyValue(x => x.MarkAllPresentMode).BindTo(this, x => x.DataModel.MarkAllPresent);
            this.WhenAnyValue(x => x.Barcode).BindTo(this, x => x.DataModel.Barcode);
            this.WhenAnyValue(x => x.AttendanceCode).BindTo(this, x => x.DataModel.AttendanceCode);
            this.WhenAnyValue(x => x.Period).BindTo(this, x => x.DataModel.Period);
            this.WhenAnyValue(x => x.EntryTime).BindTo(this, x => x.DataModel.EntryTime);
            this.WhenAnyValue(x => x.StudentGuid).BindTo(this, x => x.DataModel.StudentGuid);
            this.WhenAnyValue(x => x.StudentId).BindTo(this, x => x.DataModel.StudentId);
            this.WhenAnyValue(x => x.SwipeMode).BindTo(this, x => x.DataModel.SwipeMode);
            this.WhenAnyValue(x => x.IsManualSwipe).BindTo(this, x => x.DataModel.IsManual);
            //this.WhenAnyValue(x => x.ScanLocation, x=>x.IsStudentDismissed).Where(x => x.Item1 != null && x.Item2).Select(x => x.Item1.RoomName).BindTo(this, x => x.DataModel.Location);
            this.WhenAnyValue(x => x.ScanLocation).Where(x => x != null).Select(x => x.RoomName).BindTo(this, x => x.DataModel.Location);
        }

        public ReactiveList<TardyStat> TardyStats { get; set; }

        string _barcode;
        public string Barcode
        {
            get { return _barcode; }
            set { this.RaiseAndSetIfChanged(ref _barcode, value); }
        }
        
        string _dismissalText;
        public string DismissalText
        {
            get { return _dismissalText; }
            set { this.RaiseAndSetIfChanged(ref _dismissalText, value); }
        }

        Guid _guid;
        public Guid StudentGuid
        {
            get { return _guid; }
            set { this.RaiseAndSetIfChanged(ref _guid, value); }
        }


        int _sId;
        public int StudentId
        {
            get { return _sId; }
            set { this.RaiseAndSetIfChanged(ref _sId, value); }
        }

        readonly ObservableAsPropertyHelper<string> _displayText;
        public string DisplayText
        {
            get { return _displayText.Value; }
        }


         string _alertDisplayText;
        public string AlertDisplayText
        {
            get { return _alertDisplayText; }
            set { this.RaiseAndSetIfChanged(ref _alertDisplayText, value); }
        }


        readonly ObservableAsPropertyHelper<string> _formattedDate;
        public string FormattedDate
        {
            get { return _formattedDate.Value; }
        }


        readonly ObservableAsPropertyHelper<string> _tardyModeDisplay;
        public string TardyModeDisplay
        {
            get { return _tardyModeDisplay.Value; }
        }

        string _entryModeDisplay;
        public string EntryModeDisplay
        {
             get { return _entryModeDisplay; }
            set { this.RaiseAndSetIfChanged(ref _entryModeDisplay, value); }
        }
        

        string _displayEntry;
        public string DisplayEntry
        {
            get { return _displayEntry; }
            set { this.RaiseAndSetIfChanged(ref _displayEntry, value); }
        }
       
        string _studentName;
        public string StudentName
        {
            get { return _studentName; }
            set { this.RaiseAndSetIfChanged(ref _studentName, value); }
        }


        Lane _lane;
        public Lane SwipeLane
        {
            get { return _lane; }
            set { this.RaiseAndSetIfChanged(ref _lane, value); }
        }


        bool _isManualSwipe;
        public bool IsManualSwipe
        {
            get { return _isManualSwipe; }
            set { this.RaiseAndSetIfChanged(ref _isManualSwipe, value); }
        }

        List<StationAlert> _alerts;
        public List<StationAlert> Alerts
        {
            get { return _alerts; }
            set { this.RaiseAndSetIfChanged(ref _alerts, value); }
        }
        
        readonly ObservableAsPropertyHelper<bool> _hasAlerts;
        public bool HasAlerts
        {
            get { return _hasAlerts.Value; }
        } 
        
        readonly ObservableAsPropertyHelper<bool> _wrongGroup;
        public bool IsWrongGroup
        {
            get { return _wrongGroup.Value; }
        }

        readonly ObservableAsPropertyHelper<bool> _hasLunchAlert;
        public bool HasLunchAlert
        {
            get { return _hasLunchAlert.Value; }
        }

        private readonly ObservableAsPropertyHelper<StationAlert> _topMostAlert;
        public StationAlert TopMostAlert
        {
            get { return _topMostAlert.Value; }
        }

        bool _invalidScan;
        public bool InvalidScan
        {
            get { return _invalidScan; }
            set { this.RaiseAndSetIfChanged(ref _invalidScan, value); }
        }


        bool _isStaffScan;
        public bool IsStaffScan
        {
            get { return _isStaffScan; }
            set { this.RaiseAndSetIfChanged(ref _isStaffScan, value); }
        }


        bool _isLeaving;
        public bool IsLeavingLocation
        {
            get { return _isLeaving; }
            set { this.RaiseAndSetIfChanged(ref _isLeaving, value); }
        }


        bool _isMarkAllPresent;
        public bool MarkAllPresentMode
        {
            get { return _isMarkAllPresent; }
            set { this.RaiseAndSetIfChanged(ref _isMarkAllPresent, value); }
        }


        bool _alreadySwiped;
        public bool AlreadySwiped
        {
            get { return _alreadySwiped; }
            set { this.RaiseAndSetIfChanged(ref _alreadySwiped, value); }
        }

        bool _studentDismissed;
        public bool IsStudentDismissed
        {
            get { return _studentDismissed; }
            set { this.RaiseAndSetIfChanged(ref _studentDismissed, value); }
        }

        SwipeMode _swipeMode;
        public SwipeMode SwipeMode
        {
            get { return _swipeMode; }
            set { this.RaiseAndSetIfChanged(ref _swipeMode, value); }
        }

        ScanLocation _scanLocation;
        public ScanLocation ScanLocation
        {
            get { return _scanLocation; }
            set { this.RaiseAndSetIfChanged(ref _scanLocation, value); }
        }


        BitmapSource _scanImage;
        public BitmapSource ScanImage
        {
            get { return _scanImage; }
            set { this.RaiseAndSetIfChanged(ref _scanImage, value); }
        }


        DateTime _entryTime;
        public DateTime EntryTime
        {
            get { return _entryTime; }
            set { this.RaiseAndSetIfChanged(ref _entryTime, value); }
        }


        string _period;
        public string Period
        {
            get { return _period; }
            set { this.RaiseAndSetIfChanged(ref _period, value); }
        }


        string _studentNum;
        public string StudentNumber
        {
            get { return _studentNum; }
            set { this.RaiseAndSetIfChanged(ref _studentNum, value); }
        }
        
        string _attendanceCode;
        public string AttendanceCode
        {
            get { return _attendanceCode; }
            set { this.RaiseAndSetIfChanged(ref _attendanceCode, value); }
        }


        /*
        readonly ObservableAsPropertyHelper<string> _period;
        public string Period
        {
            get { return _period.Value; }
        }
       
        readonly ObservableAsPropertyHelper<string> _attendanceCode;
        public string AttendanceCode
        {
            get { return _attendanceCode.Value; }
        }
       */


        string _desc;
        public string PassDescription
        {
            get { return _desc; }
            set { this.RaiseAndSetIfChanged(ref _desc, value); }
        }

        string _homeroom;
        public string Homeroom
        {
            get { return _homeroom; }
            set { this.RaiseAndSetIfChanged(ref _homeroom, value); }
        }

        string _grade;
        public string Grade
        {
            get { return _grade; }
            set { this.RaiseAndSetIfChanged(ref _grade, value); }
        }


        string _room;
        public string Room
        {
            get { return _room; }
            set { this.RaiseAndSetIfChanged(ref _room, value); }
        }

        string _entryStatus;
        public string EntryStatus
        {
            get { return _entryStatus; }
            set { this.RaiseAndSetIfChanged(ref _entryStatus, value); }
        }

        readonly ObservableAsPropertyHelper<string> _gradeLabel;
        public string GradeLabel
        {
            get { return _gradeLabel.Value; }
        }

        readonly ObservableAsPropertyHelper<string> _locationText;
        public string LocationText
        {
            get { return _locationText.Value; }
        }

        readonly ObservableAsPropertyHelper<string> _homeroomLabel;
        public string HomeroomLabel
        {
            get { return _homeroomLabel.Value; }
        }

       
        public string StationName
        {
            get { return Environment.MachineName; }
        }

        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3},{4},{5}", SwipeLane, SwipeMode, StudentId, ScanLocation, Barcode, EntryTime);
        }
    }
}
