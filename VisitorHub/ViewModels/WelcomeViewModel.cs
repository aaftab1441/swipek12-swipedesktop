using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Printing;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Autofac;
using Common;
using log4net;
using ReactiveUI;
using SwipeDesktop.Api;
using SwipeDesktop.Common;
using SwipeDesktop.Interfaces;
using SwipeDesktop.Modal;

namespace SwipeDesktop.ViewModels
{
    public class WelcomeViewModel : ReactiveObject, IHostedViewModel
    {
        private LocalStorage LocalDatabase;

        public WelcomeViewModel()
        {
            InitDialog();
            LocalDatabase = App.Container.Resolve<LocalStorage>();

            StaffModeSettings = Settings.Default.IncludeStaff;
            ModeSettings = Settings.Default.StudentKioskEntry;
            VisitorModeSettings = Settings.Default.VisitorKioskEntry;
            SchoolName = Settings.Default.School;
            StudentLocationModeSettings = Settings.Default.AllowKioskLocation;

            Locations = new ReactiveList<ScanLocation>();

            TransitionToVisitorExit = this.WhenAny(x => x.SchoolName, x => !String.IsNullOrWhiteSpace(x.Value)).ToCommand();

            this.WhenAnyObservable(x => x.TransitionToVisitorExit).Subscribe(x =>
            {
                Main.CurrentView = new VisitorExitViewModel(Main);
            });

            TransitionToVisitorEntry = this.WhenAny(x => x.SchoolName, x => !String.IsNullOrWhiteSpace(x.Value)).ToCommand();

            this.WhenAnyObservable(x => x.TransitionToVisitorEntry).Subscribe(x =>
            {
                Main.CurrentView = new VisitorEntryViewModel(Main);
            });

            TransitionToStudentExit = this.WhenAny(x => x.SchoolName, x => !String.IsNullOrWhiteSpace(x.Value)).ToCommand();

            this.WhenAnyObservable(x => x.TransitionToStudentExit).Subscribe(x =>
            {
                var vm = new StudentExitViewModel(Main);
               
                Main.CurrentView = vm;
            });


            TransitionToStaffExit = this.WhenAny(x => x.SchoolName, x => !String.IsNullOrWhiteSpace(x.Value)).ToCommand();

            this.WhenAnyObservable(x => x.TransitionToStaffExit).Subscribe(x =>
            {
                var vm = new StaffExitViewModel(Main);

                Main.CurrentView = vm;
            });

            TransitionToStudentEntry = this.WhenAny(x => x.SchoolName, x => !String.IsNullOrWhiteSpace(x.Value)).ToCommand();

            this.WhenAnyObservable(x => x.TransitionToStudentEntry).Subscribe(x =>
            {
                var vm = new StudentEnterViewModel(Main);
               
                Main.CurrentView = vm;
            });


            TransitionToStaffEntry = this.WhenAny(x => x.SchoolName, x => !String.IsNullOrWhiteSpace(x.Value)).ToCommand();

            this.WhenAnyObservable(x => x.TransitionToStaffEntry).Subscribe(x =>
            {
                var vm = new StaffEnterViewModel(Main);

                Main.CurrentView = vm;
            });


            TransitionToLocationExit = this.WhenAny(x => x.SchoolName, x => !String.IsNullOrWhiteSpace(x.Value)).ToCommand();

            this.WhenAnyObservable(x => x.TransitionToLocationExit).Subscribe(x =>
            {
                var vm = new LocationExitViewModel(Main);
                
                Main.CurrentView = vm;
            });

            TransitionToLocationEntry = this.WhenAny(x => x.SchoolName, x => !String.IsNullOrWhiteSpace(x.Value)).ToCommand();

            this.WhenAnyObservable(x => x.TransitionToLocationEntry).Subscribe(x =>
            {
                var vm = new LocationEnterViewModel(Main);
                
                Main.CurrentView = vm;
            });

            KeyCommand = this.WhenAny(x => x.SchoolName, x => !String.IsNullOrWhiteSpace(x.Value)).ToCommand();

            this.WhenAnyObservable(x => x.KeyCommand).Subscribe(x =>
            {
                ShowPopup = true;
            });

            LocalDatabase.GetLocations(LocationType.InOut).ObserveOnDispatcher().Subscribe(OnLocationsReturned);
        }

        public IPopupViewModelLocator DialogService { get; private set; }

        private PopupViewModel _currentModal;

        public PopupViewModel CurrentDialogViewModel
        {
            get { return _currentModal; }
            set { this.RaiseAndSetIfChanged(ref _currentModal, value); }
        }
        void InitDialog()
        {
            CurrentDialogViewModel = new PopupViewModel();
            CurrentDialogViewModel.ConfirmText = string.Empty;
            CurrentDialogViewModel.CancelText = string.Empty;
            CurrentDialogViewModel.ShowPopup = false;
        }


        public ReactiveList<ScanLocation> Locations { get; set; }

        private void OnLocationsReturned(IEnumerable<ScanLocation> locations)
        {
            Locations.Clear();
            Locations.AddRange(locations);

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
                  
                    LocalStorage.EnsureSchoolRecordExists(schoolId);

                    school = LocalDatabase.GetSchoolSettings(schoolId);

                    var version = Assembly.GetEntryAssembly().GetName().Version.ToString();

                    if (school == null || string.IsNullOrEmpty(school.SchoolName))
                    {

                        var t3 = Task.Run(() => DataReplicator.InitRemoteServer());

                        Task.WhenAll(new[] { t3 }).ContinueWith((c) =>
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

                                    Task.Run(() => DataReplicator.RemoteSnapshot());
                                }

                            }, uiContext);
                        }, uiContext);
                    }
                    else
                    {
                        Settings.Default.School = school.SchoolName;
                        var t3 = Task.Run(() => DataReplicator.InitRemoteServer());

                        Task.WhenAll(new[] { t3 }).ContinueWith((c) =>
                        {

                            Task.Run(() => DataReplicator.RemoteSnapshot());

                        }, uiContext);
#if DEBUG
                        version = version + ".D";
#else
                            version = version + ".R";
#endif
                    }


                    if (screen != null)
                        screen.Title = string.Format("{0} (Build {1})", Settings.Default.School, version);

                    Settings.Default.AlertStartDate = settings.AlertStartDate;
                    Settings.Default.TardyAlertCount = settings.TardyAlertCount;
                    Settings.Default.StartupCode = settings.StartupAttendanceCode;
                    Settings.Default.StartupPeriod = settings.StartupPeriod;
                    Settings.Default.PassPrinter = settings.PassPrinter;
                    Settings.Default.PvcPrinter = settings.PvcPrinter;
                    Settings.Default.TempIdPrinter = settings.TempIdPrinter;
                    Settings.Default.ImagesFolder = settings.ImagesFolder;
                    Settings.Default.SoundsFolder = settings.SoundsFolder;
                    Settings.Default.SqlMasterIp = settings.SqlIp;

                    Settings.Default.SelectedPassType = settings.SelectedPassType;
                    Settings.Default.VisitorKioskEntry = settings.AllowVisitorKiosk;
                    Settings.Default.StudentKioskEntry = settings.AllowStudentKiosk;

                    Settings.Default.IncludeStaff = settings.IncludeStaff;
                    Settings.Default.EnableLunchAlerts = settings.EnableLunchAlerts;
                    Settings.Default.AllowKioskDismissalPass = settings.AllowKioskDismissalPass;
                    Settings.Default.AllowKioskLocation = settings.AllowKioskLocation;
                    Settings.Default.AllowKioskLocationPass = settings.AllowKioskLocationPass;
                    Settings.Default.AllowKioskSearchName = settings.AllowKioskSearchName;
                    Settings.Default.AllowKioskTardyPass = settings.AllowKioskTardyPass;
                    Settings.Default.KioskLocation = settings.KioskLocation;

                    if (Settings.Default.KioskImagePath != settings.KioskWelcomeImage)
                    {
                        MessageBus.Current.SendMessage(new Tuple<string, string>(MessageEvents.WelcomeImageChanged, settings.KioskWelcomeImage));
                    }

                    Settings.Default.KioskImagePath = settings.KioskWelcomeImage;
                    /*
                    Settings.Default.PrintScaleFactor = settings.PrintScaleFactor;
                    Settings.Default.PrintOffsetX = settings.PrintOffsetX;
                    Settings.Default.PrintOffsetY = settings.PrintOffsetY;

                    if (settings.TempIdTemplate != null)
                    {
                        Settings.Default.TempIdTemplateName = settings.TempIdTemplate.TemplateName;
                    }

                    if (settings.StudentIdTemplate != null)
                    {
                        Settings.Default.StudentIdTemplateName = settings.StudentIdTemplate.TemplateName;
                    }
                    */
                    try
                    {
                        Application.Current.Properties["PassPrintQueue"] = ((PrintQueueCollection)Application.Current.Properties["Printers"]).FirstOrDefault(x => x.Name.Contains(Settings.Default.PassPrinter));
                    }catch(Exception ex)
                    {
                      Logger.Error("Could not set Pass Printer ", ex);   
                    }

                    try
                    {
                        Application.Current.Properties["PvcPrintQueue"] = ((PrintQueueCollection)Application.Current.Properties["Printers"]).FirstOrDefault(x => x.Name.Contains(Settings.Default.PvcPrinter));
                    }
                    catch (Exception ex) { }

                    try
                    {
                        Application.Current.Properties["TempIdPrintQueue"] = ((PrintQueueCollection)Application.Current.Properties["Printers"]).FirstOrDefault(x => x.Name.Contains(Settings.Default.TempIdPrinter));
                    }catch(Exception ex) { Logger.Error("Could not set Visitor Id Printer ", ex); }

                    Settings.Default.Save();
                    InButtonText = Settings.Default.KioskLocation;
                    OutButtonText = Settings.Default.KioskLocation;
                    screen.UpdateLayout();
                }
            }
        }

        public void RaiseSettingsPopup()
        {
          
            CurrentDialogViewModel.CurrentContent = DialogService.LocateDialog(DialogConstants.VisitorSettingsDialog.ToString());

            //CurrentDialogViewModel.ConfirmPopupCommand = new DelegateCommand(() => SendMessage(MessageTypes.ConfirmSettings, new NotificationEventArgs(string.Empty)));
            CurrentDialogViewModel.HideAction = (x) =>
            {
                //CancelSettings(x);
                CurrentDialogViewModel.ShowPopup = false;


                MessageBus.Current.SendMessage(new Tuple<string>("SettingsClosed"));
            };

            CurrentDialogViewModel.SaveAction = (o) => {
                //todo: save settings
                SaveSettings(o);
                SchoolName = Settings.Default.School;
                ModeSettings = Settings.Default.StudentKioskEntry;
                VisitorModeSettings = Settings.Default.VisitorKioskEntry;
                StudentLocationModeSettings = Settings.Default.AllowKioskLocation;
                StaffModeSettings = Settings.Default.IncludeStaff;
                CurrentDialogViewModel.ShowPopup = false;
            };

            var settings = CurrentDialogViewModel.CurrentContent as SettingsViewModel;

            if (settings != null)
            {
                CurrentDialogViewModel.ShowPopup = true;
                settings.HideAdvancedTabs = true;
                settings.SelectedTabIndex = 0;
                settings.PopupWindow = CurrentDialogViewModel;
                settings.SchoolName = Settings.Default.School;
                settings.AllowStudentKiosk = Settings.Default.StudentKioskEntry;
                settings.AllowVisitorKiosk = Settings.Default.VisitorKioskEntry;
                settings.KioskWelcomeImage = Settings.Default.KioskImagePath;
                settings.IncludeStaff = Settings.Default.IncludeStaff;
                settings.EnableLunchAlerts = Settings.Default.EnableLunchAlerts;
                settings.AllowKioskDismissalPass = Settings.Default.AllowKioskDismissalPass;
                settings.AllowKioskLocation = Settings.Default.AllowKioskLocation;
                settings.AllowKioskLocationPass = Settings.Default.AllowKioskLocationPass;
                settings.AllowKioskSearchName = Settings.Default.AllowKioskSearchName;
                settings.AllowKioskTardyPass = Settings.Default.AllowKioskTardyPass;

                var list = Locations.Select(s => s.RoomName).Distinct().ToArray();
                settings.LocationList.ItemsSource.Clear();
                settings.LocationList.ItemsSource.AddRange(list);

                settings.LocationList.SelectedItem = !string.IsNullOrEmpty(Settings.Default.KioskLocation) ? Settings.Default.KioskLocation : list.FirstOrDefault();

                if (!list.Contains(settings.LocationList.SelectedItem))
                {
                    settings.LocationList.SelectedItem = list.FirstOrDefault();
                }



            }
            CurrentDialogViewModel.Title = "Settings";

            CalcViewSize(CurrentDialogViewModel);

            CurrentDialogViewModel.VerticalOffset = -30;
            //CurrentDialogViewModel.HorizontalOffset = 505;
            CurrentDialogViewModel.Placement = PlacementMode.Bottom;
            CurrentDialogViewModel.ConfirmText = "save".ToUpper();
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

        private LocalStorage Storage;

        public WelcomeViewModel(MainViewModel main) : this()
        {
            Storage = App.Container.Resolve<LocalStorage>();
            _main = main;
            DialogService = main.DialogService;
        }

        MainViewModel _main;
        public MainViewModel Main
        {
            get { return _main; }
            //set { this.RaiseAndSetIfChanged(ref f_name, value); }
        }


        bool _isProcessing;
        public bool IsProcessing
        {
            get { return _isProcessing; }
            set { this.RaiseAndSetIfChanged(ref _isProcessing, value); }
        }

        bool _showPopup;
        public bool ShowPopup
        {
            get { return _showPopup; }
            set { this.RaiseAndSetIfChanged(ref _showPopup, value); }
        }


        bool _modeSettings;
        public bool ModeSettings
        {
            get { return _modeSettings; }
            set { this.RaiseAndSetIfChanged(ref _modeSettings, value); }
        }



        bool _staffModeSettings;
        public bool StaffModeSettings
        {
            get { return _staffModeSettings; }
            set { this.RaiseAndSetIfChanged(ref _staffModeSettings, value); }
        }


        bool _modeVisitorSettings;
        public bool VisitorModeSettings
        {
            get { return _modeVisitorSettings; }
            set { this.RaiseAndSetIfChanged(ref _modeVisitorSettings, value); }
        }


        bool _modeStudentLocationSettings;
        public bool StudentLocationModeSettings
        {
            get { return _modeStudentLocationSettings; }
            set { this.RaiseAndSetIfChanged(ref _modeStudentLocationSettings, value); }
        }

        string _schoolName;
        public string SchoolName
        {
            get { return _schoolName; }
            set { this.RaiseAndSetIfChanged(ref _schoolName, value); }
        }

        string _displayText = "Place your license in the scanner to begin.";
        public string DisplayText
        {
            get { return _displayText; }
            set { this.RaiseAndSetIfChanged(ref _displayText, value); }
        }



        string _inButtonText = "{0} IN";
        public string InButtonText
        {
            get { 
                return string.Format("{0} IN", Settings.Default.KioskLocation); 
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _inButtonText, string.Format("{0} IN", value));
            }
        }

        string _outButtonText = "{0} OUT";
        public string OutButtonText
        {
            get { 
                return string.Format("{0} OUT", Settings.Default.KioskLocation); 
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _outButtonText, string.Format("{0} OUT", value));
            }
        }

        public ReactiveCommand<object> TransitionToVisitorExit { get; private set; }

        public ReactiveCommand<object> TransitionToStaffEntry { get; private set; }
        public ReactiveCommand<object> TransitionToStaffExit { get; private set; }

        public ReactiveCommand<object> TransitionToVisitorEntry { get; private set; }

        public ReactiveCommand<object> TransitionToStudentExit { get; private set; }
        public ReactiveCommand<object> TransitionToStudentEntry { get; private set; }

        public ReactiveCommand<object> TransitionToLocationExit { get; private set; }
        public ReactiveCommand<object> TransitionToLocationEntry { get; private set; }

        public ReactiveCommand<object> KeyCommand { get; private set; }
        
        private static readonly ILog Logger = LogManager.GetLogger(typeof(WelcomeViewModel));

    }


}
