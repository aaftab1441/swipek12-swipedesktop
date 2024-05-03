using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Printing;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Oak;
using ReactiveUI;
using SwipeDesktop.Api;
using SwipeDesktop.Interfaces;
using Ookii;
using SwipeDesktop.Modal;
using SwipeDesktop.Models;
using SwipeDesktop.Storage;
using Telerik.Windows.Controls.Rating;
using Common;
using SwipeDesktop.Common;

namespace SwipeDesktop.ViewModels
{
    public class SettingsViewModel : ReactiveObject, IViewModel //, ReactiveUI.IRoutableViewModel
    {
        public ReactiveCommand<object> AddPersonCommand { get; set; }

        static ReactiveList<string> _passTypes = new ReactiveList<string>(new[] { "Separate Pass and Alert", "Combined Pass and Alert" });
        
        static ReactiveList<Duplexing> _duplexSettings = new ReactiveList<Duplexing>(new[] {
            Duplexing.Unknown,
            Duplexing.TwoSidedLongEdge,
            Duplexing.TwoSidedShortEdge
        });

        IViewModel _content;
        public IViewModel IdCardContent
        {
            get { return _content; }
            set
            {
                this.RaiseAndSetIfChanged(ref _content, value);
            }
        }

        PopupViewModel _popup;
        public PopupViewModel PopupWindow
        {
            get { return _popup; }
            set
            {
                this.RaiseAndSetIfChanged(ref _popup, value);
            }
        }

        private readonly LocalStorage Storage;
        private readonly IdCardStorage IdCardStorage;

        private ReactiveList<Tuple<string, int>> _stats;
        public ReactiveList<Tuple<string, int>> DatabaseStats
        {
            get { return _stats; }
            set { this.RaiseAndSetIfChanged(ref _stats, value); }
        }

        public ReactiveList<string> PassTypes
        {
            get { return _passTypes; }
            set { this.RaiseAndSetIfChanged(ref _passTypes, value); }
        }

        public ReactiveList<Duplexing> DuplexSettings
        {
            get { return _duplexSettings; }
            set { this.RaiseAndSetIfChanged(ref _duplexSettings, value); }
        }

        ObservableAsPropertyHelper<string> _selectedPassPrinter;
        public string PassPrinter
        {
            get { return _selectedPassPrinter.Value; }
        }

        ObservableAsPropertyHelper<string> _selectedTempIdPrinter;
        public string TempIdPrinter
        {
            get { return _selectedTempIdPrinter.Value; }
        }

        ObservableAsPropertyHelper<string> _selectedPvcPrinter;
        public string PvcPrinter
        {
            get { return _selectedPvcPrinter.Value; }
        }

        private string _selectedPassType;

        public String SelectedPassType
        {
            get { return _selectedPassType; }
            set { this.RaiseAndSetIfChanged(ref _selectedPassType, value); }
        }


        private string _host;

        public string Host
        {
            get { return _host; }
            set { this.RaiseAndSetIfChanged(ref _host, value); }
        }


        private string _sqlIp;

        public string SqlIp
        {
            get { return _sqlIp; }
            set { this.RaiseAndSetIfChanged(ref _sqlIp, value); }
        }

        private Duplexing _duplexSettingValue;

        public Duplexing SelectedDuplexValue
        {
            get { return _duplexSettingValue; }
            set { this.RaiseAndSetIfChanged(ref _duplexSettingValue, value); }
        }

        private string _config;

        public string Config
        {
            get { return _config; }
            set { this.RaiseAndSetIfChanged(ref _config, value); }
        }


        ObservableAsPropertyHelper<bool> _showDefault;
        public bool DefaultShown
        {
            get { return _showDefault != null && _showDefault.Value ; }
        }

        private bool _takeAttendance;

        public bool TakeAttendanceInLocationMode
        {
            get { return _takeAttendance; }
            set { this.RaiseAndSetIfChanged(ref _takeAttendance, value); }
        }

        private bool _allowStudentKiosk;

        public bool AllowStudentKiosk
        {
            get { return _allowStudentKiosk; }
            set { this.RaiseAndSetIfChanged(ref _allowStudentKiosk, value); }
        }


        private bool _allowVisitorKiosk;

        public bool AllowVisitorKiosk
        {
            get { return _allowVisitorKiosk; }
            set { this.RaiseAndSetIfChanged(ref _allowVisitorKiosk, value); }
        }


        /*
        private bool _allowStaffKiosk;

        public bool AllowStaffKiosk
        {
            get { return _allowStaffKiosk; }
            set { this.RaiseAndSetIfChanged(ref _allowStaffKiosk, value); }
        }
        */

        private bool _allowKioskLocationAttendance;

        public bool AllowKioskLocationAttendance
        {
            get { return _allowKioskLocationAttendance; }
            set { this.RaiseAndSetIfChanged(ref _allowKioskLocationAttendance, value); }
        }

        private bool _markPresentLocationMode;

        public bool MarkPresentInLocationMode
        {
            get { return _markPresentLocationMode; }
            set { this.RaiseAndSetIfChanged(ref _markPresentLocationMode, value); }
        }

        private bool _inlcudeStaff;

        public bool IncludeStaff
        {
            get { return _inlcudeStaff; }
            set { this.RaiseAndSetIfChanged(ref _inlcudeStaff, value); }
        }

        private bool _enableLunchAlerts;

        public bool EnableLunchAlerts
        {
            get { return _enableLunchAlerts; }
            set { this.RaiseAndSetIfChanged(ref _enableLunchAlerts, value); }
        }

        private bool _suppressIdOnPass;

        public bool SuppressIdOnPass
        {
            get { return _suppressIdOnPass; }
            set { this.RaiseAndSetIfChanged(ref _suppressIdOnPass, value); }
        }


        ObservableAsPropertyHelper<bool> _showAdvanced;
        public bool AdvancedShown
        {
            get { return _showAdvanced != null && _showAdvanced.Value; }
        }

        ObservableAsPropertyHelper<bool> _showKiosk;
        public bool KioskShown
        {
            get { return _showKiosk != null && _showKiosk.Value; }
        }


        ObservableAsPropertyHelper<bool> _showStats;
        public bool StatsShown
        {
            get { return _showStats != null && _showStats.Value; }
        }

        private int _studentCount;
        public int StudentCount
        {
            get { return _studentCount; }
            set { this.RaiseAndSetIfChanged(ref _studentCount, value); }
        }
        private int _consequenceCount;
        public int ConsequencesCount
        {
            get { return _consequenceCount; }
            set { this.RaiseAndSetIfChanged(ref _consequenceCount, value); }
        }
        private int _timetableCount;
        public int TimeTableCount
        {
            get { return _timetableCount; }
            set { this.RaiseAndSetIfChanged(ref _timetableCount, value); }
        }

        private int _alertCount;
        public int AlertCount
        {
            get { return _alertCount; }
            set { this.RaiseAndSetIfChanged(ref _alertCount, value); }
        }

        private int _lunchCount;
        public int LunchRecordCount
        {
            get { return _lunchCount; }
            set { this.RaiseAndSetIfChanged(ref _lunchCount, value); }
        }

        private double _printScaleFactor;
        public double PrintScaleFactor
        {
            get { return _printScaleFactor; }
            set { this.RaiseAndSetIfChanged(ref _printScaleFactor, value); }
        }

        private int _printOffsetX;
        public int PrintOffsetX
        {
            get { return _printOffsetX; }
            set { this.RaiseAndSetIfChanged(ref _printOffsetX, value); }
        }
        private int _printOffsetY;
        public int PrintOffsetY
        {
            get { return _printOffsetY; }
            set { this.RaiseAndSetIfChanged(ref _printOffsetY, value); }
        }

        private ReactiveList<CardTemplate> _idCards;
        public ReactiveList<CardTemplate> IdCardTemplates
        {
            get { return _idCards; }
            set { this.RaiseAndSetIfChanged(ref _idCards, value); }
        }

        private CardTemplate _studentIdTemplate;
        public CardTemplate StudentIdTemplate
        {
            get { return _studentIdTemplate; }
            set { this.RaiseAndSetIfChanged(ref _studentIdTemplate, value); }
        }


        private bool _hideAdvanced;
        public bool HideAdvancedTabs
        {
            get { return _hideAdvanced; }
            set { this.RaiseAndSetIfChanged(ref _hideAdvanced, value); }
        }



        private CardTemplate _tempIdTemplate;
        public CardTemplate TempIdTemplate
        {
            get { return _tempIdTemplate; }
            set { this.RaiseAndSetIfChanged(ref _tempIdTemplate, value); }
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

        public ReactiveList<ScanLocation> Locations { get; set; }

        private void OnLocationsReturned(IEnumerable<ScanLocation> locations)
        {
            Locations.Clear();
            Locations.AddRange(locations);

            var list = Locations.Select(s => s.RoomName).Distinct().ToArray();
            _locationList.ItemsSource.Clear();
            list.ForEach(x => _locationList.ItemsSource.Add(x));
            
            _locationList.SelectedItem = !string.IsNullOrEmpty(Settings.Default.KioskLocation) ? Settings.Default.KioskLocation : list.FirstOrDefault();

            if (!list.Contains(_locationList.SelectedItem))
            {
                _locationList.SelectedItem = list.FirstOrDefault();
            }



        }

      
        public SettingsViewModel(LocalStorage storage)
        {

          
            var uiContext = TaskScheduler.FromCurrentSynchronizationContext();

            _locationList = new ReactiveDataSource<string>();

            Locations = new ReactiveList<ScanLocation>();
            /*
            Task.Run(() =>
            {

            });
            */

            DatabaseStats = new ReactiveList<Tuple<string, int>>();

            Storage = storage;
            Config = "Default";

            Printers = Application.Current.Properties["Printers"] as PrintQueueCollection;
            PassPrintQueue = Application.Current.Properties["PassPrintQueue"] as PrintQueue;
            TempIdPrinterQueue = Application.Current.Properties["TempIdPrintQueue"] as PrintQueue;
            PvcPrinterQueue = Application.Current.Properties["PvcPrintQueue"] as PrintQueue;

            ImagesFolder = Settings.Default.ImagesFolder;
            SoundsFolder = Settings.Default.SoundsFolder;
            SchoolName = Settings.Default.School;
            SchoolId = Settings.Default.SchoolId.ToString(CultureInfo.CurrentCulture);
            AlertStartDate = Settings.Default.AlertStartDate;
            TardyAlertCount = Settings.Default.TardyAlertCount;
            StartupAttendanceCode = Settings.Default.StartupCode;
            StartupPeriod = Settings.Default.StartupPeriod;
            TakeAttendanceInLocationMode = Settings.Default.TakeAttendance;
            MarkPresentInLocationMode = Settings.Default.MarkPresentInLocationMode;
            IncludeStaff = Settings.Default.IncludeStaff;
            EnableLunchAlerts = Settings.Default.EnableLunchAlerts;
            SuppressIdOnPass = Settings.Default.SuppressIdOnPass;
            SelectedPassType = Settings.Default.SelectedPassType;
            AllowKioskDismissalPass = Settings.Default.AllowKioskDismissalPass;
            AllowKioskLocation = Settings.Default.AllowKioskLocation;
            AllowKioskLocationPass = Settings.Default.AllowKioskLocationPass;
            AllowKioskSearchName = Settings.Default.AllowKioskSearchName;
            AllowKioskTardyPass = Settings.Default.AllowKioskTardyPass;
            

            PrintScaleFactor = Settings.Default.PrintScaleFactor;
            PrintOffsetX = Settings.Default.PrintOffsetX;
            PrintOffsetY = Settings.Default.PrintOffsetY;

            SqlIp = Settings.Default.SqlMasterIp;


            Host = Environment.MachineName;
            SelectedDuplexValue = (Duplexing)Settings.Default.DuplexSetting;

            this.WhenAnyValue(x => x.PassPrintQueue).Where(x=>x != null).Select(x => x.FullName).ToProperty(this, x => x.PassPrinter, out _selectedPassPrinter);
            this.WhenAnyValue(x => x.TempIdPrinterQueue).Where(x => x != null).Select(x => x.FullName).ToProperty(this, x => x.TempIdPrinter, out _selectedTempIdPrinter);
            this.WhenAnyValue(x => x.PvcPrinterQueue).Where(x => x != null).Select(x => x.FullName).ToProperty(this, x => x.PvcPrinter, out _selectedPvcPrinter);

            //this.WhenAnyValue(x => x.Config).Select(x => x == "Default").ToProperty(this, x => x.DefaultShown, out _showDefault);
            //this.WhenAnyValue(x => x.Config).Select(x => x == "Advanced").ToProperty(this, x => x.AdvancedShown, out _showAdvanced);
            //this.WhenAnyValue(x => x.Config).Select(x => x == "Stats").ToProperty(this, x => x.StatsShown, out _showStats);

            this.WhenAnyValue(x => x.TakeAttendanceInLocationMode).Subscribe(x =>
            {
                Settings.Default.TakeAttendance = x;
            });

            this.WhenAnyValue(x => x.SelectedDuplexValue).Subscribe(x =>
            {
                Settings.Default.DuplexSetting = (int)x;
            });

            this.WhenAnyValue(x => x.MarkPresentInLocationMode).Subscribe(x =>
            {
                Settings.Default.MarkPresentInLocationMode = x;
            });

            this.WhenAnyValue(x => x.IncludeStaff).Subscribe(x =>
            {
                Settings.Default.IncludeStaff = x;
            });

            this.WhenAnyValue(x => x.EnableLunchAlerts).Subscribe(x =>
            {
                Settings.Default.EnableLunchAlerts = x;
            });

            this.WhenAnyValue(x => x.SuppressIdOnPass).Subscribe(x =>
            {
                Settings.Default.SuppressIdOnPass = x;
            });

            this.WhenAnyValue(x => x.PrintOffsetX).Subscribe(x =>
            {
                Settings.Default.PrintOffsetX = x;
            });

            this.WhenAnyValue(x => x.PrintOffsetY).Subscribe(x =>
            {
                Settings.Default.PrintOffsetY = x;
            });

            this.WhenAnyValue(x => x.PrintScaleFactor).Subscribe(x =>
            {
                Settings.Default.PrintScaleFactor = x;
            });

            this.WhenAnyValue(x => x.SelectedPassType).Subscribe(x =>
            {
                Settings.Default.SelectedPassType = x;
            });


            this.WhenAnyValue(x => x.AllowKioskTardyPass).Subscribe(x =>
            {
                Settings.Default.AllowKioskTardyPass = x;
            });

            this.WhenAnyValue(x => x.AllowKioskLocation).Subscribe(x =>
            {
                Settings.Default.AllowKioskLocation = x;
            });

            this.WhenAnyValue(x => x.AllowKioskSearchName).Subscribe(x =>
            {
                Settings.Default.AllowKioskSearchName = x;
            });

            this.WhenAnyValue(x => x.AllowKioskLocationPass).Subscribe(x =>
            {
                Settings.Default.AllowKioskLocationPass = x;
            });

            this.WhenAnyValue(x => x.AllowKioskDismissalPass).Subscribe(x =>
            {
                Settings.Default.AllowKioskDismissalPass = x;
            });

            this.WhenAnyValue(x => x.KioskLocation).Subscribe(x =>
            {

                Settings.Default.KioskLocation = x;
                
            });

            /*
            this.WhenAnyValue(x => x.AllowStaffKiosk).Subscribe(x =>
            {

                Settings.Default.StaffKioskEntry = x;

            });
            */

            this.WhenAnyValue(x => x.LocationList.SelectedItem).Where(x => x != null).ToProperty(this, x => x.KioskLocation, out _kioskLocation);

            this.WhenAnyValue(x => x.DatabaseStats)//.Where(x => x.Count > 0).Select(x => x[0].Item2).ToProperty(this, x => x.StudentCount, out _studentCount);
            .Subscribe(x =>
            {
                if (x != null && x.Count > 0)
                {
                    StudentCount = x[0].Item2;
                    ConsequencesCount = x[1].Item2;
                    TimeTableCount = x[2].Item2;
                    LunchRecordCount = x[3].Item2;
                }
            });

            string priorText = String.Empty;
            this.WhenAnyValue(x => x.SelectedTabIndex).Where(x => x > -1).Subscribe(x =>
            {
                //priorText = PopupWindow.ConfirmText;
                if (PopupWindow != null)
                {
                    priorText = "save";

                    if (x > 1 && x != 4)
                    {
                        PopupWindow.ConfirmText = null;
                        PopupWindow.CancelText = "close".ToUpper();
                    }
                    else
                    {
                        PopupWindow.ConfirmText = priorText.ToUpper();
                    }
                }


            });


            //ShowAdvanced = this.WhenAny(x => x.Config, x => x.Value == "Default" || x.Value == "Stats").ToCommand();
            //ShowAdvanced.Subscribe(x => Config = "Advanced");

            //ShowStats = this.WhenAny(x => x.Config, x => x.Value == "Default" || x.Value == "Advanced").ToCommand();
            /*ShowStats.Subscribe(x =>
            {
                Config = "Stats";
                //DatabaseStats.AddRange(storage.DatabaseStats());
            });*/

            SelectPhotoFolder = this.WhenAny(x => x.Config, x => !String.IsNullOrEmpty(x.Value)).ToCommand();
            SelectPhotoFolder.Subscribe(x =>
            {
                var fd = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
                //fd.RootFolder = Settings.Default.ImagesFolder;

                if ((bool)fd.ShowDialog())
                {
                    ImagesFolder = fd.SelectedPath;
                }


            });

            SelectSoundFolder = this.WhenAny(x => x.Config, x => !String.IsNullOrEmpty(x.Value)).ToCommand();
            SelectSoundFolder.Subscribe(x =>
            {
                var fd = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();

                //fd.RootFolder = Settings.Default.ImagesFolder;

                if ((bool)fd.ShowDialog())
                {
                    SoundsFolder = fd.SelectedPath;
                }


            });

            SelectKioskImage = this.WhenAny(x => x.Config, x => !String.IsNullOrEmpty(x.Value)).ToCommand();
            SelectKioskImage.Subscribe(x =>
            {
                var fd = new Ookii.Dialogs.Wpf.VistaOpenFileDialog();
                fd.DefaultExt = "jpg";
                //fd.RootFolder = Settings.Default.ImagesFolder;

                if ((bool)fd.ShowDialog())
                {
                    KioskWelcomeImage = fd.FileName;
                }


            });


            Storage.GetLocations(LocationType.InOut).ObserveOnDispatcher().Subscribe(OnLocationsReturned);
        }

        public SettingsViewModel(LocalStorage storage, IdCardStorage idCardStorage)
        {
        
            IdCardContent = new IdCardDesignerViewModel();
            IdCardStorage = idCardStorage;

            IdCardTemplates = new ReactiveList<CardTemplate>(storage.Cards(Settings.Default.SchoolId));

            var uiContext = TaskScheduler.FromCurrentSynchronizationContext();
            /*
            Task.Run(() =>
            {
                
            });
            */
            DatabaseStats = new ReactiveList<Tuple<string, int>>();
         
            Storage = storage;
            Config = "Default";
            
            Printers = Application.Current.Properties["Printers"] as PrintQueueCollection;
            PassPrintQueue = Application.Current.Properties["PassPrintQueue"] as PrintQueue;
            TempIdPrinterQueue = Application.Current.Properties["TempIdPrintQueue"] as PrintQueue;
            PvcPrinterQueue = Application.Current.Properties["PvcPrintQueue"] as PrintQueue;

            ImagesFolder = Settings.Default.ImagesFolder;
            SoundsFolder = Settings.Default.SoundsFolder;
            SchoolName = Settings.Default.School;
            SchoolId = Settings.Default.SchoolId.ToString(CultureInfo.CurrentCulture);
            AlertStartDate = Settings.Default.AlertStartDate;
            TardyAlertCount = Settings.Default.TardyAlertCount;
            SuppressIdOnPass = Settings.Default.SuppressIdOnPass;
            StartupAttendanceCode = Settings.Default.StartupCode;
            StartupPeriod = Settings.Default.StartupPeriod;
            TakeAttendanceInLocationMode = Settings.Default.TakeAttendance;
            MarkPresentInLocationMode = Settings.Default.MarkPresentInLocationMode;
            IncludeStaff = Settings.Default.IncludeStaff;
            EnableLunchAlerts = Settings.Default.EnableLunchAlerts;
            SelectedPassType = Settings.Default.SelectedPassType;
            AllowStudentKiosk = Settings.Default.StudentKioskEntry;
            AllowVisitorKiosk = Settings.Default.VisitorKioskEntry;
            KioskWelcomeImage = Settings.Default.KioskImagePath;

            PrintScaleFactor = Settings.Default.PrintScaleFactor;
            PrintOffsetX = Settings.Default.PrintOffsetX;
            PrintOffsetY = Settings.Default.PrintOffsetY;

            SqlIp = Settings.Default.SqlMasterIp;
            SelectedDuplexValue = (Duplexing)Settings.Default.DuplexSetting;

            StudentIdTemplate =
                IdCardTemplates.SingleOrDefault(x => x.TemplateName == Settings.Default.StudentIdTemplateName);

            TempIdTemplate =
               IdCardTemplates.SingleOrDefault(x => x.TemplateName == Settings.Default.TempIdTemplateName);

            Host = Environment.MachineName;

            this.WhenAnyValue(x => x.PassPrintQueue).Select(x => x.FullName).ToProperty(this, x => x.PassPrinter, out _selectedPassPrinter);
            this.WhenAnyValue(x => x.TempIdPrinterQueue).Select(x => x.FullName).ToProperty(this, x => x.TempIdPrinter, out _selectedTempIdPrinter);
            this.WhenAnyValue(x => x.PvcPrinterQueue).Select(x => x.FullName).ToProperty(this, x => x.PvcPrinter, out _selectedPvcPrinter);

            //this.WhenAnyValue(x => x.Config).Select(x => x == "Default").ToProperty(this, x => x.DefaultShown, out _showDefault);
            //this.WhenAnyValue(x => x.Config).Select(x => x == "Advanced").ToProperty(this, x => x.AdvancedShown, out _showAdvanced);
            //this.WhenAnyValue(x => x.Config).Select(x => x == "Stats").ToProperty(this, x => x.StatsShown, out _showStats);

            this.WhenAnyValue(x => x.SelectedDuplexValue).Subscribe(x =>
            {
                Settings.Default.DuplexSetting = (int)x;
            });

            this.WhenAnyValue(x => x.TakeAttendanceInLocationMode).Subscribe(x =>
            {
                Settings.Default.TakeAttendance = x;
            });


            this.WhenAnyValue(x => x.AllowStudentKiosk).Subscribe(x =>
            {
                Settings.Default.StudentKioskEntry = x;
            });

            this.WhenAnyValue(x => x.AllowVisitorKiosk).Subscribe(x =>
            {
                Settings.Default.VisitorKioskEntry = x;
            });

            this.WhenAnyValue(x => x.MarkPresentInLocationMode).Subscribe(x =>
            {
                Settings.Default.MarkPresentInLocationMode = x;
            });

            this.WhenAnyValue(x => x.IncludeStaff).Subscribe(x =>
            {
                Settings.Default.IncludeStaff = x;
            });

            this.WhenAnyValue(x => x.EnableLunchAlerts).Subscribe(x =>
            {
                Settings.Default.EnableLunchAlerts = x;
            });

            this.WhenAnyValue(x => x.SuppressIdOnPass).Subscribe(x =>
            {
                Settings.Default.SuppressIdOnPass = x;
            });

            this.WhenAnyValue(x => x.PrintOffsetX).Subscribe(x =>
            {
                Settings.Default.PrintOffsetX = x;
            });

            this.WhenAnyValue(x => x.PrintOffsetY).Subscribe(x =>
            {
                Settings.Default.PrintOffsetY = x;
            });

            this.WhenAnyValue(x => x.PrintScaleFactor).Subscribe(x =>
            {
                Settings.Default.PrintScaleFactor = x;
            });

            this.WhenAnyValue(x => x.SelectedPassType).Subscribe(x =>
            {
                Settings.Default.SelectedPassType = x;
            });


            this.WhenAnyValue(x => x.DatabaseStats)//.Where(x => x.Count > 0).Select(x => x[0].Item2).ToProperty(this, x => x.StudentCount, out _studentCount);
            .Subscribe(x =>
            {
                if (x != null && x.Count > 0)
                {
                    StudentCount = x[0].Item2;
                    ConsequencesCount = x[1].Item2;
                    TimeTableCount = x[2].Item2;
                    AlertCount = x[3].Item2;
                    LunchRecordCount = x[4].Item2;
                }
            });

            string priorText = String.Empty;
            this.WhenAnyValue(x => x.SelectedTabIndex).Where(x=>x > -1).Subscribe(x =>
            {
                //priorText = PopupWindow.ConfirmText;
                if (PopupWindow != null)
                {
                    priorText = "save";

                    if (x > 1 && x != 4)
                    {
                        PopupWindow.ConfirmText = null;
                        PopupWindow.CancelText = "close".ToUpper();
                    }
                    else
                    {
                        PopupWindow.ConfirmText = priorText.ToUpper();
                    }
                }
                

            });


            //ShowAdvanced = this.WhenAny(x => x.Config, x => x.Value == "Default" || x.Value == "Stats").ToCommand();
            //ShowAdvanced.Subscribe(x => Config = "Advanced");

            //ShowStats = this.WhenAny(x => x.Config, x => x.Value == "Default" || x.Value == "Advanced").ToCommand();
            /*ShowStats.Subscribe(x =>
            {
                Config = "Stats";
                //DatabaseStats.AddRange(storage.DatabaseStats());
            });*/

            SelectPhotoFolder = this.WhenAny(x => x.Config, x => !String.IsNullOrEmpty(x.Value)).ToCommand();
            SelectPhotoFolder.Subscribe(x =>
            {
                var fd = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
                //fd.RootFolder = Settings.Default.ImagesFolder;

                if ((bool) fd.ShowDialog())
                {
                    ImagesFolder = fd.SelectedPath;
                }
               

            });

            SelectSoundFolder = this.WhenAny(x => x.Config, x => !String.IsNullOrEmpty(x.Value)).ToCommand();
            SelectSoundFolder.Subscribe(x =>
            {
                var fd = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
                
                //fd.RootFolder = Settings.Default.ImagesFolder;

                if ((bool)fd.ShowDialog())
                {
                    SoundsFolder = fd.SelectedPath;
                }
               

            });

            AddPersonCommand = this.WhenAny(x => x.SelectedTabIndex, x => x.Value > -1).ToCommand();
            AddPersonCommand.Subscribe(_ =>
            {

                MessageBus.Current.SendMessage(new Tuple<DialogConstants>(DialogConstants.AddPersonViewModel));

            });


            //ShowDefault = this.WhenAny(x => x.Config, x => x.Value == "Stats" || x.Value == "Advanced").ToCommand();
            //ShowDefault.Subscribe(x => Config = "Default");

        }

        private PrintQueue _passPrintQueue;

        public PrintQueue PassPrintQueue
        {
            get { return _passPrintQueue; }
            set { this.RaiseAndSetIfChanged(ref _passPrintQueue, value); }
        }

        private int _tabIndex = 0;

        public int SelectedTabIndex
        {
            get { return _tabIndex; }
            set { this.RaiseAndSetIfChanged(ref _tabIndex, value); }
        }


        private PrintQueue _tempIdPrinterQueue;

        public PrintQueue TempIdPrinterQueue
        {
            get { return _tempIdPrinterQueue; }
            set { this.RaiseAndSetIfChanged(ref _tempIdPrinterQueue, value); }
        }



        private PrintQueue _pvcPrinterQueue;

        public PrintQueue PvcPrinterQueue
        {
            get { return _pvcPrinterQueue; }
            set { this.RaiseAndSetIfChanged(ref _pvcPrinterQueue, value); }
        }


        private string _schoolId;

        public string SchoolId
        {
            get { return _schoolId; }
            set { this.RaiseAndSetIfChanged(ref _schoolId, value); }
        }

        private string _schoolName;

        public string SchoolName
        {
            get { return _schoolName; }
            set { this.RaiseAndSetIfChanged(ref _schoolName, value); }
        }

        private string _startupPeriod;

        public string StartupPeriod
        {
            get { return _startupPeriod; }
            set { this.RaiseAndSetIfChanged(ref _startupPeriod, value); }
        }

        private string _imagesFolder;

        public string ImagesFolder
        {
            get { return _imagesFolder; }
            set { this.RaiseAndSetIfChanged(ref _imagesFolder, value); }
        }

        private string _soundsFolder;

        public string SoundsFolder
        {
            get { return _soundsFolder; }
            set { this.RaiseAndSetIfChanged(ref _soundsFolder, value); }
        }

        private string _kioskWelcomeImage;

        public string KioskWelcomeImage
        {
            get { return _kioskWelcomeImage; }
            set { this.RaiseAndSetIfChanged(ref _kioskWelcomeImage, value); }
        }

        private string _startupAttendanceCode;

        public string StartupAttendanceCode
        {
            get { return _startupAttendanceCode; }
            set { this.RaiseAndSetIfChanged(ref _startupAttendanceCode, value); }
        }

        private DateTime _alertStartDate;

        public DateTime AlertStartDate
        {
            get { return _alertStartDate; }
            set { this.RaiseAndSetIfChanged(ref _alertStartDate, value); }
        }

        private int _tardyAlertCount;

        public int TardyAlertCount
        {
            get { return _tardyAlertCount; }
            set { this.RaiseAndSetIfChanged(ref _tardyAlertCount, value); }
        }

        private PrintQueueCollection _printers;

        public PrintQueueCollection Printers
        {
            get { return _printers; }
            set { this.RaiseAndSetIfChanged(ref _printers, value); }
        }

        public ReactiveCommand<object> ShowKiosk { get; private set; }

        public ReactiveCommand<object> ShowAdvanced { get; private set; }

        public ReactiveCommand<object> ShowDefault { get; private set; }

        public ReactiveCommand<object> ShowStats { get; private set; }

        public ReactiveCommand<object> SelectPhotoFolder { get; private set; }

        public ReactiveCommand<object> SelectSoundFolder { get; private set; }

        public ReactiveCommand<object> SelectKioskImage { get; private set; }

        private bool _allowKioskSearchName;
        public bool AllowKioskSearchName
        {
            get { return _allowKioskSearchName; }
            set { this.RaiseAndSetIfChanged(ref _allowKioskSearchName, value); }
        }

        private bool _allowKioskLocation;
        public bool AllowKioskLocation
        {
            get { return _allowKioskLocation; }
            set { this.RaiseAndSetIfChanged(ref _allowKioskLocation, value); }
        }

        private bool _allowKioskTardyPass;
        public bool AllowKioskTardyPass
        {
            get { return _allowKioskTardyPass; }
            set { this.RaiseAndSetIfChanged(ref _allowKioskTardyPass, value); }
        }

        private bool _allowKioskDismissalPass;
        public bool AllowKioskDismissalPass
        {
            get { return _allowKioskDismissalPass; }
            set { this.RaiseAndSetIfChanged(ref _allowKioskDismissalPass, value); }
        }

        private bool _allowKioskLocationPass;
        public bool AllowKioskLocationPass
        {
            get { return _allowKioskLocationPass; }
            set { this.RaiseAndSetIfChanged(ref _allowKioskLocationPass, value); }
        }

        private ObservableAsPropertyHelper<string> _kioskLocation;
        public string KioskLocation
        {
            get { return _kioskLocation != null ? _kioskLocation.Value : Settings.Default.KioskLocation; }
        }

        /*
        public IScreen HostScreen
        {
            get { throw new NotImplementedException(); }
        }

        public string UrlPathSegment
        {
            get { throw new NotImplementedException(); }
        }*/
    }
}
