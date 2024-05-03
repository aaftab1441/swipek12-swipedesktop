using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using ReactiveUI;

namespace SwipeDesktop.ViewModels
{
    public class ScanLocation : ReactiveObject
    {

        public ScanLocation()
        {

            InitObservables();

        }

        void InitObservables()
        {
            this.WhenAnyValue(x => x.RoomName, x=>x.Type).Where(x => x.Item2 == LocationType.Tardy || x.Item2 == LocationType.Release).Subscribe(x =>
            {
                if (x.Item1 != null)
                {
                    try
                    {
                        string[] charArrayAtt = x.Item1.Split("()".ToCharArray());
                        string[] charArrayPer = x.Item1.Split("\"\"".ToCharArray());

                        if (charArrayAtt.Length > 1)
                            AttendanceCode = string.Format("{0}", charArrayAtt[1]);

                        if (charArrayPer.Length > 1)
                            PeriodCode = string.Format("{0}", charArrayPer[1]);
                    }
                    catch
                    {
                        //do nothing - just move on
                    }
                }

            });
        }

        public long Id { get; set; }

        string _roomName;
        public string RoomName
        {
            get { return _roomName; }
            set { this.RaiseAndSetIfChanged(ref _roomName, value); }
        }

        int _inOut;
        public int InOut
        {
            get { return _inOut; }
            set { this.RaiseAndSetIfChanged(ref _inOut, value); }
        }

        public int SchoolId { get; set; }

        LocationType _type;
        public LocationType Type
        {
            get { return _type; }
            set { this.RaiseAndSetIfChanged(ref _type, value); }
        }

        string _attCode;
        public string AttendanceCode
        {
            get { return _attCode; }
            set { this.RaiseAndSetIfChanged(ref _attCode, value); }
        }


        string _periodCode;
        public string PeriodCode
        {
            get { return _periodCode; }
            set { this.RaiseAndSetIfChanged(ref _periodCode, value); }
        }

        bool _allowMultipleScans;
        public bool AllowMultipleScans
        {
            get { return _allowMultipleScans; }
            set { this.RaiseAndSetIfChanged(ref _allowMultipleScans, value); }
        }


    }
}
