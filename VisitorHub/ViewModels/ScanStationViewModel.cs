using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlTypes;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.Configuration;
using System.Printing;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Xps.Packaging;
using Common;
using log4net;
using Newtonsoft.Json;
using ReactiveDialog.Implementations;
using ReactiveUI;

using ServiceStack;
using ServiceStack.Common.Extensions;
using ServiceStack.Redis;
using Simple.Data;
using Simple.Data.Extensions;
using SwipeDesktop.Api;
using SwipeDesktop.Common;
using SwipeDesktop.Interfaces;
using SwipeDesktop.Modal;
using SwipeDesktop.Models;
using SwipeDesktop.Storage;
using SwipeDesktop.Views;
using SwipeK12;
using Telerik.Windows.Media.Imaging;
using Telerik.Windows.Media.Imaging.FormatProviders;
using System.Xml;
using Common.Models;
using Telerik.Windows.Documents.Spreadsheet.Expressions.Functions;
using Exception = System.Exception;


namespace SwipeDesktop.ViewModels
{

    public interface IScanStationViewModel : IRoutableViewModel, IViewModel
    {

    }


    public class ScanStationViewModel : ReactiveObject, IScanStationViewModel
    {
        private int scanHistoryCount = 50;
        //Uri goodScan = new Uri(@"pack://siteoforigin:,,,/Resources/ding.wav");
        //Uri badScan = new Uri(@"pack://siteoforigin:,,,/Resources/badscan.wav");
        private string goodScan = Settings.Default.SoundsFolder + "\\ding.wav";
        private string badScan = Settings.Default.SoundsFolder + "\\badscan.wav";

        private ScanUtility _scanUtility;

        private static readonly ILog Logger = LogManager.GetLogger(typeof(ScanStationViewModel));

        Dictionary<string, string> addFields = new Dictionary<string, string>();

        static readonly SwipeCardBLL IdCardBll = new SwipeCardBLL();

        public RemoteStorage RemoteStorage { get; private set; }

        public DetentionStorage Detentions { get; private set; }

        public IPopupViewModelLocator DialogService { get; private set; }

        ScanStorage Scans { get; set; }

        StaffScanStorage StaffScans { get; set; }

        InOutStorage LocationScans { get; set; }

        DismissalStorage Dismissals { get; set; }

        AlertPrintedStorage AlertsPrinted { get; set; }

        FineStorage Fines { get; set; }

        IdCardStorage IdCards { get; set; }

        private dynamic SchoolSettings;

        private IEnumerable<SchoolStartTime> SchoolStartTimes;
        private IEnumerable<StudentStartTime> StudentStartTimes;

        public LocalStorage LocalStorage { get; private set; }

        static readonly ReactiveList<ScanMode> _items = new ReactiveList<ScanMode>(new[] { new ScanMode() { Name = "Group Scan Mode", Type = SwipeMode.Group }, new ScanMode() { Name = "Period Scan Mode", Type = SwipeMode.ClassroomTardy }, new ScanMode() { Name = "Normal Scan Mode", Type = SwipeMode.Entry }, new ScanMode() { Name = "Location Mode", Type = SwipeMode.Location }, new ScanMode() { Name = "Café Entrance Mode", Type = SwipeMode.CafeEntrance } });

        public ReactiveList<ScanLocation> Locations { get; set; }
       
        private ReactiveDataSource<ScanMode> scanModeList = new ReactiveDataSource<ScanMode>()
        {
            ItemsSource = _items,
            SelectedItem = _items.FirstOrDefault(x=>(SwipeMode)Enum.Parse(typeof(SwipeMode),Settings.Default.StartupScanMode) == x.Type) ?? _items.FirstOrDefault(),
            Label="Mode:"
        };

        public ReactiveDataSource<ScanMode> ScanModeList
        {
            get { return scanModeList; }
            set
            {
                this.RaiseAndSetIfChanged(ref scanModeList, value);
            }
        }

        private ReactiveDataSource<string> _attendanceCodeList;

        public ReactiveDataSource<string> AttendanceCodeList
        {
            get { return _attendanceCodeList; }
            set
            {
                this.RaiseAndSetIfChanged(ref _attendanceCodeList, value);
            }
        }

        private string _title;

        public string ApplicationTitle
        {
            get { return _title; }
            set
            {
                this.RaiseAndSetIfChanged(ref _title, value);
            }
        }

        private bool _offline = false;

        public bool Offline
        {
            get { return _offline; }
            set
            {
                this.RaiseAndSetIfChanged(ref _offline, value);
            }
        }

        private ReactiveDataSource<string> _periodList;

        public ReactiveDataSource<string> PeriodList
        {
            get { return _periodList; }
            set
            {
                this.RaiseAndSetIfChanged(ref _periodList, value);
            }
        }

        private ReactiveDataSource<string> _locationList;

        public ReactiveDataSource<string> LocationList
        {
            get { return _locationList; }
            set
            {
                this.RaiseAndSetIfChanged(ref _locationList, value);
            }
        }

        
        StudentModel _selectedStudent;
        public StudentModel SelectedStudent
        {
            get { return _selectedStudent; }
            set { this.RaiseAndSetIfChanged(ref _selectedStudent, value); }
        }

        PersonModel _selectedStaff;
        public PersonModel SelectedStaff
        {
            get { return _selectedStaff; }
            set { this.RaiseAndSetIfChanged(ref _selectedStaff, value); }
        }

        bool _dismissal;
        public bool Dismissal
        {
            get { return _dismissal; }
            set { this.RaiseAndSetIfChanged(ref _dismissal, value); }
        }

        bool _markAllPresent;
        public bool MarkAllPresent
        {
            get { return _markAllPresent; }
            set { this.RaiseAndSetIfChanged(ref _markAllPresent, value); }
        }

        bool _printPasses;
        public bool PrintPasses
        {
            get { return _printPasses; }
            set { this.RaiseAndSetIfChanged(ref _printPasses, value); }
        }


        bool _inOutPasses;
        public bool InOutPasses
        {
            get { return _inOutPasses; }
            set { this.RaiseAndSetIfChanged(ref _inOutPasses, value); }
        }

        string _barcode;
        public string Barcode
        {
            get { return _barcode; }
            set { this.RaiseAndSetIfChanged(ref _barcode, value); }
        }

        /*
        string _searchText;
        public string SearchText
        {
            get { return _searchText; }
            set { this.RaiseAndSetIfChanged(ref _searchText, value); }
        }*/

        string _connected = "Not Connected";
        public string Connected
        {
            get { return _connected; }
            set
            {
                this.RaiseAndSetIfChanged(ref _connected, value);
            }
        }

        private ObservableAsPropertyHelper<int> _queuedRecords;
        public int QueuedRecords
        {
            get { return _queuedRecords.Value; }
        }

        private ObservableAsPropertyHelper<string> _queuedRecordsText;
        public string QueuedRecordsText
        {
            get { return _queuedRecordsText.Value; }
        }

        private ObservableAsPropertyHelper<string> _studentRecordsText;
        public string StudentRecordsText
        {
            get { return _studentRecordsText.Value; }
        }

        private ObservableAsPropertyHelper<string> _syncErrorsText;
        public string SyncErrorsText
        {
            get { return _syncErrorsText.Value; }
        }

        private ObservableAsPropertyHelper<Tuple<SwipeMode, bool>> _modeSettings;
        public Tuple<SwipeMode, bool> ModeSettings
        {
            get { return _modeSettings.Value; }
        }

        private ObservableAsPropertyHelper<SwipeMode> _swipeMode;
        public SwipeMode SwipeMode
        {
            get { return _swipeMode.Value; }
        }
        private ObservableAsPropertyHelper<string> _selectedLocation;
        public string SelectedLocation
        {
            get { return _selectedLocation.Value; }
        }

        int _queuedScanRecords;
        public int QueuedScanRecords
        {
            get { return _queuedScanRecords; }
            set
            {
                this.RaiseAndSetIfChanged(ref _queuedScanRecords, value);
            }
        }

        int _syncErrorRecords;
        public int SyncErrorRecords
        {
            get { return _syncErrorRecords; }
            set
            {
                this.RaiseAndSetIfChanged(ref _syncErrorRecords, value);
            }
        }

        int _studentScanRecords;
        public int StudentScanRecords
        {
            get { return _studentScanRecords; }
            set
            {
                this.RaiseAndSetIfChanged(ref _studentScanRecords, value);
            }
        }

        int _queuedDetentionRecords;
        public int QueuedDetentionRecords
        {
            get { return _queuedDetentionRecords; }
            set
            {
                this.RaiseAndSetIfChanged(ref _queuedDetentionRecords, value);
            }
        }

        int _queuedFines;
        public int QueuedFines
        {
            get { return _queuedFines; }
            set
            {
                this.RaiseAndSetIfChanged(ref _queuedFines, value);
            }
        }

        int _queuedIdCards;
        public int QueuedIdCards
        {
            get { return _queuedIdCards; }
            set
            {
                this.RaiseAndSetIfChanged(ref _queuedIdCards, value);
            }
        }


        int _queuedLocationRecords;
        public int QueuedLocationScans
        {
            get { return _queuedLocationRecords; }
            set
            {
                this.RaiseAndSetIfChanged(ref _queuedLocationRecords, value);
            }
        }

        int _queuedDismissalRecords;
        public int QueuedDismissalRecords
        {
            get { return _queuedDismissalRecords; }
            set
            {
                this.RaiseAndSetIfChanged(ref _queuedDismissalRecords, value);
            }
        }

        string _activity;
        public string CurrentActivity
        {
            get { return _activity; }
            set
            {
                this.RaiseAndSetIfChanged(ref _activity, value);
            }
        }

        private ReactiveList<Scan> _laneScans = new ReactiveList<Scan>();

        public ReactiveList<Scan> LaneScans
        {
            get { return _laneScans; }
            set
            {
                this.RaiseAndSetIfChanged(ref _laneScans, value);
            }
        }


        private ReactiveList<Scan> _leftLaneScans = new ReactiveList<Scan>();

        public ReactiveList<Scan> LeftLaneScans
        {
            get { return _leftLaneScans; }
            set
            {
                this.RaiseAndSetIfChanged(ref _leftLaneScans, value);
            }
        }

        private ReactiveList<PersonModel> _searchResults = new ReactiveList<PersonModel>();

        public ReactiveList<PersonModel> SearchResults
        {
            get { return _searchResults; }
            set
            {
                this.RaiseAndSetIfChanged(ref _searchResults, value);
            }
        }

        public void DismissStudent(Dismissal dismiss)
        {
           
            Dismissals.InsertObject(dismiss);
            
        }

        public void AlertWasPrinted(AlertPrinted alert)
        {

            //AlertsPrinted.InsertObject(alert);

            var deactivateNow = alert.Details.ToUpper().Contains("CUT");

            LocalStorage.DeactivateAlert(alert.AlertId, deactivateNow);

        }

        public void ClearLeftLaneScanList()
        {
            LeftLaneScans.Clear();
        }

        TimeSpan GetSchoolStart(Scan scan)
        {
            if (scan != null)
            {
                try
                {
                    var barcode = scan.Barcode;

                    if (StudentStartTimes.Any(x => x.StudentNumber == barcode))
                    {
                        var startTime = StudentStartTimes.Single(x => x.StudentNumber == barcode);
                        if (DateTime.TryParse(startTime.StartTime.Trim().Replace(":000",""), out var start))
                        {
                            return start.TimeOfDay;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Could not get custom student start time", ex);
                }

                try
                {
                    var grade = scan.Grade;

                    if (SchoolStartTimes.Any(x => x.Grade == grade))
                    {
                        if (DateTime.TryParse(SchoolStartTimes.Single(x => x.Grade == grade).StartTime, out var start))
                        {
                            return start.TimeOfDay;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Could not get custom start time", ex);
                }
            }

            return SchoolSettings.DayStartTime.TimeOfDay;
        }

        public void ClearRightLaneScanList()
        {
            LaneScans.Clear();
        }

        public void Swipe(Scan scan)
        {
            var startTime = GetSchoolStart(scan);
            var current = DateTime.Now;

            scan.IsStaffScan = false;

            bool recordSwipe = !(scan.StudentName.Contains("INVALID SCAN") || scan.StudentName.Contains("ALREADY SCANNED") || scan.AlreadySwiped);

            if (recordSwipe)
            {
                if (MarkAllPresent)
                {
                    scan.EntryStatus = "PRE";
                    scan.MarkAllPresentMode = true;
                }
                else
                {
                    scan.EntryStatus = startTime > DateTime.Now.TimeOfDay ? "PRE" : "LTE";
                }

                if (SwipeMode == SwipeMode.CafeEntrance)
                {
                    
                    if (!scan.HasLunchAlert)
                    {
                        //todo: handle cafe enrance scan storage
                        var location = new LocationScan()
                        {
                            StudentNumber = scan.Barcode,
                            SwipeTime = scan.EntryTime,
                            //SwipedOut = isOut,
                            RoomName = string.Format("Lunch {0}", PeriodList.SelectedItem),
                            MarkAllPresent = scan.MarkAllPresentMode
                        };

                        LocationScans.InsertObject(location);
                    }
                }

                if (SwipeMode == SwipeMode.Group)
                {
                   
                    if (!scan.HasAlerts)
                    {
                        var location = new LocationScan()
                        {
                            StudentNumber = scan.Barcode,
                            SwipeTime = scan.EntryTime,
                            //SwipedOut = isOut,
                            RoomName = string.Format("Group {0}", scan.Room),
                            MarkAllPresent = scan.MarkAllPresentMode
                        };

                        LocationScans.InsertObject(location);
                    }
                }

                if (SwipeMode == SwipeMode.Entry)
                {
                    scan.DataModel.Location = null;

                    Scans.InsertObject(scan.DataModel);

                }

                if (SwipeMode == SwipeMode.ClassroomTardy)
                {
                    //tardy mode should never allow wmark all present mode
                    scan.MarkAllPresentMode = false;

                    //scan.DataModel.TakeAttendance = Settings.Default.TakeAttendance;
                    Scans.InsertObject(scan.DataModel);

                    LocalStorage.InsertTardySwipe(scan);
                }

                if (SwipeMode == SwipeMode.Location)
                {
                    //var entry = LocalStorage.CheckEntrySwipe(scan.StudentId);

                    var currentSwipeTime = scan.EntryTime;

                    var room = Environment.MachineName;

                    if (scan.ScanLocation != null)
                    {
                        room = scan.ScanLocation.RoomName;
                    }
                    
                    scan.Room = room;
                    var locationsScanned = LocationScans.GetItemsByStudent(room, currentSwipeTime, scan.Barcode);

                        if (locationsScanned.Count() % 2 != 0)
                        {
                            scan.IsLeavingLocation = true;
                        }

                        LocationScans.InsertObject(new LocationScan()
                        {
                            StudentNumber = scan.Barcode,
                            SwipeTime = currentSwipeTime,
                            //SwipedOut = isOut,
                            RoomName = room,
                            MarkAllPresent = scan.MarkAllPresentMode
                    });
                }
            }

            var elapsed = (DateTime.Now - current).TotalSeconds;

            Logger.WarnFormat("{0} total seconds elapsed on swipe", elapsed);

            if (scan.Alerts.Any())
                scan.AlertDisplayText = scan.Alerts[0].AlertText;

            if (SwipeMode == SwipeMode.CafeEntrance && Settings.Default.EnableLunchAlerts)
            {
                if (scan.Alerts.Count() > 1)
                    scan.AlertDisplayText = scan.AlertDisplayText + "\n" + scan.Alerts[1].AlertText;
               
            }
           

            if (scan.SwipeLane == Lane.Left)
            {
               
                LeftLaneScans.Insert(0, scan);
                
                //TrimList(LeftLaneScans);
            }
            else
            {
                current = DateTime.Now;
                LaneScans.Insert(0, scan);
                elapsed = (DateTime.Now - current).TotalSeconds;

                Logger.WarnFormat("{0} seconds elapsed on Lane insert", elapsed);

                //TrimList(LaneScans);
            }
        }

        public void StaffSwipe(StaffScan scan)
        {
            var startTime = SchoolSettings.DayStartTime.TimeOfDay;
            scan.IsStaffScan = true;

            bool recordSwipe = !(scan.StudentName.Contains("INVALID SCAN") || scan.StudentName.Contains("ALREADY SCANNED") || scan.AlreadySwiped);

            if (recordSwipe)
            {
                
                if (MarkAllPresent)
                {
                    scan.EntryStatus = "PRE";
                    scan.AttendanceCode = "PRE";
                    scan.MarkAllPresentMode = true;
                }
                else
                {
                    scan.EntryStatus = startTime > DateTime.Now.TimeOfDay ? "PRE" : "LTE";
                    scan.AttendanceCode = scan.EntryStatus;
                }

                scan.Room = "Staff Scan";

                var currentSwipeTime = scan.EntryTime;
                var locationsScanned = LocationScans.GetItemsByStudent(scan.Room, currentSwipeTime, scan.Barcode);

                if (locationsScanned.Count() % 2 != 0)
                {
                    scan.IsLeavingLocation = true;
                }
                
                StaffScans.InsertObject(scan.StaffScanModel);

            }

            if (scan.SwipeLane == Lane.Left)
            {

                LeftLaneScans.Insert(0, scan);

                TrimList(LeftLaneScans);
            }
            else
            {

                LaneScans.Insert(0, scan);

                TrimList(LaneScans);
            }
        }

        public void TrimList(ReactiveList<Scan> scans )
        {
            try
            {
                var count = scans.Count;
                if (count == scanHistoryCount)
                {
                    var startIndex = (scanHistoryCount/2) - 1;
                    scans.RemoveRange(startIndex, scanHistoryCount / 2);
                    /*while (scans.Count > count/2)
                    {
                        scans.RemoveAt(scans.Count-1);
                    }*/
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Problem trimming scan history", ex);
            }
        }

        public string UrlPathSegment
        {
            get { return "scanStation"; }
        }

        public IScreen HostScreen { get; protected set; }

        private IViewModel _currentContent;

        public IViewModel CurrentView
        {
            get { return _currentContent; }
            set { this.RaiseAndSetIfChanged(ref _currentContent, value); }
        }

        private bool _isSyncing;

        public bool IsSyncing
        {
            get { return _isSyncing; }
            set { this.RaiseAndSetIfChanged(ref _isSyncing, value); }
        }


        private PopupViewModel _currentModal;

        public PopupViewModel CurrentDialogViewModel
        {
            get { return _currentModal; }
            set { this.RaiseAndSetIfChanged(ref _currentModal, value); }
        }


        private bool _showCamera;

        public bool ShowCameraDialog
        {
            get { return _showCamera; }
            set { this.RaiseAndSetIfChanged(ref _showCamera, value); }
        }

        public ScanStationViewModel(IScreen screen) : this()
        {
            HostScreen = screen;
        }

        private RadBitmap _bitmap;

        //[JsonIgnore]
        public RadBitmap ImageCapture { get { return _bitmap; } set { this.RaiseAndSetIfChanged(ref _bitmap, value); } }

        public void RaiseCameraPopup(object source, PersonModel person, bool raisedFromFines = false)
        {
            var lvi = source as ListViewItem;

            ShowCameraDialog = true;
            //CurrentDialogViewModel = new PopupViewModel();

            CurrentDialogViewModel.ShowPopup = false;

            CurrentDialogViewModel.VerticalOffset = -30;

            var origHeight = CurrentDialogViewModel.Height;
            var origWidth = CurrentDialogViewModel.Width;

            CurrentDialogViewModel.Height = 370;
            CurrentDialogViewModel.Width = 600;
            CurrentDialogViewModel.Placement = PlacementMode.Bottom;

            CurrentDialogViewModel.ConfirmText = "save".ToUpper();
            CurrentDialogViewModel.CancelText = "close".ToUpper();
            CurrentDialogViewModel.HideAction = (x) =>
            {


                if (raisedFromFines)
                {
                    CurrentDialogViewModel.Width = origWidth;
                    CurrentDialogViewModel.Height = origHeight;
                    CurrentDialogViewModel.ShowPopup = true;

                    CurrentDialogViewModel.HideAction = (y) =>
                    {
                        CurrentDialogViewModel.ShowPopup = false;

                        MessageBus.Current.SendMessage(new Tuple<string>("SetFocusStudentList"));
                    };
                    CurrentDialogViewModel.SaveAction = (o) =>
                    {

                        CurrentDialogViewModel.ShowPopup = false;
                        MessageBus.Current.SendMessage(new Tuple<string>("FineAdded"));

                        SaveFine(o);

                    };
                }

                ShowCameraDialog = false;

                CurrentDialogViewModel.ShowPopup = false;

            };


            CurrentDialogViewModel.SaveAction = (o) =>
            {
                if (this.ImageCapture == null)
                {
                    return;
                }
                //var folderPath = string.Format("{0}\\{1}", Settings.Default.ImagesFolder, Settings.Default.SchoolId);
                var folderPath = string.Format("{0}", Settings.Default.ImagesFolder);
                var imageName = string.Format("{0}.jpg", person.IdNumber);
                var imagePath = string.Format("{0}\\{1}", folderPath, imageName);
                string extension = System.IO.Path.GetExtension(imagePath).ToLower();

                IImageFormatProvider formatProvider = ImageFormatProviderManager.GetFormatProviderByExtension(extension);

                using (Stream fs = new FileStream(imagePath, FileMode.Create))
                {

                    formatProvider.Export(this.ImageCapture, fs);

                    fs.Close();
                }

                if (lvi != null)
                {
                    var content = lvi.Content as PersonModel;
                    if (content != null)
                    {
                        content.PhotoPath = null;
                        content.PhotoPath = imageName;
                        //content.refreshPhoto(imageName);
                        //content.Image = content.GetImage(imageName);
                        //StudentsFound.Clear();

                    }
                }

                ShowCameraDialog = false;
            };

            MessageBus.Current.Listen<Tuple<string, BitmapSource>>().Subscribe(_ =>
            {
                var msg = _;

                if (msg.Item1 == "ImageCaptured")
                {
                    //var folderPath = string.Format("{0}\\{1}", Settings.Default.ImagesFolder, Settings.Default.SchoolId);
                    string imagePath = null;
                    if (SelectedStudent != null)
                    {
                        imagePath = SaveImage(msg.Item2, SelectedStudent.IdNumber);
                    }
                    if (SelectedStaff != null)
                    {
                        imagePath = SaveImage(msg.Item2, SelectedStaff.IdNumber);
                    }

                    if (imagePath != null)
                    {
                        string extension = Path.GetExtension(imagePath).ToLower();

                        //IImageFormatProvider formatProvider = ImageFormatProviderManager.GetFormatProviderByExtension(extension);
                        //ImageCapture = formatProvider.Import(BitmapSourceToArray(msg.Item2));

                        IImageFormatProvider formatProvider =
                            ImageFormatProviderManager.GetFormatProviderByExtension(extension);
                        //Image image = Image.FromFile(resource);
                        MemoryStream mmStream = new MemoryStream();

                        using (FileStream fsStream = File.OpenRead(imagePath))
                        {
                            fsStream.CopyTo(mmStream);
                        }
                        mmStream.Seek(0, SeekOrigin.Begin);
                        ImageCapture = formatProvider.Import(mmStream);
                    }
                    //ImageCapture = new RadBitmap(new WriteableBitmap(msg.Item2));
                }

            });
        }

        private byte[] BitmapSourceToArray(BitmapSource bitmapSource)
        {
            // Stride = (width) x (bytes per pixel)
            int stride = (int)bitmapSource.PixelWidth * (bitmapSource.Format.BitsPerPixel / 8);
            byte[] pixels = new byte[(int)bitmapSource.PixelHeight * stride];

            bitmapSource.CopyPixels(pixels, stride, 0);

            return pixels;
        }

        string SaveImage(BitmapSource bitmap, string studentNumber)
        {
            var folderPath = string.Format("{0}", Settings.Default.ImagesFolder);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var imageName = string.Format("{0}.jpg", studentNumber);
            var imagePath = string.Format("{0}{1}", folderPath, imageName);

            var encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (var fs = new FileStream(imagePath, FileMode.Create))
            {
                encoder.Save(fs);
                fs.Close();
            }

            return imagePath;
        }
        void LoadImageForEdit()
        {
            
        }

        public void RaiseStudentAltId(StudentModel student)
        {
            
            CurrentDialogViewModel.ShowPhotoButton = false;

            //var vm = new StudentAlternateIdViewModel();
            var vm = DialogService.LocateDialog(DialogConstants.StudentAlternateId.ToString()) as StudentAlternateIdViewModel;

            if (vm != null)
            {
                vm.Student = student;
              
                //vm.PopupWindow = CurrentDialogViewModel;
            }

            CurrentDialogViewModel.CurrentContent = vm;
            CurrentDialogViewModel.Title = "Attach Alternate ID";

            //CalcViewSize(CurrentDialogViewModel);
            CurrentDialogViewModel.Height = 350;
            CurrentDialogViewModel.Width = 530;

            CurrentDialogViewModel.VerticalOffset = -30;
            //CurrentDialogViewModel.HorizontalOffset = 505;
            CurrentDialogViewModel.Placement = PlacementMode.Bottom;
            CurrentDialogViewModel.ConfirmText = "ok".ToUpper();
            CurrentDialogViewModel.CancelText = "cancel".ToUpper();

            CurrentDialogViewModel.HideAction = (x) =>
            {
                CurrentDialogViewModel.CurrentContent = new NullViewModel();
                CurrentDialogViewModel.ShowPopup = false;
                MessageBus.Current.SendMessage(new Tuple<string>("SetFocusStudentList"));
            };


            CurrentDialogViewModel.SaveAction = (o) =>
            {
                //todo: save id 
                CurrentDialogViewModel.CurrentContent = new NullViewModel();
                CurrentDialogViewModel.ShowPopup = false;
                MessageBus.Current.SendMessage(new Tuple<string>("SetFocusStudentList"));
            };

            CurrentDialogViewModel.ShowPopup = true;

            MessageBus.Current.SendMessage(new Tuple<string>("AltIdOpened"));

        }

        private void RaiseSettingsPopup()
        {
            CurrentDialogViewModel.ShowPhotoButton = false;
            CurrentDialogViewModel.CurrentContent = DialogService.LocateDialog(DialogConstants.SettingsDialog.ToString());

            //CurrentDialogViewModel.ConfirmPopupCommand = new DelegateCommand(() => SendMessage(MessageTypes.ConfirmSettings, new NotificationEventArgs(string.Empty)));
            CurrentDialogViewModel.HideAction = (x) =>
            {
                CancelSettings(x);
                CurrentDialogViewModel.ShowPopup = false;


                MessageBus.Current.SendMessage(new Tuple<string>("SettingsClosed"));
            };

            CurrentDialogViewModel.SaveAction = (o) => { 
                //todo: save settings
                SaveSettings(o);
                CurrentDialogViewModel.ShowPopup = false;
            };

            var settings = CurrentDialogViewModel.CurrentContent as SettingsViewModel;

            if (settings != null)
            {
                CurrentDialogViewModel.ShowPopup = true;
                settings.SelectedTabIndex = 0;
                settings.PopupWindow = CurrentDialogViewModel;
                settings.SchoolName = Settings.Default.School;
                settings.DatabaseStats = new ReactiveList<Tuple<string, int>>(LocalStorage.DatabaseStats());
            }
            CurrentDialogViewModel.Title = "Settings";

            CalcViewSize(CurrentDialogViewModel);
           
            CurrentDialogViewModel.VerticalOffset = -30;
            //CurrentDialogViewModel.HorizontalOffset = 505;
            CurrentDialogViewModel.Placement = PlacementMode.Bottom;
            CurrentDialogViewModel.ConfirmText = "save".ToUpper();
            CurrentDialogViewModel.CancelText = "cancel".ToUpper();
            
            
        }

        public void RaiseAddPersonPopup()
        {
            CurrentDialogViewModel.ShowPhotoButton = false;
            CurrentDialogViewModel.CurrentContent = DialogService.LocateDialog(DialogConstants.AddPersonViewModel.ToString());

            //CurrentDialogViewModel.ConfirmPopupCommand = new DelegateCommand(() => SendMessage(MessageTypes.ConfirmSettings, new NotificationEventArgs(string.Empty)));
            CurrentDialogViewModel.HideAction = (x) =>
            {
                CurrentDialogViewModel.ShowPopup = false;
                ResetPerson(x);

                MessageBus.Current.SendMessage(new Tuple<string>("SetFocusSearchBox"));
                //MessageBus.Current.SendMessage(new Tuple<string>("SettingsClosed"));
            };

            CurrentDialogViewModel.SaveAction = (o) =>
            {
                SavePerson(o);
                
                MessageBus.Current.SendMessage(new Tuple<string>("SetFocusSearchBox"));
            };

            var vm = CurrentDialogViewModel.CurrentContent as AddPersonViewModel;

            if (vm != null)
            {
                CurrentDialogViewModel.ShowPopup = true;
            }

            CurrentDialogViewModel.Title = "Add New Person";

            CalcViewSize(CurrentDialogViewModel);

            CurrentDialogViewModel.Height = 510;
            CurrentDialogViewModel.Width = 650;
            CurrentDialogViewModel.VerticalOffset = -30;
            //CurrentDialogViewModel.HorizontalOffset = 505;
            CurrentDialogViewModel.Placement = PlacementMode.Bottom;
            CurrentDialogViewModel.ConfirmText = "save".ToUpper();
            CurrentDialogViewModel.CancelText = "cancel".ToUpper();


        }

        public void RaiseSelectedStudents(int cardId, IEnumerable<StudentModel> items)
        {
            CurrentDialogViewModel.ShowPhotoButton = false;
            CurrentDialogViewModel.CurrentContent = DialogService.LocateDialog(DialogConstants.StudentsSelectedForPrint.ToString());

            CurrentDialogViewModel.HideAction = (x) =>
            {
                CurrentDialogViewModel.ShowPopup = false;

                //MessageBus.Current.SendMessage(new Tuple<string>("SettingsClosed"));
            };

            CurrentDialogViewModel.SaveAction = (o) =>
            {
                var callbackModel = CurrentDialogViewModel.CurrentContent as SelectedForPrintViewModel;

                if (callbackModel != null)
                {
                   
                    var checkedStudents = callbackModel.BatchItems.Where(x => x.IsChecked).ToArray();

                    if (checkedStudents.Any())
                    {
                       this.PrintBatchCards(new Tuple<int, IEnumerable<CheckedItem>>(callbackModel.CardId, checkedStudents));
                        //.Subscribe(PrintBatchCards);
                    }

                }

                CurrentDialogViewModel.ShowPopup = false;

            };

            var vm = CurrentDialogViewModel.CurrentContent as SelectedForPrintViewModel;

            if (vm != null)
            { 
               
                vm.CardId = cardId;
                vm.OnStudentsReturned(items);
            }

            CurrentDialogViewModel.Title = "Batch Print List";

            CalcViewSize(CurrentDialogViewModel);

            CurrentDialogViewModel.Height = 600;
            CurrentDialogViewModel.Width = 500;
            CurrentDialogViewModel.VerticalOffset = -30;
            //CurrentDialogViewModel.HorizontalOffset = 505;
            CurrentDialogViewModel.Placement = PlacementMode.Bottom;
            CurrentDialogViewModel.ConfirmText = "print".ToUpper();
            CurrentDialogViewModel.CancelText = "cancel".ToUpper();

            CurrentDialogViewModel.ShowPopup = true;
        }
        public void RaiseBatchPrintPopup()
        {
            CurrentDialogViewModel.ShowPhotoButton = false;
            CurrentDialogViewModel.CurrentContent = DialogService.LocateDialog(DialogConstants.BatchPrint.ToString());

            CurrentDialogViewModel.HideAction = (x) =>
            {
                CurrentDialogViewModel.ShowPopup = false;
             
                //MessageBus.Current.SendMessage(new Tuple<string>("SettingsClosed"));
            };

            CurrentDialogViewModel.SaveAction = (o) =>
            {
                var callbackModel = CurrentDialogViewModel.CurrentContent as BatchPrintViewModel;

                if (callbackModel != null)
                {
                    var batchBy = callbackModel.BatchModeList.SelectedItem;
                    var sortBy = callbackModel.SortModeList.SelectedItem;
                    var template = callbackModel.IdTemplateList.SelectedItem;
                    var templateId = template.Item2;

                    var items = callbackModel.BatchItems.Where(x => x.IsChecked).Select(x => x.Item);

                    if (items.Any())
                    {
                        LocalStorage.GetBatchPrintCards(Settings.Default.SchoolId, batchBy, sortBy, callbackModel.OnlyWIthImages, items, templateId)
                            .Subscribe(ViewBatch);
                    }

                }

            };

            var vm = CurrentDialogViewModel.CurrentContent as BatchPrintViewModel;

            if (vm != null)
            {
                CurrentDialogViewModel.ShowPopup = true;
            }

            CurrentDialogViewModel.Title = "Batch Print";

            CalcViewSize(CurrentDialogViewModel);

            CurrentDialogViewModel.Height = 600;
            CurrentDialogViewModel.Width = 800;
            CurrentDialogViewModel.VerticalOffset = -30;
            //CurrentDialogViewModel.HorizontalOffset = 505;
            CurrentDialogViewModel.Placement = PlacementMode.Bottom;
            CurrentDialogViewModel.ConfirmText = "continue".ToUpper();
            CurrentDialogViewModel.CancelText = "cancel".ToUpper();


        }

        void CalcViewSize(PopupViewModel view)
        {
            double width = System.Windows.SystemParameters.PrimaryScreenWidth; // * .70;
            double height = System.Windows.SystemParameters.PrimaryScreenHeight; // * .95;

            double ratio = System.Windows.SystemParameters.PrimaryScreenWidth /
                            System.Windows.SystemParameters.PrimaryScreenHeight;

          
            if (Math.Round(ratio) > 1.5)
            {
                if (width > 1680)
                {
                    height = height * .65;
                    width = width * .45;
                }
                else
                {
                    height = height * .80;
                    width = width * .60;
                }
            }
            else
            {
                height = height < 100 ? 650 : height * .90;
                width = width < 100 ? 800 : width * .75;
            }
            
         
            view.Height = int.Parse(Math.Round(height).ToString());
            view.Width = int.Parse(Math.Round(width).ToString());
        }

        private IViewModel _studentCardViewModel = null;
        private IViewModel _staffCardViewModel = null;

        public void RaiseFinePopup(StudentModel model, object source)
        {
            var student = model as StudentModel;

            //Logger.Warn(source);

            if (_studentCardViewModel == null)
            {
                Logger.Warn("Student Card ViewModel is null");
                _studentCardViewModel = DialogService.LocateDialog(DialogConstants.StudentCardDialog.ToString());
            }

            if (_studentCardViewModel == null)
            {
                Logger.Warn("ViewModel not located");
            }
            else
            {
                Logger.Warn("ViewModel:" + _studentCardViewModel.GetType());
            }

            var vm = (_studentCardViewModel as StudentCardViewModel);

            if (vm == null)
            {
                Logger.Warn("Not a Student Card ViewModel");
            }

            if (student != null)
            {

                Logger.Warn(student.StudentId);
                vm.CurrentStudent = student;
            }
            
            vm.RefreshFineStats();
            vm.PaidInFull = false;

            CurrentDialogViewModel.ShowPhotoButton = true;

            CurrentDialogViewModel.CurrentContent = _studentCardViewModel;

            CurrentDialogViewModel.HideAction = (x) =>
            {
                CancelFine(x);
                CurrentDialogViewModel.ShowPopup = false;

                MessageBus.Current.SendMessage(new Tuple<string>("SetFocusStudentList"));
            };

            CurrentDialogViewModel.ShowPhotoAction = (x) =>
            {
                RaiseCameraPopup(source, student, true);
            };

            //CurrentDialogViewModel.ConfirmPopupCommand = new DelegateCommand(() => SendMessage(MessageTypes.ConfirmSettings, new NotificationEventArgs(string.Empty)));

            CurrentDialogViewModel.SaveAction = (o) =>
            {

                CurrentDialogViewModel.ShowPopup = false;

                MessageBus.Current.SendMessage(new Tuple<string>("FineAdded"));

                SaveFine(o);
               
                SearchResults.Clear();
            };

            /*
            var fineModel = CurrentDialogViewModel.CurrentContent as FineViewModel;

            if (fineModel != null)
            {
               
            }*/

            CurrentDialogViewModel.Title = String.Format("{0}, {1} ID Card History", student.LastName, student.FirstName);

            CurrentDialogViewModel.Height = 450;
            CurrentDialogViewModel.Width = 600;
            CurrentDialogViewModel.VerticalOffset = -50;
            //CurrentDialogViewModel.HorizontalOffset = 505;
            CurrentDialogViewModel.Placement = PlacementMode.Bottom;
            CurrentDialogViewModel.ConfirmText = "ok".ToUpper();
            CurrentDialogViewModel.CancelText = "cancel".ToUpper();
            CurrentDialogViewModel.ShowPopup = true;

            var screen = this.HostScreen;
        }

        public void RaiseStaffPopup(PersonModel model, object source)
        {
            var person = model as PersonModel;


            if (_staffCardViewModel == null)
                _staffCardViewModel = DialogService.LocateDialog(DialogConstants.StaffCardDialog.ToString());

            var vm = (_staffCardViewModel as StaffCardViewModel);

            if (person != null)
            {
                vm.CurrentPerson = person;
            }

            vm.PrintPvcId = true;
            //vm.RefreshFineStats();
            //vm.PaidInFull = false;

            CurrentDialogViewModel.ShowPhotoButton = true;

            CurrentDialogViewModel.CurrentContent = _staffCardViewModel;

            CurrentDialogViewModel.HideAction = (x) =>
            {
                CancelFine(x);
                CurrentDialogViewModel.ShowPopup = false;

                MessageBus.Current.SendMessage(new Tuple<string>("SetFocusStudentList"));
            };

            CurrentDialogViewModel.ShowPhotoAction = (x) =>
            {
                RaiseCameraPopup(source, person, true);
            };

            //CurrentDialogViewModel.ConfirmPopupCommand = new DelegateCommand(() => SendMessage(MessageTypes.ConfirmSettings, new NotificationEventArgs(string.Empty)));

            CurrentDialogViewModel.SaveAction = (o) =>
            {

                CurrentDialogViewModel.ShowPopup = false;

                //

                //SaveFine(o);
                PrintStaffCard(o);
                MessageBus.Current.SendMessage(new Tuple<string>("FineAdded"));
                SearchResults.Clear();
            };

            /*
            var fineModel = CurrentDialogViewModel.CurrentContent as FineViewModel;

            if (fineModel != null)
            {
               
            }*/

            CurrentDialogViewModel.Title = String.Format("Print ID Card: {0}, {1}", person.LastName, person.FirstName);

            CurrentDialogViewModel.Height = 180;
            CurrentDialogViewModel.Width = 600;
            CurrentDialogViewModel.VerticalOffset = -50;
            //CurrentDialogViewModel.HorizontalOffset = 505;
            CurrentDialogViewModel.Placement = PlacementMode.Bottom;
            CurrentDialogViewModel.ConfirmText = "ok".ToUpper();
            CurrentDialogViewModel.CancelText = "cancel".ToUpper();
            CurrentDialogViewModel.ShowPopup = true;

            var screen = this.HostScreen;
        }

        private void CancelFine(object vm)
        {
            dynamic school = null;
            var popUp = vm as PopupViewModel;

            if (popUp != null)
            {
                var scvm = popUp.CurrentContent as StudentCardViewModel;

                if (scvm != null)
                {
                 
                }
            }
        }

        private void CancelSettings(object vm)
        {
            dynamic school = null;
            var popUp = vm as PopupViewModel;

            if (popUp != null)
            {
                var settings = popUp.CurrentContent as SettingsViewModel;

                if (settings != null)
                {
                    settings.SchoolId = Settings.Default.SchoolId.ToString();
                    settings.SchoolName = Settings.Default.School;
                    settings.TakeAttendanceInLocationMode = Settings.Default.TakeAttendance;
                    settings.MarkPresentInLocationMode = Settings.Default.MarkPresentInLocationMode;
                    settings.IncludeStaff = Settings.Default.IncludeStaff;
                    settings.EnableLunchAlerts = Settings.Default.EnableLunchAlerts;
                    settings.SuppressIdOnPass = Settings.Default.SuppressIdOnPass;

                    settings.AllowKioskDismissalPass = Settings.Default.AllowKioskDismissalPass;
                    settings.AllowKioskLocation = Settings.Default.AllowKioskLocation;
                    settings.AllowKioskLocationPass = Settings.Default.AllowKioskLocationPass;
                    settings.AllowKioskSearchName = Settings.Default.AllowKioskSearchName;
                    settings.AllowKioskTardyPass = Settings.Default.AllowKioskTardyPass;
                    
                }
            }
        }

        void PrintStaffCard(object vm)
        {
            //var screen = Application.Current.Windows[0];
            //var uiContext = TaskScheduler.FromCurrentSynchronizationContext();

            var popUp = vm as PopupViewModel;

            if (popUp != null)
            {
                var scvm = popUp.CurrentContent as StaffCardViewModel;

                if (scvm != null)
                {
                  
                    if (scvm.PrintTempId)
                    {
                        if (scvm.TempTemplate == null)
                        {
                            MessageBox.Show("Please make sure a Temporary Template is selected.");
                            return;
                        }
                        Application.Current.Dispatcher.Invoke(() => PrintStaffTempId(scvm, true), DispatcherPriority.DataBind);
                    }

                    if (scvm.PrintPvcId)
                    {
                        if (scvm.PvcTemplate == null)
                        {
                            MessageBox.Show("Please make sure a Student Template is selected.");
                            return;
                        }
                        Application.Current.Dispatcher.Invoke(() => PrintStaffPvcId(scvm, true), DispatcherPriority.DataBind);
                    }

                    /*if (scvm.PrintReceipt && scvm.ChargeFee)
                    {
                        var rcpt = new Models.Receipt();
                        rcpt.PrintDate = DateTime.Today;
                        rcpt.ChargeAmt = scvm.FineAmt.Amount;
                        rcpt.PaidAmt = scvm.FinePaid;
                        rcpt.Details = scvm.FineAmt.Name;
                        //rcpt.Grade = scvm.CurrentStudent.Grade;
                        //rcpt.Homeroom = scvm.CurrentStudent.Homeroom;
                        rcpt.ReceivedBy = scvm.AcceptedBy ?? string.Empty;
                        rcpt.StudentImage = scvm.CurrentPerson.Image;

                        var view = new Views.Receipt();
                        var alertPrint = new PrintModel<Models.Receipt>(rcpt);
                        alertPrint.Title = "Fee Paid Receipt";
                        alertPrint.SchoolName = Settings.Default.School;
                        view.DataContext = alertPrint;

                        Application.Current.Dispatcher.Invoke(() => PrintReceipt(view), DispatcherPriority.DataBind);


                    }*/
                }
            }
        }

        void SaveFine(object vm)
        {
            //var screen = Application.Current.Windows[0];
            //var uiContext = TaskScheduler.FromCurrentSynchronizationContext();
          
            var popUp = vm as PopupViewModel;

            if (popUp != null)
            {
                var scvm = popUp.CurrentContent as StudentCardViewModel;

                if (scvm != null)
                {
                    scvm.SaveFine();

                    if (scvm.PrintTempId)
                    {
                        if (scvm.TempTemplate == null)
                        {
                            MessageBox.Show("Please make sure a Temporary Template is selected.");
                            return;
                        }
                        Application.Current.Dispatcher.Invoke(() => PrintTempId(scvm, true), DispatcherPriority.DataBind);
                    }

                    if (scvm.PrintPvcId)
                    {
                        if (scvm.PvcTemplate == null)
                        {
                            MessageBox.Show("Please make sure a Student Template is selected.");
                            return;
                        }
                        Application.Current.Dispatcher.Invoke(() => PrintPvcId(scvm, true), DispatcherPriority.DataBind);
                    }

                    if (scvm.PrintReceipt && scvm.ChargeFee)
                    {
                        var rcpt = new Models.Receipt();
                        rcpt.PrintDate = DateTime.Today;
                        rcpt.ChargeAmt = scvm.FineAmt.Amount;
                        rcpt.PaidAmt = scvm.FinePaid;
                        rcpt.Details = scvm.FineAmt.Name;
                        rcpt.Grade = scvm.CurrentStudent.Grade;
                        rcpt.Homeroom = scvm.CurrentStudent.Homeroom;
                        rcpt.StudentName = scvm.CurrentStudent.DisplayName;
                        rcpt.StudentNumber = scvm.CurrentStudent.IdNumber;
                        rcpt.ReceivedBy = scvm.AcceptedBy ?? string.Empty;
                        rcpt.StudentImage = scvm.CurrentStudent.Image;

                        var view = new Views.Receipt();
                        var alertPrint = new PrintModel<Models.Receipt>(rcpt);
                        alertPrint.Title = "Fee Paid Receipt";
                        alertPrint.SchoolName = Settings.Default.School;
                        view.DataContext = alertPrint;

                        Application.Current.Dispatcher.Invoke(() => PrintReceipt(view), DispatcherPriority.DataBind);
                         
                        
                    }
                }
            }
        }

        void SavePerson(object vm)
        {
            var screen = Application.Current.Windows[0];
            var uiContext = TaskScheduler.FromCurrentSynchronizationContext();
            dynamic school = null;
            var popUp = vm as PopupViewModel;

            if (popUp != null)
            {
                var personToAdd = popUp.CurrentContent as AddPersonViewModel;

                if (personToAdd.ValidateInput())
                {
                    //var cmd = ReactiveCommand.Create(() => personToAdd.AddPerson());
                    personToAdd.AddPerson().ObserveOn(new DispatcherScheduler(Dispatcher.CurrentDispatcher, DispatcherPriority.Background)).Subscribe(OnPersonAdded);
                }

            }
        }

        private void OnPersonAdded(Tuple<object, AddPersonViewModel> result)
        {
            if (result.Item1 != null)
            {
                CurrentDialogViewModel.ShowPopup = false;
                result.Item2.SaveErrors = string.Empty;
                result.Item2.ValidationErrors = string.Empty;
                result.Item2.PersonToAdd = new PersonModel();
                result.Item2.AdditionalDetails = new Models.StudentDetails();
            }
        }

        void ResetPerson(object vm)
        {
            var screen = Application.Current.Windows[0];
            var uiContext = TaskScheduler.FromCurrentSynchronizationContext();
            dynamic school = null;
            var popUp = vm as PopupViewModel;

            if (popUp != null)
            {
                var personToAdd = popUp.CurrentContent as AddPersonViewModel;

                personToAdd.PersonType = "Student";
                personToAdd.PersonToAdd = new PersonModel();
                personToAdd.AdditionalDetails = new Models.StudentDetails();
            }
        }

        void SaveSettings(object vm)
        {
            var screen = Application.Current.Windows[0];
            var uiContext = TaskScheduler.FromCurrentSynchronizationContext();
            dynamic school = null;
            var popUp = vm as PopupViewModel;

            if (popUp != null)
            {
                var settings = popUp.CurrentContent as SettingsViewModel;

                if (settings != null)
                {
                  
                    if (settings.SelectedTabIndex == 3)
                    {
                        
                        return;
                    }

                    var schoolId = int.Parse(settings.SchoolId);

                    Settings.Default.SchoolId = schoolId;
                    Settings.Default.SqlMasterIp = settings.SqlIp;

                    //Settings.Default.School = settings.SchoolName;

                    LocalStorage.EnsureSchoolRecordExists(schoolId);
                    
                    school = LocalStorage.GetSchoolSettings(schoolId);

                    var version = Assembly.GetEntryAssembly().GetName().Version.ToString();

                    if (school == null || school.SchoolName != Settings.Default.School)
                    {
                        
                        var t3 = Task.Run(() => DataReplicator.InitRemoteServer());

                        Task.WhenAll(new[] {t3}).ContinueWith((c) =>
                        {
                            Task.Run(() => DataReplicator.SyncSchoolSettings()).ContinueWith((c1) =>
                            {
                                if (c1.Result != null)
                                {
                                    school = c1.Result;//LocalStorage.GetSchoolSettings(schoolId);
                                    Settings.Default.School = school.SchoolName;
                                 
                                    #if DEBUG
                                        version = version + ".D";
                                    #else
                                        version = version + ".R";
                                    #endif

                                    if (screen != null)
                                        screen.Title = string.Format("{0} (Build {1})", Settings.Default.School, version);
                   
                                }

                            }, uiContext);
                        }, uiContext);
                    }
                    else
                    {
                        Settings.Default.School = school.SchoolName;
                      
                        #if DEBUG
                            version = version + ".D";
                        #else
                            version = version + ".R";
                        #endif
                    }


                    if (screen != null)
                        screen.Title = string.Format("{0} (Build {1})", school.SchoolName, version);
                   
                    Settings.Default.AlertStartDate = settings.AlertStartDate;
                    Settings.Default.TardyAlertCount = settings.TardyAlertCount;
                    Settings.Default.StartupCode = settings.StartupAttendanceCode;
                    Settings.Default.StartupPeriod = settings.StartupPeriod;
                    Settings.Default.PassPrinter = settings.PassPrinter;
                    Settings.Default.PvcPrinter = settings.PvcPrinter;
                    Settings.Default.TempIdPrinter = settings.TempIdPrinter;
                    Settings.Default.ImagesFolder = settings.ImagesFolder;
                    Settings.Default.SoundsFolder = settings.SoundsFolder;
                   

                    Settings.Default.SelectedPassType = settings.SelectedPassType;

                    Settings.Default.PrintScaleFactor = settings.PrintScaleFactor;
                    Settings.Default.PrintOffsetX = settings.PrintOffsetX;
                    Settings.Default.PrintOffsetY = settings.PrintOffsetY;
                    Settings.Default.SuppressIdOnPass = settings.SuppressIdOnPass;

                    Settings.Default.AllowKioskDismissalPass = settings.AllowKioskDismissalPass;
                    Settings.Default.AllowKioskLocation = settings.AllowKioskLocation;
                    Settings.Default.AllowKioskLocationPass = settings.AllowKioskLocationPass;
                    Settings.Default.AllowKioskSearchName = settings.AllowKioskSearchName;
                    Settings.Default.AllowKioskTardyPass = settings.AllowKioskTardyPass;
                    Settings.Default.KioskLocation = settings.KioskLocation;

                    if (settings.TempIdTemplate != null)
                    {
                        Settings.Default.TempIdTemplateName = settings.TempIdTemplate.TemplateName;
                    }

                    if (settings.StudentIdTemplate != null)
                    {
                        Settings.Default.StudentIdTemplateName = settings.StudentIdTemplate.TemplateName;
                    }

                    Application.Current.Properties["PassPrintQueue"] = ((PrintQueueCollection)Application.Current.Properties["Printers"]).FirstOrDefault(x => x.Name.Contains(Settings.Default.PassPrinter));
                    Application.Current.Properties["PvcPrintQueue"] = ((PrintQueueCollection)Application.Current.Properties["Printers"]).FirstOrDefault(x => x.Name.Contains(Settings.Default.PvcPrinter));
                    Application.Current.Properties["TempIdPrintQueue"] = ((PrintQueueCollection)Application.Current.Properties["Printers"]).FirstOrDefault(x => x.Name.Contains(Settings.Default.TempIdPrinter));

                    Settings.Default.DuplexSetting = (int)settings.SelectedDuplexValue;
                    Settings.Default.Save();
                  
                }
            }
        }

        void InitDialog()
        {
            CurrentDialogViewModel = new PopupViewModel();
            CurrentDialogViewModel.ConfirmText = string.Empty;
            CurrentDialogViewModel.CancelText = string.Empty;
            CurrentDialogViewModel.ShowPopup = false;
        }

        void RecoverErrorSync()
        {
            var db = Database.OpenNamedConnection("ScanStation");

            dynamic data = db.RedisScans.FindAll(db.RedisScans.RedisId == 0 && db.RedisScans.SyncTime == null);

            foreach (dynamic record in data)
            {
                var id = record.Id;
                var scan = db.RedisScans.FindById(id);
               
                var type = scan.ObjectType;

                if (type == typeof(LocationScan).ToString())
                {
                    var json = JsonConvert.DeserializeObject<LocationScan>(scan.ObjectJson);
                  
                    json.Id = id;
                        
                    LocationScans.InsertObject(json, false);
                    


                }
                if (type == typeof(ScanModel).ToString())
                {
                    var json = JsonConvert.DeserializeObject<ScanModel>(scan.ObjectJson);

                    json.Id = id;
                        
                    Scans.InsertObject(json, false);
                    


                }

                if (type == typeof(Consequence).ToString())
                {
                    var json = JsonConvert.DeserializeObject<Consequence>(scan.ObjectJson);

                    json.Id = id;
                        
                    Detentions.InsertObject(json, false);

                }

                if (type == typeof(Dismissal).ToString())
                {
                    var json = JsonConvert.DeserializeObject<Dismissal>(scan.ObjectJson);

                  
                    json.Id = id;
                        
                    Dismissals.InsertObject(json, false);

                }


                if (type == typeof(AssessedFine).ToString())
                {
                    var fine = JsonConvert.DeserializeObject<AssessedFine>(scan.ObjectJson);

                 
                    fine.Id = id;
                        
                    Fines.InsertObject(fine, false);
                    


                }

                if (type == typeof(NewIdCard).ToString())
                {
                    var card = JsonConvert.DeserializeObject<NewIdCard>(scan.ObjectJson);

                    card.Id = id;
                        
                    IdCards.InsertObject(card, false);
                    
                }

                if (type == typeof(AlertPrinted).ToString())
                {
                    var json = JsonConvert.DeserializeObject<AlertPrinted>(scan.ObjectJson);

                  
                    json.Id = id;
                        
                    json.StudentImage = null;
                    
                    //AlertsPrinted.InsertObject(json, false);

                }

            }


            LocalStorage.ResetSyncErrors(Settings.Default.SchoolId);


        }

        void ForceSync()
        {
            var db = Database.OpenNamedConnection("ScanStation");

            dynamic data = db.RedisScans.FindAll(db.RedisScans.SyncTime == null);

            foreach (dynamic scan in data)
            {
                var id = scan.Id;
                //var scan = db.RedisScans.FindById(id);

                var type = scan.ObjectType;
                try
                {
                    if (type == typeof(LocationScan).ToString())
                    {
                        var json = JsonConvert.DeserializeObject<LocationScan>(scan.ObjectJson);
                        RemoteStorage.SendLocationScan(json);

                    }
                    if (type == typeof(ScanModel).ToString())
                    {
                        var json = JsonConvert.DeserializeObject<ScanModel>(scan.ObjectJson);

                        json.Id = id;
                        RemoteStorage.SendScan(json);
                    }

                    if (type == typeof(Models.StaffRecord).ToString())
                    {
                        var json = JsonConvert.DeserializeObject<Models.StaffRecord>(scan.ObjectJson);

                        json.Id = id;
                        RemoteStorage.SendStaffScanAsync(json);
                    }

                    if (type == typeof(Consequence).ToString())
                    {
                        var json = JsonConvert.DeserializeObject<Consequence>(scan.ObjectJson);

                        json.Id = id;
                        RemoteStorage.SendConsequence(json);
                    }

                    if (type == typeof(Dismissal).ToString())
                    {
                        var json = JsonConvert.DeserializeObject<Dismissal>(scan.ObjectJson);
                        json.Id = id;
                        RemoteStorage.SendDismissal(json);

                    }


                    if (type == typeof(AssessedFine).ToString())
                    {
                        var fine = JsonConvert.DeserializeObject<AssessedFine>(scan.ObjectJson);

                        fine.Id = id;
                        RemoteStorage.SendFine(fine);

                    }

                    if (type == typeof(VisitModel).ToString())
                    {
                        var vm = JsonConvert.DeserializeObject<VisitModel>(scan.ObjectJson);

                        vm.Id = id;
                        

                    }

                    if (type == typeof(NewIdCard).ToString())
                    {
                        var card = JsonConvert.DeserializeObject<NewIdCard>(scan.ObjectJson);

                        card.Id = id;
                        RemoteStorage.SendIdCardPrinted(card);

                    }

                    if (type == typeof(AlertPrinted).ToString())
                    {
                        var json = JsonConvert.DeserializeObject<AlertPrinted>(scan.ObjectJson);


                        json.Id = id;

                        json.StudentImage = null;
                        RemoteStorage.SendAlertPrinted(json);
                    }

                    scan.SyncTime = DateTime.Now;
                    db.RedisScans.UpdateById(scan);
                }
                catch (Exception ex)
                {
                    Logger.Error("An error occurred uploading data.", ex);
                }

            }

        }

        public ScanStationViewModel()
        {
            this.ThrownExceptions.Subscribe(ex => Logger.Error(ex));

            CheckInternet();
         
            Locations = new ReactiveList<ScanLocation>();

            PrintPasses = Settings.Default.PrintPasses;

            _periodList = new ReactiveDataSource<string>()
            {
                Label = "Period:"
            };

            _attendanceCodeList = new ReactiveDataSource<string>()
            {
                Label = "Attendance Code:"
            };

            _locationList = new ReactiveDataSource<string>();


            //ReadScanCommand = this.WhenAny(x => x.Barcode, x => !String.IsNullOrWhiteSpace(x.Value)).ToCommand();
            //this.WhenAnyObservable(x => x.ReadScanCommand).Subscribe(x => MessageBox.Show(Barcode));
            ExitCommand = this.WhenAny(x => x, x=> x != null).ToCommand();
            ExitCommand.Subscribe(_ => Application.Current.Shutdown());
        
            SelectStudent = this.WhenAny(x => x, x => x != null).ToCommand();
            SelectStudent.Subscribe(_ =>
            {
                var screen = this.HostScreen;
                MessageBox.Show(string.Format("Student tapped"));

            });

            SyncNowCommand = this.WhenAny(x => x.IsSyncing, x => !x.Value).ToCommand();
            SyncNowCommand.Subscribe(_ =>
            {
                var uiContext = TaskScheduler.FromCurrentSynchronizationContext();
                var uri = new Uri(Settings.Default.JsonUrl);
                CurrentActivity = string.Format("Connecting to {0}, please wait...", string.Format("{0}://{1}", uri.Scheme, uri.Host));
                IsSyncing = true;

                var t0 = Task.Run(() => DataReplicator.Full()).ContinueWith((c) =>
                {
                    //CurrentActivity = c.Result;

                }, uiContext);

                var t1 = Task.Run(() => DataReplicator.TardySwipes()).ContinueWith((c) =>
                {
                    //CurrentActivity = c.Result;

                }, uiContext);

                var t2 = Task.Run(() => DataReplicator.InOutRooms()).ContinueWith((c) =>
                {
                    //CurrentActivity = c.Result;

                }, uiContext);

                Task.Run(() => DataReplicator.Consequences());
                Task.Run(() => DataReplicator.Groups());

                Task.Run(() => DataReplicator.CustomStartTimes());

                Task.Run(() => DataReplicator.StationAlerts(true));

                var t3 = Task.Run(() => DataReplicator.InitRemoteServer());

                Task t4 = null, t5 = null, t6 = null, t7 = null, t8 = null, t9 = null, t10 = null;
                
                Task.WhenAll(new[] { t3 }).ContinueWith((c) =>
                {
                    Task.Run(() => DataReplicator.RemoteSnapshot());
                    t4 = Task.Run(() => DataReplicator.SyncSchoolSettings());
                    t5 = Task.Run(() => DataReplicator.SyncTimeTable());
                    t6 = Task.Run(() => DataReplicator.StudentLunchTable());
                    t7 = Task.Run(() => DataReplicator.StudentStartTimes());
                    t10 = Task.Run(() => DataReplicator.SyncAlternateIds());
                });

                t8 = Task.Run(() => DataReplicator.SyncIdCardTemplates());
                t9 = Task.Run(() => ForceSync());

                var tasks = new List<Task>(new[] {t0, t1, t2});

                if(t4 != null)
                    tasks.Add(t4);
                if (t5 != null)
                    tasks.Add(t5);
                if (t6 != null)
                    tasks.Add(t6);
                if (t7 != null)
                    tasks.Add(t7);
                if (t8 != null)
                    tasks.Add(t8);
                if (t9 != null)
                    tasks.Add(t9);
                if (t10 != null)
                    tasks.Add(t10);

                Task.WhenAll(tasks).ContinueWith((c) =>
                {
                    CurrentActivity = $"Sync Complete at {DateTime.Now.ToString("MM/dd/yyyy HH:mm tt")}";
                    IsSyncing = false;

                    LocalStorage.RecordSyncAudit();
                }, uiContext);


                //RecoverErrorSync();
                //ForceSync();

            });

            /*var studentselected = this.WhenAny(x => x.SelectedStudent, x => x.Value != null);
            studentselected.Subscribe(x =>
            {
                if (SelectedStudent != null)
                {
                    var location =
                        Locations.FirstOrDefault(
                            l =>
                                l.AttendanceCode == AttendanceCodeList.SelectedItem &&
                                l.PeriodCode == PeriodList.SelectedItem);
                    
                    SearchText = "";
                    StudentsFound.Clear(); 
                    RecordScan(SelectedStudent, location);  
                    
                  
                }
            });*/


            Observable.Interval(TimeSpan.FromSeconds(30)).ObserveOnDispatcher(DispatcherPriority.Background).Subscribe(
                async x =>
                {
                    var priorStatus = Offline;
                    Offline = await InternetAvailability.ApiIsNotAvailable(Settings.Default.JsonUrl);

                    if (priorStatus != Offline)
                        Logger.Warn("API Connected: " + Offline);
                });

            Observable.Interval(TimeSpan.FromSeconds(60)).ObserveOnDispatcher(DispatcherPriority.Background).Subscribe(x => CheckInternet());

            this.WhenAnyValue(x => x.QueuedScanRecords, x => x.QueuedLocationScans, x => x.QueuedDetentionRecords, x => x.QueuedDismissalRecords, x => x.QueuedFines, x => x.QueuedIdCards).Select(x => x.Item1 + x.Item2 + x.Item3 + x.Item4 + x.Item5 + x.Item6).ToProperty(this, x => x.QueuedRecords, out _queuedRecords);

            this.WhenAnyValue(x => x.QueuedRecords).Select(x=>string.Format("{0} record(s) in queue.", x)).ToProperty(this, x => x.QueuedRecordsText, out _queuedRecordsText);

            this.WhenAnyValue(x => x.StudentScanRecords).Select(x => string.Format("{0} student(s) scanned.", x)).ToProperty(this, x => x.StudentRecordsText, out _studentRecordsText);

            this.WhenAnyValue(x => x.SyncErrorRecords).Select(x => string.Format("{0} sync error(s).", x)).ToProperty(this, x => x.SyncErrorsText, out _syncErrorsText);

            this.WhenAnyValue(x => x.ScanModeList.SelectedItem).Select(x => x.Type).ToProperty(this, x => x.SwipeMode, out _swipeMode);

            this.WhenAnyValue(x => x.LocationList.SelectedItem).Where(x=>x!=null).ToProperty(this, x => x.SelectedLocation, out _selectedLocation);
            
            this.WhenAnyValue(x => x.SwipeMode).Where(x=>x.ToString() != Settings.Default.StartupScanMode).Subscribe(x =>
            {
                Settings.Default.StartupScanMode = x.ToString();
                Settings.Default.Save();
            });

            this.WhenAnyValue(x => x.Offline).Subscribe(x =>
            {
                if (x)
                {
                    Connected = "Station Offline";
                }
            });
            this.WhenAnyValue(x => x.SwipeMode).Where(x => x == SwipeMode.Entry).Subscribe(x =>
            {
                AttendanceCodeList.SelectedItem = null;
                PeriodList.SelectedItem = null;
            });

            this.WhenAnyValue(x => x.SwipeMode).Where(x => x == SwipeMode.ClassroomTardy).Subscribe(x =>
            {
                Dismissal = false;
            });
    

            this.WhenAnyValue(x => x.SelectedLocation).Where(x => this.SwipeMode == SwipeMode.Location && !string.IsNullOrEmpty(x) && x.ToString() != Settings.Default.StartupLocation).Subscribe(x =>
            {
                Settings.Default.StartupLocation = x.ToString();
                Settings.Default.Save();
            });

            this.WhenAnyValue(x => x.SelectedLocation).Where(x => this.SwipeMode == SwipeMode.Group && !string.IsNullOrEmpty(x)).Subscribe(x =>
            {
                Settings.Default.SelectedGroup = x;
            });

            this.WhenAnyValue(x => x.PrintPasses).Subscribe(x =>
            {
                Settings.Default.PrintPasses = x;
                
            });

            this.WhenAnyValue(x => x.Dismissal, x=>x.SwipeMode).Select(x => new Tuple<SwipeMode, bool>(x.Item2, x.Item1)).ToProperty(this, x => x.ModeSettings, out _modeSettings);


            this.WhenAnyValue(x => x.Dismissal).Subscribe(x =>
            {
                var mode = ScanModeList;
                PeriodList.Label = "Reason:";

                if ((mode.SelectedItem.Type == SwipeMode.ClassroomTardy && !Dismissal) || mode.SelectedItem.Type == SwipeMode.CafeEntrance)
                {
                    
                    PeriodList.Label = "Period:";
                    //this.RaiseAndSetIfChanged(ref _periodList, PeriodList);
                }
            });
        }
        public ScanStationViewModel(IPopupViewModelLocator dialogService = null) : this()
        {
            DialogService = dialogService;
            InitDialog();
            
            ShowSettingsCommand = this.WhenAny(x => x.DialogService, x => x != null).ToCommand();
            ShowSettingsCommand.Subscribe(x => RaiseSettingsPopup());

            MessageBus.Current.Listen<Tuple<DialogConstants>>().Subscribe(_ =>
            {
                var msg = _;

                if (msg.Item1 == DialogConstants.BatchPrint)
                {
                    RaiseBatchPrintPopup();
                }

                if (msg.Item1 == DialogConstants.AddPersonViewModel)
                {
                    RaiseAddPersonPopup();
                }
            });

        }

        public ScanStationViewModel(IScreen screen, ScanStorage scans, StaffScanStorage staffScans, IPopupViewModelLocator dialogService, RemoteStorage remoteStorage, LocalStorage localStorage, DetentionStorage detentions, InOutStorage locationScans, DismissalStorage dismissals, FineStorage fines, IdCardStorage idCards, AlertPrintedStorage alertsPrinted)
            : this(dialogService)
        {
            HostScreen = screen;
            StaffScans = staffScans;
            Scans = scans;
            RemoteStorage = remoteStorage;
            LocalStorage = localStorage;
            Detentions = detentions;
            LocationScans = locationScans;
            Dismissals = dismissals;
            Fines = fines;
            IdCards = idCards;
            AlertsPrinted = alertsPrinted;

            SchoolSettings = Application.Current.Properties["SchoolSettings"] = LocalStorage.GetSchoolSettings(Settings.Default.SchoolId);

            SchoolStartTimes = LocalStorage.GetSchoolStartTimes(Settings.Default.SchoolId);
            StudentStartTimes = LocalStorage.GetStudentStartTimes(Settings.Default.SchoolId);

            var qry =  LocalStorage.GetStudentScanCount(Settings.Default.SchoolId);
          
            StudentScanRecords = qry;

            var qry1 = LocalStorage.GetErrorScanCount(Settings.Default.SchoolId);
          
            SyncErrorRecords = qry1;
            /*this.WhenAnyValue(x => x.SwipeMode).Where(x => x == SwipeMode.ClassroomTardy).Subscribe(x =>
            {
                var roomTypes = LocationType.Tardy;

                if (Dismissal)
                    roomTypes = LocationType.Release;

                RemoteStorage.GetLocations(roomTypes).ObserveOnDispatcher().Subscribe(OnLocationsReturned);
                AttendanceCodeList.SelectedItem = AttendanceCodeList.ItemsSource.FirstOrDefault();
                PeriodList.SelectedItem = PeriodList.ItemsSource.FirstOrDefault();
            });*/


            this.WhenAnyValue(x => x.SwipeMode, x => x.Dismissal).Where(x => x.Item1 == SwipeMode.Location || x.Item1 == SwipeMode.Entry).Subscribe(x =>
            {
                var type = LocationType.InOut;
                if (x.Item2)
                {
                    type = LocationType.Release;
                }

                LocationList.Label = "Location:";
                //Remote
                LocalStorage.GetLocations(type).ObserveOnDispatcher().Subscribe(OnLocationsReturned);
                AttendanceCodeList.SelectedItem = AttendanceCodeList.ItemsSource.FirstOrDefault();
                PeriodList.SelectedItem = PeriodList.ItemsSource.FirstOrDefault();
                
            });


            this.WhenAnyValue(x => x.SwipeMode).Where(x => x == SwipeMode.CafeEntrance).Subscribe(x =>
            {
                PrintPasses = false;
                Dismissal = false;
                
                var type = LocationType.Tardy;
                PeriodList.Label = "Period:";
                //Remote
                //LocalStorage.GetLocations(type).ObserveOnDispatcher().Subscribe(OnLocationsReturned);

                LocalStorage.GetLunchPeriods().ObserveOnDispatcher().Subscribe(OnLunchPeriodsReturned);
                //AttendanceCodeList.SelectedItem = AttendanceCodeList.ItemsSource.FirstOrDefault();
                //PeriodList.SelectedItem = PeriodList.ItemsSource.FirstOrDefault();

            });

            this.WhenAnyValue(x => x.SwipeMode).Where(x => x == SwipeMode.Group).Subscribe(x =>
            {
                PrintPasses = false;
                //RemoteH
                //LocalStorage.GetLocations(type).ObserveOnDispatcher().Subscribe(OnLocationsReturned);
                LocationList.Label = "Group:";
                LocalStorage.GetGroups().ObserveOnDispatcher().Subscribe(OnGroupsReturned);
              

            });

            this.WhenAnyValue(x => x.Dismissal, x => x.SwipeMode).Where(x => x.Item2 == SwipeMode.ClassroomTardy).Subscribe(x =>
            {
                var type = LocationType.Tardy;
                PeriodList.Label = "Period:";

                if (x.Item1)
                {
                    PeriodList.Label = "Reason:";
                    type = LocationType.Release;
                }

                //Remote
                LocalStorage.GetLocations(type).ObserveOnDispatcher().Subscribe(OnLocationsReturned);
                //AttendanceCodeList.SelectedItem = AttendanceCodeList.ItemsSource.FirstOrDefault();
                //PeriodList.SelectedItem = PeriodList.ItemsSource.FirstOrDefault();
            });

            /*
            QueuedIdCards = IdCards.CountNotSynced();
            QueuedFines = Fines.CountNotSynced();
            QueuedDetentionRecords = Detentions.CountNotSynced();
            QueuedDismissalRecords = Dismissals.CountNotSynced();
            QueuedLocationScans = LocationScans.CountNotSynced();
            QueuedScanRecords = Scans.CountNotSynced();
            */

            qry = LocalStorage.GetQueueCount(SwipeDesktop.Settings.Default.SchoolId);
         
            QueuedScanRecords = qry;


            TestPrintCommand = this.WhenAny(x => x.CurrentView, x => x != null).ToCommand();
            TestPrintCommand.Subscribe(x => PrintTestDocument(Application.Current.Properties["PassPrintQueue"] as PrintQueue));



            string lastSync = LocalStorage.LastManualSync() != null ? LocalStorage.LastManualSync().Item1.ToString("MM/dd/yyyy hh:mm tt") : "N/A";
            CurrentActivity = $"Last Manual Sync: {lastSync}";
        }

        void CheckInternet()
        {
            string status;
            var connected = InternetAvailability.IsInternetAvailable(out status);

            if (connected && status != Connected)
                Logger.Warn(status);

            if(!Offline)
                Connected = status;
        }

        public void NetworkScan(PersonModel vm)
        {
            ScanLocation location = null;

            if (SwipeMode == SwipeMode.Location && !Dismissal)
            {
                var locationString = LocationList.SelectedItem;

                location = Locations.FirstOrDefault(l => l.RoomName == locationString);
            }

            RecordScan(vm, location);
        }
     
        public void RecordScan(PersonModel vm, ScanLocation location, Lane lane = Lane.Right, bool isManual = false)
        {
            var startTime = SchoolSettings.DayStartTime.TimeOfDay;

            bool splitPages = Settings.Default.SelectedPassType == "Separate Pass and Alert";

            if (vm.GetType() == typeof(PersonModel))
            {
                StaffSwipe(new StaffScan()
                {
                    AlreadySwiped = false,
                    SwipeLane = lane,
                    Barcode = vm.IdNumber,
                    StudentName = vm.DisplayName,
                    ScanImage = vm.Image,
                    ScanLocation = location,
                    EntryTime = DateTime.Now,
                    PersonId = vm.PersonId
                });

            }

            if (vm.GetType() == typeof(StudentModel))
            {
              
                var student = vm as StudentModel;

                var queue = Application.Current.Properties["PassPrintQueue"] as PrintQueue;
                
                PrintCapabilities capabilities = queue.GetPrintCapabilities(queue.DefaultPrintTicket);
                var sz = new Size(capabilities.PageImageableArea.ExtentWidth, capabilities.PageImageableArea.ExtentHeight);
             
                var scan = new Scan()
                {
                    SwipeLane = lane,
                    IsManualSwipe = isManual,
                    SwipeMode = this.SwipeMode,
                    StudentId = student.StudentId,
                    ScanLocation = location,
                    StudentGuid = student.UniqueId,
                    Barcode = student.IdNumber.Trim(),
                    StudentName = student.DisplayName,
                    ScanImage = student.Image,
                    Homeroom = student.Homeroom,
                    Grade = student.Grade,
                    EntryTime = DateTime.Now
                };

                startTime = GetSchoolStart(scan);

                if (SwipeMode != SwipeMode.Location && SwipeMode != SwipeMode.Group)
                {
                    var alerts = LocalStorage.GetAlerts(scan.StudentId);
                    scan.Alerts.AddRange(alerts);
                }

               
                Logger.Info(scan.ToString());

                ScanModel exists = null;
                var b = student.IdNumber;

                if (SwipeMode == SwipeMode.CafeEntrance)
                {
                   
                    scan.Period = PeriodList.SelectedItem;
                    var allowed = LocalStorage.CheckLunchLocation(scan.StudentId, PeriodList.SelectedItem);

                    if (!Settings.Default.EnableLunchAlerts)
                    {
                       
                        scan.Alerts.Clear();
                    }


                    if (!allowed)
                    {
                        var lunchRoom = LocalStorage.GetLunchLocation(scan.StudentId, PeriodList.SelectedItem);

                        var room = lunchRoom != null ? LocalStorage.CheckRoomLocation(scan.StudentId, lunchRoom.Item1, lunchRoom.Item2) : "N/A";
                        //scan.Per = $"Invalid Room - {room}" };
                        scan.Alerts.Insert(0,
                            new StationAlert()
                            {
                                AlertColor = 12980300,
                                Active = true,
                                AlertText = $"Invalid Lunch - {room}",
                                AlertType = "Wrong Lunch",
                                SchoolId = Settings.Default.SchoolId,
                                StudentId = scan.StudentId
                            });
                    }


                }

                if (SwipeMode == SwipeMode.Group)
                {
                    scan.Room = LocationList.SelectedItem;
                    var allowed = LocalStorage.CheckGroupLocation(scan.StudentId, LocationList.SelectedItem);

                    if (!allowed)
                    {
                        scan.Alerts =
                            new List<StationAlert>(new[]
                            {
                                new StationAlert()
                                {
                                    AlertColor = 12980300,
                                    Active = true,
                                    AlertText = "Wrong Group",
                                    AlertType = "Wrong Group",
                                    SchoolId = Settings.Default.SchoolId,
                                    StudentId = scan.StudentId
                                }
                            });
                    }
                }

                if (Dismissal)
                {
                    scan.IsStudentDismissed = true;
                    var record = new Dismissal()
                    {
                        StudentGuid = scan.StudentGuid,
                        DismissalTime = scan.EntryTime,
                        StudentName = scan.StudentName,
                        StudentNumber = scan.Barcode,
                        StatusCode = AttendanceCodeList.SelectedItem,
                        Reason = PeriodList.SelectedItem,
                    };

                    var dismissals = Dismissals.GetItemsByDate(record.DismissalTime.Date);

                    //if (!dismissals.Any(x => x.StudentGuid == record.StudentGuid))
                    var wasDismissed = LocalStorage.StudentWasDismissed(scan.Barcode);
                    if (!wasDismissed)
                    {
                        DismissStudent(record);
                    }
                    else //re-entry
                    {
                        scan.IsLeavingLocation = true;
                        var mostRecent = dismissals.OrderByDescending(x => x.DismissalTime);
                        var dismissal = mostRecent.FirstOrDefault(x => x.StudentGuid == record.StudentGuid && x.Reason == record.Reason);
                        if (dismissal != null)
                        {
                            dismissal.ReEntryTime = DateTime.Now;

                            Dismissals.InsertObject(dismissal);
                        }
                    }

                    LaneScans.Insert(0, scan);

                    PlaySound(goodScan);

                    PrintPass(scan, PrintPasses, startTime);
                    //Application.Current.Dispatcher.Invoke(() => PrintPass(scan, PrintPasses, startTime), DispatcherPriority.DataBind);

                    return;

                }

                #region check for local redis scan already exists

                if (SwipeMode == SwipeMode.ClassroomTardy)
                {
                    if (location != null)
                    {
                        //exists = Scans.GetByStudent(scan.StudentGuid, scan.EntryTime.Date, location.PeriodCode);
                    }
                    else
                    {
                        MessageBox.Show(
                            "There is a problem with your configuration. The current Period / Code combination is not valid.");

                        return;
                    }

                }

                Tuple<int, DateTime, string> sqlScan = null;
                if (SwipeMode == SwipeMode.Entry)
                {
                    exists = Scans.GetByStudent(scan.StudentGuid, scan.EntryTime.Date);
                    //sqlScan = LocalStorage.CheckEntrySwipe(scan.StudentId);
                }

                if (exists != null)
                {
                    Logger.InfoFormat("Redis Scan Found for {0} {1} {2}.", scan.StudentId, scan.SwipeMode,
                        scan.EntryTime);

                    scan.AlreadySwiped = true;

                    if (SwipeMode == Common.SwipeMode.Entry)
                    {

                        scan.EntryStatus = exists.AttendanceCode;
                        scan.EntryTime = exists.EntryTime;
                        scan.SwipeMode = Common.SwipeMode.Entry;

                        //Application.Current.Dispatcher.Invoke(() => PrintPass(scan, PrintPasses, startTime), DispatcherPriority.DataBind);
                        PrintPass(scan, PrintPasses, startTime);

                        scan.StudentName = string.Format("{0} ALREADY SCANNED", student.IdNumber);
                        Swipe(scan);
                    }
                    else
                    {
                        Swipe(new Scan()
                        {
                            AlreadySwiped = true,
                            SwipeLane = lane,
                            Barcode = b,
                            StudentName = string.Format("{0} ALREADY SCANNED", b),
                            ScanLocation = location,
                            EntryTime = DateTime.Now,
                            Alerts = scan.Alerts
                        });
                    }

                    PlaySound(badScan);
                    return;
                }

                #endregion

                Tuple<int, DateTime, string> result = null;

                if (SwipeMode == SwipeMode.ClassroomTardy && !location.AllowMultipleScans)
                {
                    result = LocalStorage.CheckTardySwipe(scan.StudentId, location.PeriodCode);
                }

                if (SwipeMode == SwipeMode.Entry)
                {
                    result = LocalStorage.CheckEntrySwipe(scan.StudentId);
                }

                if (result == null)
                {
                    Swipe(scan);

                    var passes = GeneratePass(scan, PrintPasses, startTime, sz, new RotateTransform(90));

                    if (!scan.HasAlerts)
                    {
                        PlaySound(goodScan);
                    }
                    else
                    {
                        PlaySound(badScan);
                    }

                    var pages = new List<FixedPage>();

                    if (passes != null && passes.Length > 0)
                    {
                        if (splitPages)
                        {
                            pages.AddRange(CreatePages(passes, sz));

                            /*
                            foreach (var pass in passes.Reverse())
                            {

                                pages.Add(CreatePage(pass, sz));
                                
                                Application.Current.Dispatcher.Invoke(() => PrintPass(scan, PrintPasses), DispatcherPriority.DataBind);
                  
                            }*/
                        }
                        else
                        {
                            pages.Add(CreateStackedPage(passes, sz));
                        }
                      
                    }

                    CalculateAlertsToPrint(scan, sz, pages, queue);
                }
                else
                {
                    scan.AlreadySwiped = true;

                    if (SwipeMode == SwipeMode.Entry)
                    {
                        Logger.InfoFormat("SQL record Found: {0} {1} {2}.", result.Item1, result.Item2, result.Item3);

                        scan.EntryStatus = result.Item3;
                        scan.EntryTime = result.Item2;
                        scan.SwipeMode = Common.SwipeMode.Entry;

                        //Application.Current.Dispatcher.Invoke(() => PrintPass(scan, PrintPasses), DispatcherPriority.Loaded);
                       
                        scan.StudentName = string.Format("{0} ALREADY SCANNED", student.IdNumber);
                        
                        Swipe(scan);

                        CalculateAlertsToPrint(scan, sz, new List<FixedPage>(), queue);

                    }
                    else
                    {
                        Swipe(new Scan()
                        {
                            AlreadySwiped = true,
                            SwipeLane = lane,
                            Barcode = student.IdNumber,
                            StudentName = string.Format("{0} ALREADY SCANNED", student.IdNumber),
                            ScanLocation = location,
                            EntryTime = result.Item2,
                            Alerts = scan.Alerts
                        });

                        CalculateAlertsToPrint(scan, sz, new List<FixedPage>(), queue);
                    }

                    PlaySound(badScan);

                }
            }
            //SelectedStudent = null;
        }

        void CalculateAlertsToPrint(Scan scan, Size sz, List<FixedPage> pages, PrintQueue queue)
        {
          
            /*
            if (SwipeMode != SwipeMode.Location && SwipeMode != SwipeMode.Group)
            {
                var recs = LocalStorage.GetAlerts(scan.StudentId);
                scan.Alerts.AddRange(recs);
            }
            */

                var alerts = new List<Control>();
            //SwipeMode != SwipeMode.CafeEntrance ||

            if (SwipeMode != SwipeMode.Location || SwipeMode != SwipeMode.Group)
            {
                //Application.Current.Dispatcher.Invoke(() => PrintAlert(scan, queue), DispatcherPriority.DataBind);
                alerts.AddRange(GenerateAlert(scan, sz, new Size(480, 270)));

                foreach (var alert in alerts)
                {
                   
                    pages.Add(CreatePage(alert, sz));
                    
                }
            }

            if (pages.Any())
            {
                Application.Current.Dispatcher.Invoke(() => PrintDocument(queue, pages.ToArray(), "Swipe Print"), DispatcherPriority.DataBind);
            }

            if (scan.HasAlerts)
            {
                var sound = Settings.Default.SoundsFolder + "\\" + scan.Alerts[0].AlertSound;

                PlaySound(sound);
            }
        }
        private void OnManualEntry(StudentModel data)
        {
            if (SwipeMode == SwipeMode.Entry && !MarkAllPresent)
                RaiseFinePopup(data, null);
        }

        private void PlaySound(string uri)
        {
           
            try
            {
                /*
                using (var player = new SoundPlayer(uri))
                {
                    player.Play();
                }
                */
                var player = new SoundPlayer(uri);
                player.LoadCompleted += delegate {
                    player.Play();
                };
                player.LoadAsync();
                
            
            }
            catch (Exception ex) { Logger.Error(ex);}
        }

        public void PrintPass(Scan model, bool printEnabled, TimeSpan schoolStart)
        {

            var queue = Application.Current.Properties["PassPrintQueue"] as PrintQueue;

            var checkAllPresent = SwipeMode == SwipeMode.Entry && !Dismissal;

            if ((checkAllPresent && schoolStart > DateTime.Now.TimeOfDay) || (checkAllPresent && MarkAllPresent && !printEnabled))
                return;

            var pass = new TardyPass(SwipeMode == SwipeMode.ClassroomTardy);

            if (Settings.Default.SuppressIdOnPass)
                pass.BarcodeLabel.Visibility = Visibility.Hidden;

            if (!Dismissal)
            {

                if (SwipeMode == SwipeMode.ClassroomTardy)
                {
                    model.Room = LocalStorage.CheckRoomLocation(model.StudentId, model.Period);
                    
                    model.TardyStats =
                        new ReactiveList<TardyStat>(LocalStorage.CheckTardyStats(model.StudentId, model.Period,
                            Settings.Default.AlertStartDate));

                    string code = model.AttendanceCode;

                    var consequence = LocalStorage.GetConsequences(code, Settings.Default.SchoolId);

                    if (consequence.Any())
                    {
                        consequence.ForEach(x =>
                        {
                            bool triggerconsequence = false;

                            var codes = x.IncidentCode; //list of codes from the consequence

                            string[] codesList = codes.Replace(" ", "").Split(',');

                            /**
                            * check that code is in list of consequence codes
                            * we need to check this because we do partial macthing on incident codes
                            * as the incident codes top match could be multiple values
                            */

                            if (codesList.Contains(code))
                            {
                                var period = x.Period == "Y" ? model.Period : null;

                                var count = LocalStorage.IncidentCount(model.StudentId, codes.Replace(" ",""), x.StartDate, period);

                                //if (Settings.Default.TardyAlertCount > 0 && ((model.TardyStats[2].YearToDate % Settings.Default.TardyAlertCount) == 0))
                                if (x.Repeats) //repeats?
                                {
                                    if (count != null && count.Item1 > 0 && (count.Item1 % x.IncidentCount) == 0)
                                    {
                                        triggerconsequence = true;
                                    }
                                }
                                else
                                {
                                  
                                    int consequenceCount = x.IncidentCount;

                                    if (count != null)
                                    {
                                        int sum = count.Item1;
                                        string opCode = x.Operator.ToString().Trim();

                                        if (sum > 0 && HelperExtensions.CompareOperator(opCode, sum, consequenceCount))
                                            triggerconsequence = true;
                                    }
                                }

                                if (triggerconsequence)
                                {
                                    issueConsequence(model, queue, x.Message, code, x.OutcomeType, x.ServeByDate);
                                }
                            
                            }

                        });
                    }
                }

                if (SwipeMode == SwipeMode.Location)
                {
                    var swipeOutList = LocationScans.GetItemsByStudent(model.Room, model.EntryTime, model.Barcode);
                    //only print on 'out' swipe which is the even swipe
                    if (!InOutPasses)
                    {
                        if (swipeOutList.Count()%2 != 0)
                        //if(!model.IsLeavingLocation)
                        {
                            return;
                        }
                    }

                }

                if (SwipeMode == SwipeMode.Group)
                {
                    if (model.IsWrongGroup)
                        return;
                }
            }

            if (!printEnabled)
                return;

            var printModel = new PrintModel<Scan>(model);

            printModel.SchoolName = Settings.Default.School;

            pass.DataContext = printModel;

            if (Settings.Default.SuppressIdOnPass)
                pass.BarcodeLabel.Visibility = Visibility.Hidden;

            try
            {
             
                if (queue != null)
                {

                    printVisual(queue, pass, "Swipe Desktop Pass", new RotateTransform(90));

                }
            }
            catch (Exception ex)
            {
                //Logger.Error("Could not print.", ex);
                MessageBox.Show("Could not print: " + ex.Message);
            }
            finally
            {
                //if (session != null)
                //    session.Dispose();

                //Label = null;
            }
        }

        public Control[] GeneratePass(Scan model, bool printEnabled, TimeSpan schoolStart, Size paperSize, RotateTransform rotate)
        {
            var list = new List<Control>();

            if ((SwipeMode == SwipeMode.Entry && schoolStart > DateTime.Now.TimeOfDay) || (SwipeMode == SwipeMode.Entry && MarkAllPresent && !printEnabled))
                return null;

            var pass = new TardyPass(SwipeMode == SwipeMode.ClassroomTardy);

            if (Settings.Default.SuppressIdOnPass)
                pass.BarcodeLabel.Visibility = Visibility.Hidden;

            if (!Dismissal)
            {

                if (SwipeMode == SwipeMode.ClassroomTardy)
                {
                    model.Room = LocalStorage.CheckRoomLocation(model.StudentId, model.Period);

                    model.TardyStats =
                        new ReactiveList<TardyStat>(LocalStorage.CheckTardyStats(model.StudentId, model.Period,
                            Settings.Default.AlertStartDate));

                    string code = model.AttendanceCode;

                    var consequence = LocalStorage.GetConsequences(code, Settings.Default.SchoolId);

                    if (consequence.Any())
                    {
                        consequence.ForEach(x =>
                        {
                            bool triggerconsequence = false;

                           
                            var codes = x.IncidentCode; //list of codes from the consequence

                            string[] codesList = codes.Replace(" ", "").Split(',');

                            /**
                            * check that code is in list of consequence codes
                            * we need to check this because we do partial macthing on incident codes
                            * as the incident codes top match could be multiple values
                            */

                            if (codesList.Contains(code))
                            {

                                var period = x.Period == "Y" ? model.Period : null;

                                var count = LocalStorage.IncidentCount(model.StudentId, codes.Replace(" ", ""),
                                    x.StartDate, period);

                                //if (Settings.Default.TardyAlertCount > 0 && ((model.TardyStats[2].YearToDate % Settings.Default.TardyAlertCount) == 0))
                                if (x.Repeats) //repeats?
                                {
                                    if (count != null && count.Item1 > 0 && (count.Item1 % x.IncidentCount) == 0)
                                    {
                                        triggerconsequence = true;
                                    }
                                }
                                else
                                {

                                    int consequenceCount = x.IncidentCount;

                                    if (count != null)
                                    {
                                        int sum = count.Item1;
                                        string opCode = x.Operator.ToString().Trim();

                                        if (sum > 0 && HelperExtensions.CompareOperator(opCode, sum, consequenceCount))
                                            triggerconsequence = true;
                                    }
                                }

                                if (triggerconsequence)
                                {
                                    var cAlert = new StationAlert()
                                    {
                                        AlertId = -99,
                                        AlertColor = 1,
                                        Active = true,
                                        AlertText = "Consequence Issued",
                                        AlertType = "Consequence Issued",
                                        SchoolId = Settings.Default.SchoolId,
                                        StudentId = model.StudentId,
                                        AlertSound = "general.wav"
                                    };

                                    Control cons = null;
                                    if (Settings.Default.SelectedPassType == "Separate Pass and Alert")
                                    {
                                        cons = GenerateConsequence(model, x.Message, code, x.OutcomeType,
                                            x.ServeByDate);
                                    }
                                    else
                                    {
                                        cons = GenerateShortConsequence(model, x.Message, code, x.OutcomeType,
                                            x.ServeByDate);
                                    }

                                    if (PrintPasses)
                                    {
                                        if (cons != null)
                                            list.Add(cons);
                                    }

                                    if (!model.Alerts.Any())
                                    {
                                        model.Alerts.Add(cAlert);
                                    }
                                }
                            }
                        });
                    }
                }

                if (SwipeMode == SwipeMode.Location)
                {
                    var swipeOutList = LocationScans.GetItemsByStudent(model.Room, model.EntryTime, model.Barcode);
                    //only print on 'out' swipe which is the even swipe
                    if (!InOutPasses)
                    {
                        if (swipeOutList.Count() % 2 != 0)
                        //if(!model.IsLeavingLocation)
                        {
                            return null;
                        }
                    }

                }

                if (SwipeMode == SwipeMode.Group)
                {
                    if (model.IsWrongGroup)
                        return null;
                }
            }

            if (!printEnabled)
                return null;

            var printModel = new PrintModel<Scan>(model);

            printModel.SchoolName = Settings.Default.School;

            pass.DataContext = printModel;

            if (Settings.Default.PrintScaleFactor > 1)
            {
                //second parameter is north/south alignment X: -30, Y:-130
                pass.Margin = new Thickness(Settings.Default.PrintOffsetX, Settings.Default.PrintOffsetY, 0, 0);

            }
            else
            {
                pass.Margin = new Thickness(10, 10, 0, 0);
            }

            var scale = Scale();

            var transform = new TransformGroup();
            if (scale != null)
                transform.Children.Add(scale);
            transform.Children.Add(rotate);

            pass.LayoutTransform = transform;
            pass.UpdateLayout();

            //Point ptGrid = new Point(pass.ActualWidth, pass.ActualHeight);
            //pass.Arrange(new Rect(ptGrid, paperSize));

            //pass.Measure(paperSize);
          
            list.Insert(0, pass);

            return list.ToArray();
        }

        private Tuple<Canvas,Canvas, string , string> LoadIdCard(int cardID, bool isTempId)
        {
            string frontOrientation = string.Empty;
            string backOrientation = string.Empty;
            var frontCanvas = new Canvas();
            frontCanvas.Margin = new Thickness(1);
            var backCanvas = new Canvas();
            backCanvas.Margin = new Thickness(1);

            IdCardUtils.SwipeCard.IDCardsDataTable idTable = IdCardBll.GetCardById(cardID);
            
            // Update Front Canvas layout and card information area
            if (idTable[0].FrontPortrait)
            {
                frontOrientation = App.APP_CARD_ORIENTATION_PORTRAIT;

                frontCanvas.Width = App.APP_CARD_SHORT_SIDE;
                //FrontCanvasBorder.Width = App.APP_CARD_SHORT_SIDE + 2;

                frontCanvas.Height = App.APP_CARD_LONG_SIDE;
                //FrontCanvasBorder.Height = App.APP_CARD_LONG_SIDE + 2;
            }
            else
            {
                frontOrientation = App.APP_CARD_ORIENTATION_LANDSCAPE;

                frontCanvas.Width = App.APP_CARD_LONG_SIDE;
                //FrontCanvasBorder.Width = App.APP_CARD_LONG_SIDE + 2;

                frontCanvas.Height = App.APP_CARD_SHORT_SIDE;
                //FrontCanvasBorder.Height = App.APP_CARD_SHORT_SIDE + 2;
            }

            // Update Back Canvas layout and card information area
            if (idTable[0].BackPortrait)
            {
                backOrientation = App.APP_CARD_ORIENTATION_PORTRAIT;

                backCanvas.Width = App.APP_CARD_SHORT_SIDE;
                //BackCanvasBorder.Width = App.APP_CARD_SHORT_SIDE + 2;

                backCanvas.Height = App.APP_CARD_LONG_SIDE;
                //BackCanvasBorder.Height = App.APP_CARD_LONG_SIDE + 2;
            }
            else
            {
                backOrientation = App.APP_CARD_ORIENTATION_LANDSCAPE;
                
                backCanvas.Width = App.APP_CARD_LONG_SIDE;
                //BackCanvasBorder.Width = App.APP_CARD_LONG_SIDE + 2;

                backCanvas.Height = App.APP_CARD_SHORT_SIDE;
                //BackCanvasBorder.Height = App.APP_CARD_SHORT_SIDE + 2;
            }

            bool tempCard = idTable[0].TempCard;

            if (!isTempId)
            {
                // Update Background Image and Opacity 
                if (idTable[0].FrontBackground != null)
                {
                    updateCanvasBackground(frontCanvas, idTable[0].FrontBackground, idTable[0].FrontOpacity);
                }
                if (idTable[0].BackBackground != null)
                {
                    updateCanvasBackground(backCanvas, idTable[0].BackBackground, idTable[0].BackOpacity);
                }
            }

            // Update card fields on canvases
            if (idTable[0].Fields != null)
            {
                var cardItems = Serialisation.DeserializeObject<List<CardItem>>(idTable[0].Fields);
                loadCardItems(cardItems, frontCanvas, backCanvas);
            }

            return new Tuple<Canvas, Canvas, string, string>(frontCanvas, backCanvas, frontOrientation, backOrientation);
        }

        private void updateCanvasBackground(Canvas c, string uri, double opacity)
        {
            ImageBrush ib = new ImageBrush();
            ib.ImageSource = SwipeUtils.getImageFile(uri);
            c.Background = ib;
            c.Background.Opacity = opacity;
            c.UpdateLayout();
        }

        private Dictionary<string, string> LoadFieldsList(bool tempCard, int personID, bool isStaff = false)
        {
            DataTable table = null;
           
            if (tempCard)
            {
                table = IdCardBll.GetTempStudentCard(personID);
            }
            else
            {
                if (isStaff)
                    table = IdCardBll.GetTeacherCard(personID);
                else
                {
                    table = IdCardBll.GetStudentCard(personID);
                }
            }
           
            switch (table.Rows.Count)
            {
                case 0:
                    Logger.Error("Error loading ID card field list. No rows returned.");
                    break;
                case 1:
                    string textEntry = "<Text Entry>";
                    addFields.Add(textEntry, textEntry);

                    string logoPicture = "<Image>";
                    addFields.Add(logoPicture, logoPicture);

                    DataRow row = table.Rows[0];

                    foreach (DataColumn col in row.Table.Columns)
                    {
                        addFields.Add(col.ToString(), row[col].ToString());
                    }

                    break;
                default:
                    Logger.Error("Error loading ID card field list. " + table.Rows.Count + " rows returned.");
                    break;
            }

            return addFields;
        }

        private Size MeasureString(string candidate, Control tb)
        {
            var formattedText = new FormattedText(
                candidate,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface(tb.FontFamily, tb.FontStyle, tb.FontWeight, tb.FontStretch), tb.FontSize,
                Brushes.Black);

            return new Size(formattedText.Width, formattedText.Height);
        }

        private Size MeasureString(string candidate, TextBlock tb)
        {
            var formattedText = new FormattedText(
                candidate,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface(tb.FontFamily, tb.FontStyle, tb.FontWeight, tb.FontStretch), tb.FontSize,
                Brushes.Black);

            return new Size(formattedText.Width, formattedText.Height);
        }

 
        private void addSavedItemToCanvas(Canvas canvas, CardItem cardItem)
        {
            Size measure = new Size();

            try
            {
                if (cardItem.FieldType.Contains("Text_Entry"))
                {
                
                    TextBox te = new TextBox();
                    //te.Name = cardItem.FieldType;
                    te.Name = cardItem.FieldType + "_" + DateTime.Now.ToString("HHmmFFF");
                    te.Text = cardItem.Text;

                    if (cardItem.Width != 0)
                    {
                        te.Width = cardItem.Width;
                    }

                    if (cardItem.Height != 0)
                    {
                        te.Height = cardItem.Height;
                    }

                    te.BorderThickness = new Thickness(0);
                    te.Foreground =
                        new SolidColorBrush(SwipeUtils.ConvertHexStringToColour(cardItem.Foreground));

                    te.Background = new SolidColorBrush(Colors.Transparent);
                    //te.Background =
                    //    new SolidColorBrush(SwipeUtils.ConvertHexStringToColour(cardItem.Background));

                    te.TextAlignment = (cardItem.Alignment.Equals("Center")
                        ? TextAlignment.Center
                        : (cardItem.Alignment.Equals("Right") ? TextAlignment.Right : TextAlignment.Left));
                    te.FontFamily = new FontFamily(cardItem.TextFont);
                    te.FontSize = cardItem.TextSize;
                    te.FontWeight = (cardItem.TextBold ? FontWeights.Bold : FontWeights.Regular);
                    te.FontStyle = (cardItem.TextItalic ? FontStyles.Italic : FontStyles.Normal);

                    if (cardItem.TextUnderline)
                    {
                        te.TextDecorations.Add(TextDecorations.Underline);
                    }

                    measure = MeasureString(cardItem.Text, te);

                    te.Width = cardItem.Width < measure.Width ? measure.Width : cardItem.Width;
                    te.Height = cardItem.Height < measure.Height ? measure.Height : cardItem.Height;

                    te.TextWrapping = TextWrapping.Wrap;
                    te.TextChanged +=
                        new System.Windows.Controls.TextChangedEventHandler(textBox_TextChanged);

                    Canvas.SetTop(te, cardItem.Top);
                    Canvas.SetLeft(te, cardItem.Left);
                    Canvas.SetZIndex(te, cardItem.ZIndex);

                    canvas.Children.Add(te);
                    canvas.UpdateLayout();

                }
                else
                {
                    var barcode = addFields.ContainsKey("IdNumber") ? addFields["IdNumber"] : null;
                    if (string.IsNullOrEmpty(barcode))
                    {
                        barcode = addFields.ContainsKey("Student Number")
                            ? addFields["Student Number"]
                            : null;
                    }

                    switch (cardItem.FieldType)
                    {
                        case "BarcodeImage":

                            var height = 90; // cardItem.Height;
                            var width = 90; // cardItem.Width;

                            if (barcode == null)
                                barcode = "99999999";

                            Image img2 = new Image();
                            img2.Name = cardItem.FieldType;
                            var bi = CanvasHelper.Draw2dBarcode(barcode, height, width);
                            img2.Source = bi;
                            img2.DataContext = barcode;

                            Canvas.SetTop(img2, cardItem.Top);
                            Canvas.SetLeft(img2, cardItem.Left);
                            Canvas.SetZIndex(img2, cardItem.ZIndex);

                            canvas.Children.Add(img2);
                            canvas.UpdateLayout();

                            break;
                        case "Print_Date":
                            Label prntlbl = new Label();
                            prntlbl.Name = cardItem.FieldType;
                            //prntlbl.Content = addFieldsList["<Print Date>"];
                            //blbl.FontFamily = new FontFamily(new Uri("pack://application:,,,/"), App.APP_FONT_BARCODE);
                            prntlbl.FontSize = cardItem.TextSize;
                            prntlbl.Content = "Issued: " + DateTime.Now.ToString("F");
                            prntlbl.Height = 30;

                            prntlbl.HorizontalAlignment = HorizontalAlignment.Center;
                            prntlbl.VerticalAlignment = VerticalAlignment.Top;


                            measure = MeasureString(prntlbl.Content.ToString(), prntlbl);

                            prntlbl.Width = cardItem.Width < measure.Width ? measure.Width : cardItem.Width;
                            prntlbl.Height = cardItem.Height < measure.Height ? measure.Height : cardItem.Height;



                            Canvas.SetTop(prntlbl, cardItem.Top);
                            Canvas.SetLeft(prntlbl, cardItem.Left);
                            Canvas.SetZIndex(prntlbl, cardItem.ZIndex);

                            canvas.Children.Add(prntlbl);
                            canvas.UpdateLayout();

                            break;
                        case "Bar_Code":
                            Label blbl = new Label();
                            blbl.Name = cardItem.FieldType;


                            if (barcode != null)
                                blbl.Content = "*" + barcode + "*";
                            else
                            {
                                blbl.Content = "*99999*";
                            }

                            blbl.FontFamily = new FontFamily(new Uri("pack://application:,,,/"),
                                App.APP_FONT_BARCODE);
                            blbl.FontSize = cardItem.TextSize;

                            if (cardItem.Width != 0)
                            {
                                blbl.Width = cardItem.Width;
                            }

                            if (cardItem.Height != 0)
                            {
                                blbl.Height = cardItem.Height;
                            }

                            Canvas.SetTop(blbl, cardItem.Top);
                            Canvas.SetLeft(blbl, cardItem.Left);
                            Canvas.SetZIndex(blbl, cardItem.ZIndex);

                            canvas.Children.Add(blbl);
                            canvas.UpdateLayout();

                            break;

                        case "Photo_Image":
                        case "Image":
                            string imgSource;

                            if (cardItem.FieldType.Equals("Image"))
                            {
                                imgSource = Uri.UnescapeDataString(cardItem.Source.Replace("file:///", ""));
                            }
                            else
                            {
                                imgSource = System.IO.Path.Combine(SwipeUtils.getPhotoImageFolder(),
                                    addFields["Photo Image"]);
                            }

                            Image img = new Image();
                            img.Name = cardItem.FieldType;

                            BitmapImage bitmapImg = SwipeUtils.getImageFile(imgSource);

                            if (bitmapImg != null)
                            {
                                img.Source = bitmapImg;
                                img.Width = cardItem.Width;
                                img.Height = cardItem.Height;

                                Canvas.SetTop(img, cardItem.Top);
                                Canvas.SetLeft(img, cardItem.Left);
                                Canvas.SetZIndex(img, cardItem.ZIndex);

                                canvas.Children.Add(img);
                                canvas.UpdateLayout();
                            }

                            break;


                        default:
                            TextBlock tb = new TextBlock();
                            tb.Name = cardItem.FieldType;

                            tb.Foreground =
                                new SolidColorBrush(SwipeUtils.ConvertHexStringToColour(cardItem.Foreground));

                            tb.Background = new SolidColorBrush(Colors.Transparent);

                            //tb.Background =
                            //    new SolidColorBrush(SwipeUtils.ConvertHexStringToColour(cardItem.Background));
                            tb.TextAlignment = (cardItem.Alignment.Equals("Center")
                                ? TextAlignment.Center
                                : (cardItem.Alignment.Equals("Right") ? TextAlignment.Right : TextAlignment.Left));
                            tb.FontFamily = new FontFamily(cardItem.TextFont);
                            tb.FontSize = cardItem.TextSize;
                            tb.FontWeight = (cardItem.TextBold ? FontWeights.Bold : FontWeights.Regular);
                            tb.FontStyle = (cardItem.TextItalic ? FontStyles.Italic : FontStyles.Normal);

                            if (cardItem.TextUnderline)
                            {
                                tb.TextDecorations.Add(TextDecorations.Underline);
                            }

                            if (cardItem.FieldType.Equals("DOB"))
                            {
                                DateTime parsedDate;
                                DateTime.TryParse(addFields["DOB"], out parsedDate);
                                tb.Text = parsedDate.ToString("MM-dd-yyyy");
                            }
                            else
                            {
                                string key = cardItem.FieldType.Replace("_", " ");
                                if (!addFields.ContainsKey(key))
                                {
                                    return;
                                }
                                if (addFields.Count > 0)
                                    tb.Text = addFields[key];
                                else
                                {
                                    tb.Text = string.Empty;
                                }
                            }


                            measure = MeasureString(tb.Text, tb);

                            tb.Width = cardItem.Width < measure.Width ? measure.Width : cardItem.Width;
                            tb.Height = cardItem.Height < measure.Height ? measure.Height : cardItem.Height;


                            Canvas.SetTop(tb, cardItem.Top);
                            Canvas.SetLeft(tb, cardItem.Left);
                            Canvas.SetZIndex(tb, cardItem.ZIndex);

                            canvas.Children.Add(tb);
                            canvas.UpdateLayout();

                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("Could not add item to id card. {0}"), ex);
            }
        }

        private void loadCardItems(List<CardItem> cardItems, Canvas front, Canvas back)
        {
            foreach (CardItem ci in cardItems)
            {
                try
                {
                    if (ci.Side.Equals(App.APP_CARD_FRONT))
                    {
                        addSavedItemToCanvas(front, ci);
                    }
                    else if (ci.Side.Equals(App.APP_CARD_BACK))
                    {
                        addSavedItemToCanvas(back, ci);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ci.Name, ex);
                }
            }
        }

        protected void textBox_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = (TextBox)sender;

            // Get measured text size with 10 pixel padding added
            double textSize = SwipeUtils.MeasureTextSize(tb.Text, tb.FontFamily, tb.FontStyle, tb.FontWeight, tb.FontStretch, tb.FontSize).Width + 10;
            double textboxWidth = tb.Width;

            if (textSize >= textboxWidth)
            {
                // New textbox size can not exceed canvas size
                Canvas c = (Canvas)tb.Parent;
                double newWidth = Math.Ceiling(textSize);
                double left = Canvas.GetLeft(tb);

                // If the width of the textbox (based on position within canvas) exceeds 
                // canvas then no longer increase textbox size so it will start to wrap
                if ((left + newWidth) <= c.Width)
                {
                    tb.Width = newWidth;
                }
            }
            else
            {
                // See if textbox needs to be reduced in size
                double newWidth = Math.Ceiling(textSize);

                if (tb.IsReadOnly)
                {
                    // These are dB fields, therefore no min size
                    tb.Width = newWidth;
                }
                else
                {
                    // These are user generated dynamic fields, therefore apply min default size
                    if (newWidth < App.APP_DEFAULT_TEXTBOX_WIDTH)
                    {
                        tb.Width = App.APP_DEFAULT_TEXTBOX_WIDTH;
                    }
                    else
                    {
                        tb.Width = newWidth;
                    }
                }
            }
        }

        public void PrintReceipt(Views.Receipt rcpt)
        {
            var queue = Application.Current.Properties["PassPrintQueue"] as PrintQueue;

            try
            {

                if (queue != null)
                {
                    printVisual(queue, rcpt, "Print Receipt", new RotateTransform(0));
                }
            }
            catch (Exception ex)
            {
                //Logger.Error("Could not print.", ex);
                MessageBox.Show("Could not print: " + ex.Message);
            }
            finally
            {

            }
        }
        public void PrintTempId(StudentCardViewModel model, bool printEnabled)
        {
            var queue = Application.Current.Properties["TempIdPrintQueue"] as PrintQueue;

            try
            {

                if (queue != null)
                {
                    addFields.Clear();
                    addFields = LoadFieldsList(true, model.CurrentStudent.PersonId);

                    var idCard = LoadIdCard(model.TempTemplate.Id, true);

                    var dialog = new PrintDialog();
                    dialog.PrintQueue = queue;
                    dialog.PrintTicket = new PrintTicket();
                    dialog.PrintTicket.PageOrientation = PageOrientation.Portrait;
                    dialog.PrintTicket.PageBorderless = PageBorderless.Borderless;
                     dialog.PrintTicket.OutputQuality = OutputQuality.Photographic;
                    dialog.PrintTicket.PageMediaSize = new PageMediaSize(215, 370);

                    dialog.PrintTicket.PageOrientation = PageOrientation.Portrait;
                    if (idCard.Item3.Equals(App.APP_CARD_ORIENTATION_LANDSCAPE))
                    {
                        //dialog.PrintTicket.PageOrientation = PageOrientation.Landscape;
                        
                        RotateTransform rotateTransform1 = new RotateTransform(90);

                        idCard.Item1.LayoutTransform = rotateTransform1;
                        idCard.Item1.UpdateLayout();

                    }

                    //dialog.PrintVisual(idCard.Item1, "Print ID Card");

                    /*if (queue.Name.ToUpper().Contains("DYMO") || queue.Name.ToUpper().Contains("STAR"))
                    {
                        var rotate = new RotateTransform(90);
                        idCard.Item1.LayoutTransform = rotate;
                        idCard.Item1.UpdateLayout();
                    }*/

                    var document = new FixedDocument();
                    document.DocumentPaginator.PageSize = new Size(dialog.PrintableAreaWidth, dialog.PrintableAreaHeight);

                    document.PrintTicket = dialog.PrintTicket;

                    var content = new PageContent();
                    var page = new FixedPage();
                    page.Width = document.DocumentPaginator.PageSize.Width;
                    page.Height = document.DocumentPaginator.PageSize.Height;
                    page.Children.Add(idCard.Item1);
                    document.Pages.Add(content);
                    ((IAddChild)content).AddChild(page);
                    dialog.PrintDocument(document.DocumentPaginator, "Temporary ID");
                    addFields.Clear();
                  
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Could not print temp id.", ex);
                MessageBox.Show("Could not print Temp ID: " + ex.StackTrace);
            }
            finally
            {
               
            }
        }

        public void PrintBatchCards(Tuple<int, IEnumerable<CheckedItem>> model)
        {
            
            PrintBatchPvcId(model.Item2, model.Item1);
    
        }

        public void ViewBatch(Tuple<int, IEnumerable<StudentModel>> source)
        {
            this.RaiseSelectedStudents(source.Item1, source.Item2);
        }

        public void PrintBatchPvcId(IEnumerable<CheckedItem> model, int templateId)
        {
            var queue = Application.Current.Properties["PvcPrintQueue"] as PrintQueue;

            try
            {

                if (queue != null)
                {
                   
                    PrintDialog dialog = new PrintDialog();
                    dialog.PageRangeSelection = PageRangeSelection.AllPages;
                    dialog.UserPageRangeEnabled = true;

                    PrintCapabilities capabilities = queue.GetPrintCapabilities(queue.DefaultPrintTicket);

                    var print = dialog.ShowDialog();
                    if (print == true)
                    {
                        var printTicket = new PrintTicket();
                        
                        printTicket.PageBorderless = PageBorderless.Borderless;
                        printTicket.PhotoPrintingIntent = PhotoPrintingIntent.PhotoBest;
                        //printTicket.PageMediaSize = new PageMediaSize(215, 370);
                        foreach (var item in model)
                        {
                            addFields.Clear();
                            addFields = LoadFieldsList(false, item.ItemId);

                            var idCard = LoadIdCard(templateId, false);

                            printTicket.PageOrientation = idCard.Item3.Equals(App.APP_CARD_ORIENTATION_LANDSCAPE) ? PageOrientation.Landscape : PageOrientation.Portrait;
                            //dialog.PrintTicket.PageOrientation = idCard.Item3.Equals(App.APP_CARD_ORIENTATION_LANDSCAPE) ? PageOrientation.Landscape : PageOrientation.Portrait;
                            //dialog.PrintTicket.PageOrientation = PageOrientation.Portrait;

                            if (idCard.Item3.Equals(App.APP_CARD_ORIENTATION_LANDSCAPE))
                            {
                                /*RotateTransform rotateTransform1 = new RotateTransform(90);

                                idCard.Item1.LayoutTransform = rotateTransform1;
                                idCard.Item1.UpdateLayout();

                                idCard.Item2.LayoutTransform = rotateTransform1;
                                idCard.Item2.UpdateLayout();
                                */
                            }

                            dialog.PrintTicket = printTicket;

                            printDuplex(dialog, idCard.Item1, idCard.Item2, item.ItemNumber + "ID");

                            addFields.Clear();
                        }
                    }

                }
            }
            catch (ConstraintException cex)
            {
                Logger.Error(cex);
            }
            catch (Exception ex)
            {
                //Logger.Error("Could not print.", ex);
                MessageBox.Show("Could not print PVC ID: " + ex.Message);
            }
            finally
            {

            }
        }

        public void PrintPvcId(StudentCardViewModel model, bool printEnabled)
        {
            var queue = Application.Current.Properties["PvcPrintQueue"] as PrintQueue;

            try
            {
                if (queue != null)
                {
                    addFields.Clear();
                    addFields = LoadFieldsList(false, model.CurrentStudent.PersonId);

                    var idCard = LoadIdCard(model.PvcTemplate.Id, false);

                    var dialog = new PrintDialog();
                    dialog.PrintQueue = queue;
                    PrintCapabilities capabilities = queue.GetPrintCapabilities(queue.DefaultPrintTicket);
                    //var sz = new Size(capabilities.PageImageableArea.ExtentWidth, capabilities.PageImageableArea.ExtentHeight);

                    dialog.PrintTicket = new PrintTicket();
                    dialog.PrintTicket.PageBorderless = PageBorderless.Borderless;
                     dialog.PrintTicket.OutputQuality = OutputQuality.Photographic;
                    //dialog.PrintTicket.PageMediaSize = new PageMediaSize(215, 370);
                    dialog.PrintTicket.PageOrientation = PageOrientation.Portrait;

                    if (idCard.Item3.Equals(App.APP_CARD_ORIENTATION_LANDSCAPE))
                    {
                        dialog.PrintTicket.PageOrientation = PageOrientation.Landscape;
                        /*RotateTransform rotateTransform1 = new RotateTransform(90);

                        idCard.Item1.LayoutTransform = rotateTransform1;
                        idCard.Item1.UpdateLayout();

                        idCard.Item2.LayoutTransform = rotateTransform1;
                        idCard.Item2.UpdateLayout();*/
                    }

                    printDuplex(dialog, idCard.Item1, idCard.Item2, string.Format("Student ID {0}", model.CurrentStudent.DisplayName));

                    addFields.Clear();
                }
            }
            catch (ConstraintException cex)
            {
                Logger.Error(cex);
            }
            catch (Exception ex)
            {
                //Logger.Error("Could not print.", ex);
                MessageBox.Show("Could not print PVC ID: " + ex.Message);
            }
            finally
            {

            }
        }

         public void PrintStaffTempId(StaffCardViewModel model, bool printEnabled)
        {
            var queue = Application.Current.Properties["TempIdPrintQueue"] as PrintQueue;

            try
            {

                if (queue != null)
                {
                    addFields.Clear();
                    addFields = LoadFieldsList(true, model.CurrentPerson.PersonId, true);

                    var idCard = LoadIdCard(model.TempTemplate.Id, true);

                    var dialog = new PrintDialog();
                    dialog.PrintQueue = queue;
                    dialog.PrintTicket = new PrintTicket();
                    dialog.PrintTicket.PageOrientation = PageOrientation.Portrait;
                    dialog.PrintTicket.PageBorderless = PageBorderless.Borderless;
                     dialog.PrintTicket.OutputQuality = OutputQuality.Photographic;
                    dialog.PrintTicket.PageMediaSize = new PageMediaSize(215, 370);

                    dialog.PrintTicket.PageOrientation = PageOrientation.Portrait;
                    if (idCard.Item3.Equals(App.APP_CARD_ORIENTATION_LANDSCAPE))
                    {
                        RotateTransform rotateTransform1 = new RotateTransform(90);

                        idCard.Item1.LayoutTransform = rotateTransform1;
                        idCard.Item1.UpdateLayout();

                    }


                    //dialog.PrintVisual(idCard.Item1, "Print ID Card");
                    
                    /*if (queue.Name.ToUpper().Contains("DYMO") || queue.Name.ToUpper().Contains("STAR"))
                    {
                        var rotate = new RotateTransform(90);
                        idCard.Item1.LayoutTransform = rotate;
                        idCard.Item1.UpdateLayout();
                    }*/

                    var document = new FixedDocument();
                    document.DocumentPaginator.PageSize = new Size(dialog.PrintableAreaWidth, dialog.PrintableAreaHeight);

                    document.PrintTicket = dialog.PrintTicket;

                    var content = new PageContent();
                    var page = new FixedPage();
                    page.Width = document.DocumentPaginator.PageSize.Width;
                    page.Height = document.DocumentPaginator.PageSize.Height;
                    page.Children.Add(idCard.Item1);
                    document.Pages.Add(content);
                    ((IAddChild)content).AddChild(page);
                    dialog.PrintDocument(document.DocumentPaginator, "Temporary ID");
                    addFields.Clear();
                  
                }
            }
            catch (Exception ex)
            {
                //Logger.Error("Could not print.", ex);
                MessageBox.Show("Could not print STAFF TEMP ID: " + ex.Message);
            }
            finally
            {
               
            }
        }

        public void PrintStaffPvcId(StaffCardViewModel model, bool printEnabled)
        {
            var queue = Application.Current.Properties["PvcPrintQueue"] as PrintQueue;

            try
            {

                if (queue != null)
                {
                    addFields.Clear();
                    addFields = LoadFieldsList(false, model.CurrentPerson.PersonId, true);

                    var idCard = LoadIdCard(model.PvcTemplate.Id, false);

                    var dialog = new PrintDialog();
                    dialog.PrintQueue = queue;
                    dialog.PrintTicket = new PrintTicket();
                    dialog.PrintTicket.PageOrientation = PageOrientation.Portrait;
                    dialog.PrintTicket.PageBorderless = PageBorderless.Borderless;
                     dialog.PrintTicket.OutputQuality = OutputQuality.Photographic;
                    dialog.PrintTicket.PageMediaSize = new PageMediaSize(215, 370);

                    dialog.PrintTicket.PageOrientation = PageOrientation.Portrait;

                    if (idCard.Item3.Equals(App.APP_CARD_ORIENTATION_LANDSCAPE))
                    {
                        dialog.PrintTicket.PageOrientation = PageOrientation.Landscape;

                        /*RotateTransform rotateTransform1 = new RotateTransform(90);

                        idCard.Item1.LayoutTransform = rotateTransform1;
                        idCard.Item1.UpdateLayout();

                        idCard.Item2.LayoutTransform = rotateTransform1;
                        idCard.Item2.UpdateLayout();*/
                    }
                 
                    /* 
                    var document = new FixedDocument();
                    document.DocumentPaginator.PageSize = new Size(dialog.PrintableAreaWidth, dialog.PrintableAreaHeight);

                    document.PrintTicket = dialog.PrintTicket;

                    var content = new PageContent();
                    var page = new FixedPage();
                    page.Width = document.DocumentPaginator.PageSize.Width;
                    page.Height = document.DocumentPaginator.PageSize.Height;
                    page.Children.Add(idCard.Item1);
                    document.Pages.Add(content);
                    ((IAddChild) content).AddChild(page);
                    dialog.PrintDocument(document.DocumentPaginator, "PVC ID");*/
                    printDuplex(dialog, idCard.Item1, idCard.Item2, "Staff ID");
                    addFields.Clear();

                }
            }
            catch (ConstraintException cex)
            {
                Logger.Error(cex);
            }
            catch (Exception ex)
            {
                //Logger.Error("Could not print.", ex);
                MessageBox.Show("Could not print STAFF PVC ID: " + ex.Message);
            }
            finally
            {

            }
        }

        void issueConsequence(Scan model, PrintQueue queue, string message, string code, int outcomeType, DateTime serveByDate)
        {
            var detention = new Consequence()
            {
                InfractionDate = DateTime.Now,
                Details = message,
                StudentNumber = model.Barcode,
                //todo: bind explicitly to student number - barcode *could* be an alt id - is not currently an alt id
                StudentGuid = model.StudentGuid,
                Units = 1,
                InfractionCode = code,
                OutcomeType = outcomeType,
                ServeBy = serveByDate
            };

            Detentions.InsertObject(detention);

            detention.StudentName = model.StudentName;
            detention.Grade = model.GradeLabel;
            detention.Homeroom = model.HomeroomLabel;
            detention.StudentImage = model.ScanImage;

            var alert = new Alert();
            var alertPrint = new PrintModel<Consequence>(detention);
            alertPrint.Title = "Alert Report";
            alertPrint.SchoolName = Settings.Default.School;

            alert.DataContext = alertPrint;
            if (queue != null && PrintPasses)
            {
                printVisual(queue, alert, "Alert Report", new RotateTransform(0));
            }
        }

        Alert GenerateConsequence(Scan model, string message, string code, int outcomeType, DateTime serveByDate)
        {
            var detention = new Consequence()
            {
                InfractionDate = DateTime.Now,
                Details = message,
                StudentNumber = model.Barcode,
                //todo: bind explicitly to student number - barcode *could* be an alt id - is not currently an alt id
                StudentGuid = model.StudentGuid,
                Units = 1,
                InfractionCode = code,
                OutcomeType = outcomeType,
                ServeBy = serveByDate
            };

            Detentions.InsertObject(detention);

            detention.StudentName = model.StudentName;
            detention.Grade = model.GradeLabel;
            detention.Homeroom = model.HomeroomLabel;
            detention.StudentImage = model.ScanImage;

            var alert = new Alert();
            alert.Margin = new Thickness(10, 10, 0, 0);
            var alertPrint = new PrintModel<Consequence>(detention);
            alertPrint.Title = "Alert Report";
            alertPrint.SchoolName = Settings.Default.School;

            alert.DataContext = alertPrint;
            var scale = Scale();

            var transform = new TransformGroup();
            if (scale != null)
                transform.Children.Add(scale);

            alert.LayoutTransform = transform;

            return alert;
        }

        ShortAlert GenerateShortConsequence(Scan model, string message, string code, int outcomeType, DateTime serveByDate)
        {
            var detention = new Consequence()
            {
                InfractionDate = DateTime.Now,
                Details = message,
                StudentNumber = model.Barcode,
                //todo: bind explicitly to student number - barcode *could* be an alt id - is not currently an alt id
                StudentGuid = model.StudentGuid,
                Units = 1,
                InfractionCode = code,
                OutcomeType = outcomeType,
                ServeBy = serveByDate
            };

            Detentions.InsertObject(detention);

            detention.StudentName = model.StudentName;
            detention.Grade = model.GradeLabel;
            detention.Homeroom = model.HomeroomLabel;
            detention.StudentImage = model.ScanImage;

            var alert = new ShortAlert();
            alert.Margin = new Thickness(15, 15, 0, 0);
            //alert.Padding = new Thickness(15, 15, 0, 0);
            var alertPrint = new PrintModel<Consequence>(detention);
          
            alert.DataContext = alertPrint;
            var scale = Scale();

            var transform = new TransformGroup();
            if (scale != null)
                transform.Children.Add(scale);

            alert.LayoutTransform = transform;

            return alert;
        }

        void PrintAlert(Scan model, PrintQueue queue)
        {
            if (model.HasAlerts)
            {
                var alertsPrintedToday = AlertsPrinted.GetItemsByDate(DateTime.Today).ToArray();
                foreach (var alert in model.Alerts)
                {
                    //alert was already printed today
                    if (alertsPrintedToday.Any(x => x.StudentGuid == model.StudentGuid && x.Details == alert.AlertText))
                        continue;

                    var sound = Settings.Default.SoundsFolder + "\\" + alert.AlertSound;

                    Application.Current.Dispatcher.Invoke(() => PlaySound(sound), DispatcherPriority.Normal);

                    var print = new AlertPrinted()
                    {
                        AlertId = alert.AlertId,
                        Details = alert.AlertText,
                        StudentNumber = model.Barcode,   
                        StudentGuid = model.StudentGuid,
                        StudentName = model.StudentName,
                        Grade = model.GradeLabel,
                        Homeroom = model.HomeroomLabel,
                        StudentImage = model.ScanImage,
                        CorrelationId = alert.CorrelationId,
                    };

                    var alertView = new Alert();

                    var alertPrint = new PrintModel<AlertPrinted>(print);
                    alertPrint.Title = "Alert Report";
                    alertPrint.SchoolName = Settings.Default.School;

                    alertView.DataContext = alertPrint;

                    if (queue != null && PrintPasses)
                    {
                        printVisual(queue, alertView, "Alert Report", new RotateTransform(0));
                    }

                    AlertWasPrinted(print);
                }
                
            }
        }

        Control[] GenerateAlert(Scan model, Size paperSize, Size printSize)
        {
            var list = new List<Control>();

            if (model.HasAlerts)
            {
                var alertsPrintedToday = AlertsPrinted.GetItemsByDate(DateTime.Today).ToArray();
                foreach (var alert in model.Alerts.Where(a=>a.AlertId > -99)) //-99 is a artificial alert - we don't print
                {
                    /**** don't print wrong group alerts ****/
                    if (alert.AlertText.ToLower().Contains("wrong group"))
                        continue;

                    //alert was already printed today
                    if (alertsPrintedToday.Any(x =>
                            x.StudentGuid == model.StudentGuid && x.Details == alert.AlertText))
                        continue;

                    var print = new AlertPrinted()
                    {
                        AlertId = alert.AlertId,
                        Details = alert.AlertText,
                        StudentNumber = model.Barcode,
                        StudentGuid = model.StudentGuid,
                        StudentName = model.StudentName,
                        Grade = model.GradeLabel,
                        Homeroom = model.HomeroomLabel,
                        StudentImage = model.ScanImage,
                        CorrelationId = alert.CorrelationId,
                    };


                    Control alertView = new Alert();

                    alertView.Margin = new Thickness(10, 10, 0, 0);

                    var alertPrint = new PrintModel<AlertPrinted>(print);
                    alertPrint.Title = "Alert Report";
                    alertPrint.SchoolName = Settings.Default.School;

                    alertView.DataContext = alertPrint;
                    var scale = Scale();

                    var transform = new TransformGroup();
                    if (scale != null)
                        transform.Children.Add(scale);

                    alertView.LayoutTransform = transform;

                    alertView.Measure(paperSize);

                    Point ptGrid = new Point(printSize.Width, printSize.Height);
                    alertView.Arrange(new Rect(ptGrid, paperSize));

                    if (PrintPasses)
                    {
                        list.Add(alertView);
                    }

                    AlertWasPrinted(print);
                    
                }

            }

            return list.ToArray();
        }
        /*Canvas cloneCanvas(Canvas visual)
        {

            string gridXaml = XamlWriter.Save(visual);

            StringReader stringReader = new StringReader(gridXaml);
            XmlReader xmlReader = XmlReader.Create(stringReader);
            var clone = (Canvas)XamlReader.Load(xmlReader);
            clone.Visibility = Visibility.Visible;

            return clone;
        }*/

        void printDuplex(PrintDialog dialog, Canvas frontCanvas, Canvas backCanvas, string jobtitle)
        {
            int duplexEdge = Settings.Default.DuplexSetting;

            PrintCapabilities capabilities = dialog.PrintQueue.GetPrintCapabilities(dialog.PrintTicket);
            var sz = new Size(capabilities.PageImageableArea.ExtentWidth, capabilities.PageImageableArea.ExtentHeight);
            Logger.WarnFormat("Extent Width: {0} Extent Height {1}", sz.Width, sz.Height);
            
            var sz1 = new Size(capabilities.PageImageableArea.OriginWidth, capabilities.PageImageableArea.OriginHeight);
            Logger.WarnFormat("Origin Width: {0} Origin Height {1}", sz1.Width, sz1.Height);
            
            var sz2 = new Size(dialog.PrintableAreaWidth, dialog.PrintableAreaHeight);
            Logger.WarnFormat("Printable Width: {0} Origin Height {1}", sz2.Width, sz2.Height);
          
            Logger.WarnFormat("Page Resolution: {0} ", dialog.PrintTicket.PageResolution);

            dialog.PrintTicket.PageMediaSize = new PageMediaSize(sz.Width, sz.Height);
            
            if (backCanvas.Children.Count > 0)
            {
                dialog.PrintTicket.Duplexing = (Duplexing)duplexEdge;
            }

            var document = new FixedDocument();
            document.PrintTicket = dialog.PrintTicket;

            var page = new FixedPage();
            page.Width = sz.Width;
            page.Height = sz.Height;
            page.Children.Add(frontCanvas);

            var content = new PageContent();
            document.Pages.Add(content);
            ((IAddChild)content).AddChild(page);
            page.UpdateLayout();

            if (backCanvas.Children.Count > 0)
            {
                var page2 = new FixedPage();
                page.Width = sz.Width;
                page.Height = sz.Height;
                page2.Children.Add(backCanvas);

                var content2 = new PageContent();
                document.Pages.Add(content2);
                ((IAddChild) content2).AddChild(page2);
                page2.UpdateLayout();
            }

            dialog.PrintDocument(document.DocumentPaginator, jobtitle);
        }

        void printVisual(PrintQueue queue, UserControl pass, string jobTitle, RotateTransform rotate, int width = 480, int height = 270)
        {
            bool isPassPrinter = false;
          
            try
            {
                
                pass.Width = width;
                pass.Height = height;

                var dialog = new PrintDialog();
                dialog.PrintQueue = queue;
                dialog.PrintTicket = new PrintTicket();
                PrintCapabilities capabilities = dialog.PrintQueue.GetPrintCapabilities(dialog.PrintTicket);
                var sz = new Size(capabilities.PageImageableArea.ExtentWidth, capabilities.PageImageableArea.ExtentHeight);

                //Logger.WarnFormat("Print size: W: {0} H: {1}", sz.Width, sz.Height);
                //Logger.WarnFormat("Print size: PG MEDIA - W: {0} H: {1}", capabilities.OrientedPageMediaWidth, capabilities.OrientedPageMediaHeight);

                dialog.PrintTicket.PageOrientation = PageOrientation.Portrait;
                dialog.PrintTicket.PageBorderless = PageBorderless.Borderless;
                 dialog.PrintTicket.OutputQuality = OutputQuality.Photographic;
                dialog.PrintTicket.PageMediaSize = new PageMediaSize(sz.Width, sz.Height);

                //page.Margin = new Thickness(5);
                if (queue.Name.ToUpper().Contains("DYMO"))
                {
                    //Logger.Warn("Printing to DYMO Print Queue");

                    isPassPrinter = true;
                    //dialog.PrintTicket.PageMediaSize = new PageMediaSize(215, 370);

                }

                if (jobTitle == "Alert Report" || jobTitle == "Print Receipt")
                {
                  
                    pass.Height = 450;
                    pass.Width = 270;
                }

                if (queue.Name.ToUpper().Contains("STAR") || queue.Name.ToUpper().Contains("MZ 220"))
                {
                    //Logger.Warn("Printing to Label Print Queue");
                    dialog.PrintTicket.PageMediaSize = new PageMediaSize(sz.Width, sz.Height + 50);

                    if (jobTitle == "Alert Report")
                    {
                        pass.Margin = new Thickness(-15, 0, 0, 0);
                      
                    }
                    else
                    {
                        //dialog.PrintTicket.PageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA5Rotated);
                        //dialog.PrintTicket.PageOrientation = PageOrientation.Landscape;
                        if (Settings.Default.PrintScaleFactor > 1)
                        {
                            //second parameter is north/south alignment X: -30, Y:-130
                            pass.Margin = new Thickness(Settings.Default.PrintOffsetX, Settings.Default.PrintOffsetY, 0, 0);

                            //dialog.PrintTicket.PageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA5Extra);
                        }
                        else
                        {
                            pass.Margin = new Thickness(-20, 0, 0, 0);
                        }

                    }

                    //dialog.PrintTicket.PageMediaSize = new PageMediaSize(pass.Width, pass.Height);

                }

                pass.UpdateLayout();

                Logger.WarnFormat("Scale: {0}, OffsetY: {1}, OffsetX: {2}, Size: W {3}, H {4}", Settings.Default.PrintScaleFactor, Settings.Default.PrintOffsetY, Settings.Default.PrintOffsetX, sz.Width, sz.Height);
                //dialog.PrintVisual(pass,jobTitle);


                var document = new FixedDocument();
           
                document.DocumentPaginator.PageSize = sz;

                document.PrintTicket = dialog.PrintTicket;

                var page = new FixedPage();
                page.Width = sz.Width;
                page.Height = sz.Height;

                /*
                if (!isPassPrinter)
                {

                    page.Width = document.DocumentPaginator.PageSize.Width;
                    page.Height = document.DocumentPaginator.PageSize.Height;


                }*/
                
                /*
                pass.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
                pass.Arrange(new Rect(pass.DesiredSize));
                SaveToPng(pass, "c:\\temp\\pass.png");
                */

                var transform = new TransformGroup();
                var scale = Scale();
                if (scale != null)
                {
                    transform.Children.Add(scale);
                }
                transform.Children.Add(rotate);
                pass.LayoutTransform = transform;
                //pass.RenderTransform = transform;

                //pass.Arrange(new Rect(new Point(capabilities.PageImageableArea.OriginWidth, capabilities.PageImageableArea.OriginHeight), sz));

                page.Children.Add(pass);
                //dialog.PrintVisual(pass, jobTitle);

                var content = new PageContent();
                document.Pages.Add(content);
                ((IAddChild)content).AddChild(page);
                page.UpdateLayout();


                //Logger.WarnFormat("Print res: {0} {1}", dialog.PrintTicket.PageResolution.X, dialog.PrintTicket.PageResolution.Y);

                dialog.PrintDocument(document.DocumentPaginator, jobTitle);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

        }

        void PrintDocument(PrintQueue queue, FixedPage[] pages, string jobTitle)
        {
          
            try
            {
                var dialog = new PrintDialog();
                dialog.PrintQueue = queue;
                dialog.PrintTicket = new PrintTicket();
                PrintCapabilities capabilities = dialog.PrintQueue.GetPrintCapabilities(dialog.PrintTicket);
                var sz = new Size(capabilities.PageImageableArea.ExtentWidth, capabilities.PageImageableArea.ExtentHeight);

                //Logger.WarnFormat("Print size: W: {0} H: {1}", sz.Width, sz.Height);
                //Logger.WarnFormat("Print size: PG MEDIA - W: {0} H: {1}", capabilities.OrientedPageMediaWidth, capabilities.OrientedPageMediaHeight);

                dialog.PrintTicket.PageOrientation = PageOrientation.Portrait;
                dialog.PrintTicket.PageBorderless = PageBorderless.Borderless;
                 dialog.PrintTicket.OutputQuality = OutputQuality.Photographic;
                dialog.PrintTicket.PageMediaSize = new PageMediaSize(sz.Width, sz.Height);
                dialog.PrintTicket.PageMediaSize = new PageMediaSize(sz.Width, sz.Height + 50);

                Logger.WarnFormat("Scale: {0}, OffsetY: {1}, OffsetX: {2}, Size: W {3}, H {4}", Settings.Default.PrintScaleFactor, Settings.Default.PrintOffsetY, Settings.Default.PrintOffsetX, sz.Width, sz.Height);

                var document = new FixedDocument();
                document.PrintTicket = dialog.PrintTicket;

                foreach (var page in pages)
                {
                    var content = new PageContent();
                    document.Pages.Add(content);
                    ((IAddChild)content).AddChild(page);
                    page.UpdateLayout();
                   
                }

                dialog.PrintDocument(document.DocumentPaginator, jobTitle);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

        }

        void PrintDocument(PrintQueue queue, FixedPage page, string jobTitle)
        {

            try
            {
                var dialog = new PrintDialog();
                dialog.PrintQueue = queue;
                dialog.PrintTicket = new PrintTicket();
                PrintCapabilities capabilities = dialog.PrintQueue.GetPrintCapabilities(dialog.PrintTicket);
                var sz = new Size(capabilities.PageImageableArea.ExtentWidth, capabilities.PageImageableArea.ExtentHeight);

                //Logger.WarnFormat("Print size: W: {0} H: {1}", sz.Width, sz.Height);
                //Logger.WarnFormat("Print size: PG MEDIA - W: {0} H: {1}", capabilities.OrientedPageMediaWidth, capabilities.OrientedPageMediaHeight);

                dialog.PrintTicket.PageOrientation = PageOrientation.Portrait;
                dialog.PrintTicket.PageBorderless = PageBorderless.Borderless;
                 dialog.PrintTicket.OutputQuality = OutputQuality.Photographic;
                dialog.PrintTicket.PageMediaSize = new PageMediaSize(sz.Width, sz.Height);
                dialog.PrintTicket.PageMediaSize = new PageMediaSize(sz.Width, sz.Height + 50);

                Logger.WarnFormat("Scale: {0}, OffsetY: {1}, OffsetX: {2}, Size: W {3}, H {4}", Settings.Default.PrintScaleFactor, Settings.Default.PrintOffsetY, Settings.Default.PrintOffsetX, sz.Width, sz.Height);

                var document = new FixedDocument();
                document.PrintTicket = dialog.PrintTicket;

                var content = new PageContent();
                document.Pages.Add(content);
                ((IAddChild)content).AddChild(page);
                page.UpdateLayout();

                dialog.PrintDocument(document.DocumentPaginator, jobTitle);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

        }

        void PrintTestDocument(PrintQueue queue)
        {

            try
            {
                var dialog = new PrintDialog();
                dialog.PrintQueue = queue;
                dialog.PrintTicket = new PrintTicket();
                PrintCapabilities capabilities = dialog.PrintQueue.GetPrintCapabilities(dialog.PrintTicket);
                var sz = new Size(capabilities.PageImageableArea.ExtentWidth, capabilities.PageImageableArea.ExtentHeight);

                //Logger.WarnFormat("Print size: W: {0} H: {1}", sz.Width, sz.Height);
                //Logger.WarnFormat("Print size: PG MEDIA - W: {0} H: {1}", capabilities.OrientedPageMediaWidth, capabilities.OrientedPageMediaHeight);

                dialog.PrintTicket.PageOrientation = PageOrientation.Portrait;
                dialog.PrintTicket.PageBorderless = PageBorderless.Borderless;
                 dialog.PrintTicket.OutputQuality = OutputQuality.Photographic;
                //dialog.PrintTicket.PageMediaSize = new PageMediaSize(sz.Width, sz.Height);
                dialog.PrintTicket.PageMediaSize = new PageMediaSize(sz.Width, sz.Height + 50);

                Logger.WarnFormat("Scale: {0}, OffsetY: {1}, OffsetX: {2}, Size: W {3}, H {4}", Settings.Default.PrintScaleFactor, Settings.Default.PrintOffsetY, Settings.Default.PrintOffsetX, sz.Width, sz.Height);

                var document = new FixedDocument();
                document.PrintTicket = dialog.PrintTicket;

                var test = new TestPrint();

                var page = CreatePage(test, sz);
               
                var content = new PageContent();
                document.Pages.Add(content);
                ((IAddChild) content).AddChild(page);
                page.UpdateLayout();

                dialog.PrintDocument(document.DocumentPaginator, "Swipe Desktop Test Print");
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

        }

        FixedPage CreatePage(Control pass, Size sz)
        {
            bool isPassPrinter = false;
            var page = new FixedPage();

            try
            {
             
              
                page.Width = sz.Width;
                page.Height = sz.Height;
                page.Children.Add(pass);
             
                page.UpdateLayout();

             
                //Logger.WarnFormat("Print res: {0} {1}", dialog.PrintTicket.PageResolution.X, dialog.PrintTicket.PageResolution.Y);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return page;
        }
        FixedPage CreateStackedPage(Control[] print, Size sz)
        {
            bool isPassPrinter = false;
            var page = new FixedPage();

            try
            {
                page.Width = sz.Width;
                page.Height = sz.Height;
                var stack = new System.Windows.Controls.StackPanel();
                stack.Orientation = Orientation.Vertical;

                foreach (var pass in print)
                    stack.Children.Add(pass);

                page.Children.Add(stack);

                page.UpdateLayout();


                //Logger.WarnFormat("Print res: {0} {1}", dialog.PrintTicket.PageResolution.X, dialog.PrintTicket.PageResolution.Y);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return page;
        }

        FixedPage[] CreatePages(Control[] print, Size sz)
        {
            var pages = new List<FixedPage>();

            bool isPassPrinter = false;
          
            try
            {
                foreach (var pass in print)
                {
                    var page = new FixedPage();

                    page.Width = sz.Width;
                    page.Height = sz.Height;

                    page.Children.Add(pass);

                    page.UpdateLayout();

                    pages.Add(page);
                }


                //Logger.WarnFormat("Print res: {0} {1}", dialog.PrintTicket.PageResolution.X, dialog.PrintTicket.PageResolution.Y);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return pages.ToArray();
        }


        ScaleTransform Scale()
        {
            var scaleFactor = Settings.Default.PrintScaleFactor;

            if (scaleFactor != 0)
                return new ScaleTransform(scaleFactor, scaleFactor);

            return null;
        }
        void SaveToPng(FrameworkElement visual, string fileName)
        {
            try
            {
                var encoder = new PngBitmapEncoder();
                SaveUsingEncoder(visual, fileName, encoder);
            }catch(Exception ex)
            {
                Logger.Error(ex);   
            }
        }

        // and so on for other encoders (if you want)


        void SaveUsingEncoder(FrameworkElement visual, string fileName, BitmapEncoder encoder)
        {
            RenderTargetBitmap bitmap = new RenderTargetBitmap((int)visual.ActualWidth, (int)visual.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            BitmapFrame frame = BitmapFrame.Create(bitmap);
            encoder.Frames.Add(frame);

            using (var stream = File.Create(fileName))
            {
                encoder.Save(stream);
            }
        }

        private void OnLunchPeriodsReturned(IEnumerable<string> periods)
        {
            _periodList.ItemsSource.Clear();
          
            if (SwipeMode == SwipeMode.CafeEntrance)
            {
                periods.ForEach(x => _periodList.ItemsSource.Add(x));
                _periodList.SelectedItem = periods.FirstOrDefault();

            }

        }

        private void OnGroupsReturned(IEnumerable<string> groups)
        {
            _locationList.ItemsSource.Clear();

            if (SwipeMode == SwipeMode.Group)
            {
                groups.ForEach(x => _locationList.ItemsSource.Add(x));
               
                _locationList.SelectedItem = !string.IsNullOrEmpty(Settings.Default.SelectedGroup)
                    ? Settings.Default.SelectedGroup : groups.FirstOrDefault();
            }
        
        }

        private void OnLocationsReturned(IEnumerable<ScanLocation> locations)
        {
            Locations.Clear();
            Locations.AddRange(locations);

            if (SwipeMode == SwipeMode.CafeEntrance)
            {
                var periods = Locations.Select(s => s.PeriodCode).Distinct().ToArray();

                _periodList.ItemsSource.Clear();
                periods.ForEach(x => _periodList.ItemsSource.Add(x));
                _periodList.SelectedItem = !string.IsNullOrEmpty(Settings.Default.StartupPeriod)
                    ? Settings.Default.StartupPeriod
                    : periods.FirstOrDefault();
            }

            if (SwipeMode == SwipeMode.ClassroomTardy || Dismissal )
            {
                var periods = Locations.Select(s => s.PeriodCode).Distinct().ToArray();

                _periodList.ItemsSource.Clear();
                periods.ForEach(x => _periodList.ItemsSource.Add(x));
                _periodList.SelectedItem = !string.IsNullOrEmpty(Settings.Default.StartupPeriod)
                    ? Settings.Default.StartupPeriod
                    : periods.FirstOrDefault();

                var attCodes = Locations.Select(s => s.AttendanceCode).Distinct().ToArray();

                _attendanceCodeList.ItemsSource.Clear();
                attCodes.ForEach(x => _attendanceCodeList.ItemsSource.Add(x));
                _attendanceCodeList.SelectedItem = !string.IsNullOrEmpty(Settings.Default.StartupCode)
                    ? Settings.Default.StartupCode
                    : attCodes.FirstOrDefault();
            }
            else if (SwipeMode == SwipeMode.Location)
            {
                var list = Locations.Select(s => s.RoomName).Distinct().ToArray();
                _locationList.ItemsSource.Clear();
                list.ForEach(x => _locationList.ItemsSource.Add(x));
                //_locationList.ItemsSource.AddRange(list);
                _locationList.SelectedItem = !string.IsNullOrEmpty(Settings.Default.StartupLocation)
                    ? Settings.Default.StartupLocation : list.FirstOrDefault();

                if(!list.Contains(_locationList.SelectedItem))
                {
                    _locationList.SelectedItem = list.FirstOrDefault();
                }

                Logger.Debug(_locationList.SelectedItem);
            }

        }

        public ReactiveCommand<object> ExitCommand { get; private set; }

        public ReactiveCommand<object> ReadScanCommand { get; private set; }

        public ReactiveCommand<object> SelectStudent{ get; private set; }

        public ReactiveCommand<object> ShowSettingsCommand { get; private set; }

        public ReactiveCommand<object> SyncNowCommand { get; private set; }

        public ReactiveCommand<object> TestPrintCommand { get; private set; }
    }
}
