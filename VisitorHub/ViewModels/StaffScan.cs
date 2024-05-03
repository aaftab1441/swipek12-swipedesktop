using System;
using System.Collections.Generic;
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
    public class StaffScan : Scan
    {

        public StaffRecord StaffScanModel { get; private set; } //StaffScanModel

        public StaffScan()
        {
            StaffScanModel = new StaffRecord();

            this.WhenAnyValue(x => x.AttendanceCode).BindTo(this, x => x.StaffScanModel.StatusCode);
            this.WhenAnyValue(x => x.Barcode).BindTo(this, x => x.StaffScanModel.Barcode);
            this.WhenAnyValue(x => x.EntryTime).BindTo(this, x => x.StaffScanModel.EntryTime);
            this.WhenAnyValue(x => x.PersonId).BindTo(this, x => x.StaffScanModel.PersonId);
            this.WhenAnyValue(x => x.SwipeMode).BindTo(this, x => x.StaffScanModel.SwipeMode);
            this.WhenAnyValue(x => x.IsManualSwipe).BindTo(this, x => x.StaffScanModel.IsManual);
            this.WhenAnyValue(x => x.Room).BindTo(this, x => x.StaffScanModel.Location);
            this.WhenAnyValue(x => x.IsLeavingLocation).Where(x=>x == true).BindTo(this, x => x.StaffScanModel.SwipedOut);
            this.WhenAnyValue(x => x.IsKiosk).BindTo(this, x => x.StaffScanModel.IsKiosk);


            this.WhenAnyValue(x => x.EntryTime, x => x.Room)
                .Subscribe(x =>
                {
                    var display = string.Format("{1} - {0}", x.Item1.ToString("h:mm:ss tt"), x.Item2);

                    this.EntryModeDisplay = display;
                });

            PersonId = 0;
        }

        int _Id;
        public int PersonId
        {
            get { return _Id; }
            set { this.RaiseAndSetIfChanged(ref _Id, value); }
        }

        bool _isKiosk;
        public bool IsKiosk
        {
            get { return _isKiosk; }
            set { this.RaiseAndSetIfChanged(ref _isKiosk, value); }
        }
        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3},{4},{5}", SwipeLane, SwipeMode, PersonId, ScanLocation, Barcode, EntryTime);
        }
    }
}
