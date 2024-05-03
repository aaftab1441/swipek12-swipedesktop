using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

using Autofac;
using Commands;
using Common;
using log4net;
using MahApps.Metro.Controls;
using ModernWPF.Win8TouchKeyboard.Desktop;
using Newtonsoft.Json;
using ReactiveUI;
using ServiceLayer.Client;
using ServiceLayer.Common;
using ServiceStack.ServiceClient.Web;
using Simple.Data;
using SwipeDesktop.Api;
using SwipeDesktop.Common;
using SwipeDesktop.Config;
using SwipeDesktop.Interop;
using SwipeDesktop.Modal;
using SwipeDesktop.Models;
using SwipeDesktop.Storage;
using SwipeDesktop.ViewModels;
using SwipeDesktop.Views;
using SwipeK12.NextGen.Messaging;
using SwipeK12.NextGen.ReadServices.Messages;
using SwipeDesktop.Common;

namespace SwipeDesktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(MainWindow));
     
        private static readonly bool SyncEnabled = !bool.Parse(ConfigurationManager.AppSettings["holdRecordsInQueue"]);
        private static readonly StationMode StationMode = (StationMode)Enum.Parse(typeof(StationMode), ConfigurationManager.AppSettings["mode"]);

        public RemoteStorage RemoteStorage { get; private set; }

        public LocalStorage LocalStorage { get; private set; }
        public InOutStorage LocationStorage { get; private set; }

        public StaffScanStorage StaffScanStorage { get; private set; }

        public VisitStorage VisitStorage { get; private set; }

        private readonly DispatcherTimer _timer;

        public IRequestDispatcher RequestDispatcher { get; private set; }

        public Bootstrapper AppBootstrapper { get; protected set; }

        private static readonly string StationModeString = ConfigurationManager.AppSettings["mode"];

        public MainWindow()
        {
            InitializeComponent();

            if (Application.Current.Properties["faulted"] != null)
                return;

            var version = Assembly.GetEntryAssembly().GetName().Version.ToString();

            #if DEBUG
                version = version + ".D";
            #else
             version = version + ".R";
            #endif

            //Windowtit = Application.Current.Resources["SwipeOrangeColorBrush"] as SolidColorBrush;

            _timer = new DispatcherTimer(DispatcherPriority.Background, Application.Current.Dispatcher);
            _timer.Interval = TimeSpan.FromMinutes(1);
            
            //this.Events().KeyUpObs.Where(x => x.EventArgs.Key == Key.Escape).InvokeCommand(this, x => x.ViewModel.Close);
            Loaded += OnLoaded;
            this.Closed += (s, e) =>
            {
                var r = Process.GetProcessesByName("redis-server").GetProcess();
                r.Kill();

                App.Current.Shutdown();
                Process.GetCurrentProcess().Kill();
            };

            //if (!DesignerProperties.GetIsInDesignMode(this)) 
            //{ 
            //MainGrid.DataContext = new MainViewModel(){}; 
            //}
            InkInputHelper.DisableWPFTabletSupport();

             AppBootstrapper = new Bootstrapper();
             DataContext = AppBootstrapper;

             App.Container = AppBootstrapper.GetContainer();

             //Thread.Sleep(5000);
             var modeEnum = (StationMode)Enum.Parse(typeof(StationMode), StationModeString);

             switch (modeEnum)
             {
                 case StationMode.Station:
                    var vm = new ScanStationViewModel(AppBootstrapper, App.Container.Resolve<ScanStorage>(), App.Container.Resolve<StaffScanStorage>(), App.Container.Resolve<IPopupViewModelLocator>(), App.Container.Resolve<RemoteStorage>(), App.Container.Resolve<LocalStorage>(), App.Container.Resolve<DetentionStorage>(), App.Container.Resolve<InOutStorage>(), App.Container.Resolve<DismissalStorage>(), App.Container.Resolve<FineStorage>(), App.Container.Resolve<IdCardStorage>(), App.Container.Resolve<AlertPrintedStorage>());

                    try
                    {
                        var sockerServer = new SocketServer(vm);
                        sockerServer.StartListening();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Could not start listening for network scans.",ex);
                    }

                    AppBootstrapper.Router.Navigate.Execute(vm);
                     break;
                 case StationMode.VisitorKiosk:
                     AppBootstrapper.Router.Navigate.Execute(new MainViewModel(AppBootstrapper, App.Container.Resolve<IPopupViewModelLocator>()));
                     break;
             }

             RequestDispatcher = App.Container.Resolve<IRequestDispatcher>();

             if (StationMode == StationMode.VisitorKiosk)
                BootstrapVisitorKiosk();


            if (StationMode == StationMode.Station)
            {
              
                WindowState = WindowState.Maximized;
                //ResizeMode = ResizeMode.CanMinimize;
            }

            RemoteStorage = App.Container.Resolve<RemoteStorage>();
            LocalStorage = App.Container.Resolve<LocalStorage>();
          
            if (SyncEnabled)
                 _timer.Start();

        }

        private void OnVisitorLocationsReturned(IEnumerable<VisitorLocation> locations)
        {
            Application.Current.Properties["Locations"] = locations.Select(x => x.Location).ToArray();
        }


        private void OnDismissalLocationsReturned(IEnumerable<ScanLocation> locations)
        {
            Application.Current.Properties["DismissalLocation"] = locations.FirstOrDefault();

        }

        public bool CheckInternet()
        {
            string status;
            var connected = InternetAvailability.IsInternetAvailable(out status);

            return connected;
        }

        public async Task<bool> ApiIsNotAvailable()
        {
            string status;
            var connected = await InternetAvailability.ApiIsNotAvailable(SwipeDesktop.Settings.Default.JsonUrl);

            return connected;
        }
        async Task<bool> Sync()
        {
            if (!CheckInternet())
            {
                Logger.ErrorFormat("Network connection not detected, skipping swipe sync at {0}.", DateTime.Now);
                return false;
            }

            if (await ApiIsNotAvailable())
            {
                Logger.ErrorFormat("API not available, skipping swipe sync at {0}.", DateTime.Now);
                return false;
            }


            var db = Database.OpenNamedConnection("ScanStation");

            dynamic data = db.RedisScans.FindAll(db.RedisScans.SyncTime == null);

            foreach (dynamic scan in data)
            {
                var processed = false;
                var id = scan.Id;
                //var scan = db.RedisScans.FindById(id);

                var type = scan.ObjectType;
                try
                {
                    if (type == typeof(LocationScan).ToString())
                    {
                        var json = JsonConvert.DeserializeObject<LocationScan>(scan.ObjectJson);
                        RemoteStorage.SendLocationScan(json);
                        processed = true;
                    }
                    if (type == typeof(ScanModel).ToString())
                    {
                        var json = JsonConvert.DeserializeObject<ScanModel>(scan.ObjectJson);

                        json.Id = id;
                        RemoteStorage.SendScan(json);
                        processed = true;
                    }

                    if (type == typeof(Consequence).ToString())
                    {
                        var json = JsonConvert.DeserializeObject<Consequence>(scan.ObjectJson);

                        json.Id = id;
                        RemoteStorage.SendConsequence(json);
                        processed = true;
                    }

                    if (type == typeof(Dismissal).ToString())
                    {
                        var json = JsonConvert.DeserializeObject<Dismissal>(scan.ObjectJson);
                        json.Id = id;
                        RemoteStorage.SendDismissal(json);
                        processed = true;
                    }


                    if (type == typeof(AssessedFine).ToString())
                    {
                        var fine = JsonConvert.DeserializeObject<AssessedFine>(scan.ObjectJson);

                        fine.Id = id;
                        RemoteStorage.SendFine(fine);
                        processed = true;
                    }

                    if (type == typeof(VisitModel).ToString())
                    {
                        var vm = JsonConvert.DeserializeObject<VisitModel>(scan.ObjectJson);

                        vm.Id = id;
                        processed = false;
                    }

                    if (type == typeof(VisitExit).ToString())
                    {
                        var vm = JsonConvert.DeserializeObject<VisitExit>(scan.ObjectJson);

                        vm.Id = id;
                        processed = false;
                    }

                    if (type == typeof(NewIdCard).ToString())
                    {
                        var card = JsonConvert.DeserializeObject<NewIdCard>(scan.ObjectJson);

                        card.Id = id;
                        RemoteStorage.SendIdCardPrinted(card);
                        processed = true;
                    }

                    if (type == typeof(AlertPrinted).ToString())
                    {
                        var json = JsonConvert.DeserializeObject<AlertPrinted>(scan.ObjectJson);


                        json.Id = id;

                        json.StudentImage = null;
                        RemoteStorage.SendAlertPrinted(json);
                        processed = true;
                    }

                    if (type == typeof(Models.StaffRecord).ToString())
                    {
                        var json = JsonConvert.DeserializeObject<Models.StaffRecord>(scan.ObjectJson);

                        json.Id = id;
                        RemoteStorage.SendStaffScanAsync(json);
                        processed = true;
                    }

                    if (processed)
                    {
                        scan.SyncTime = DateTime.Now;
                        db.RedisScans.UpdateById(scan);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("An error occurred uploading data.", ex);
                }

            }

            return true;
        }
      
        void SyncDismissal()
        {
            var db = Database.OpenNamedConnection("ScanStation");

            dynamic data = db.RedisScans.FindAll(db.RedisScans.SyncTime == null && db.RedisScans.ObjectType == "SwipeDesktop.Models.Dismissal");

            foreach (dynamic scan in data)
            {
                var id = scan.Id;

                try
                {

                    var json = JsonConvert.DeserializeObject<Dismissal>(scan.ObjectJson);
                    json.Id = id;

                    RemoteStorage.SendDismissal(json);

                    scan.SyncTime = DateTime.Now;
                    db.RedisScans.UpdateById(scan);
                }
                catch (Exception ex)
                {
                    Logger.Error("An error occurred uploading data.", ex);
                }

            }

        }

        void BootstrapVisitorKiosk()
        {
            PrintQueue queue = null;
            PrintQueue queue2 = null;

            this.SourceInitialized += Window1_SourceInitialized;
            WindowState = WindowState.Maximized;

            _timer.Tick += timer_VisitorTick;

            /*
            var dymos = ((PrintQueueCollection)Application.Current.Properties["Printers"]).Where(x => x.Name.ToLower().Contains("dymo"));

            foreach (var dymo in dymos)
            {
                var status = dymo.QueueStatus;

                if (status != PrintQueueStatus.Offline && status != PrintQueueStatus.NotAvailable && status != PrintQueueStatus.Error)
                    queue = dymo;
            }*/

            Application.Current.Properties["TempIdPrintQueue"] = ((PrintQueueCollection)Application.Current.Properties["Printers"]).FirstOrDefault(x => x.Name.Contains(Settings.Default.TempIdPrinter)); //Settings.Default.TempIdPrinter

            /*
           var stars = ((PrintQueueCollection)Application.Current.Properties["Printers"]).Where(x => x.Name.ToLower().Contains("star"));

            foreach (var star in stars)
            {
                var status = star.QueueStatus;

                if (status != PrintQueueStatus.Offline && status != PrintQueueStatus.NotAvailable && status != PrintQueueStatus.Error)
                    queue2 = star;
            }*/

            Application.Current.Properties["PassPrintQueue"] = ((PrintQueueCollection)Application.Current.Properties["Printers"]).FirstOrDefault(x => x.Name.Contains(Settings.Default.PassPrinter)); //Settings.Default.PassPrinter

            RemoteStorage = App.Container.Resolve<RemoteStorage>();
            VisitStorage = App.Container.Resolve<VisitStorage>();
            LocationStorage = App.Container.Resolve<InOutStorage>();
            StaffScanStorage = App.Container.Resolve<StaffScanStorage>();

            RemoteStorage.GetLocations(LocationType.Release).ObserveOnDispatcher().Subscribe(OnDismissalLocationsReturned);
            RemoteStorage.GetVisitorLocations().ObserveOnDispatcher().Subscribe(OnVisitorLocationsReturned);

            Observable.Interval(TimeSpan.FromSeconds(60), Scheduler.Default)
                .ObserveOnDispatcher(DispatcherPriority.Background)
                .Subscribe(async x =>
                {
                    await Sync();
                });

            this.Events().MouseRightButtonDown.Subscribe(x =>
            {
                var main = (DataContext as Bootstrapper).Router.NavigationStack[0] as MainViewModel;
                if (main == null)
                    return;

                var vm = (main.CurrentView) as WelcomeViewModel;
                if (vm == null)
                    return;

                vm.RaiseSettingsPopup();
            });

            this.Events().KeyDown.Where(k => k.Key == Key.F1).Subscribe(x =>
            {
                var main = (DataContext as Bootstrapper).Router.NavigationStack[0] as MainViewModel;
                var vm = (main.CurrentView) as WelcomeViewModel;

                if (vm == null)
                    return;

                vm.RaiseSettingsPopup();
            });

            this.Events().KeyDown.Where(k => k.Key == Key.Enter).Subscribe(async x =>
            {
                var main = (DataContext as Bootstrapper).Router.NavigationStack[0] as MainViewModel;

                if (main != null && !main.ScannerIsConnected && main.CurrentView.GetType() == typeof(VisitorEntryViewModel))
                {
                    var vm = main.CurrentView as VisitorEntryViewModel;
                    //main.CurrentView = new ProcessingViewModel(main);
                    if (string.IsNullOrEmpty(vm.VisitorId))
                        return;

                    var speedPass = await LocalStorage.SearchForSpeedpassAsync(vm.VisitorId);

                    if (speedPass == null)
                        return;

                    var source = (BitmapImage)this.Resources["NullPerson"];

                    var scanVM = new VisitorScanViewModel(main, VisitStorage, RemoteStorage, LocalStorage)
                    {
                        FirstName = speedPass.FirstName,
                        LastName = speedPass.LastName,
                        //Street1 = "100 Main Street",
                        //City = "Balitmore",
                        //State = "MD",
                        //Zip = "21015",
                        Identification = speedPass.PassId,
                        School = Settings.Default.SchoolId,
                        VisitEntryDate = DateTime.Now,
                        BitmapSource = source
                    };

                    if (speedPass.DateOfBirth > DateTime.MinValue)
                    {
                        scanVM.DateOfBirth = speedPass.DateOfBirth;
                    }

                    if (speedPass.Image != null)
                    {
                        //todo locad image from bytes
                        //scanVM.BitmapSource = speedPass.Image.LoadImage();
                    }

                    main.CurrentView = scanVM;

                }

            });

            /*
            this.Events().KeyDown.Where(k => k.Key == Key.S).Subscribe(x =>
            {
                var main = (DataContext as Bootstrapper).Router.NavigationStack[0] as MainViewModel;

                if (main != null && !main.ScannerIsConnected && main.CurrentView.GetType() == typeof(VisitorEntryViewModel))
                {
                    main.CurrentView = new ProcessingViewModel(main);

                    main.CurrentView =
                         new VisitorScanViewModel(main, VisitStorage, RemoteStorage, LocalStorage)
                         {
                             FirstName = "James",
                             LastName = "Madison",
                             Street1 = "100 Main Street",
                             City = "Balitmore",
                             State = "MD",
                             Zip = "21015",
                             DateOfBirth = DateTime.Parse("1/1/1986"),
                             Identification = "C-000-000-000-000",
                             School = Settings.Default.SchoolId,
                             VisitLocation = "Administrator Office"
                         };
                   
                }
            });
            */

            this.ShowTitleBar = false;
            ShowCloseButton = false;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.None;
            ShowTitleBar = false;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var brush = (Brush)FindResource("SwipeOrangeColorBrush");

            var rect = GetTemplateChild("PART_WindowTitleBackground") as Rectangle;
            //rect.Fill = brush;

            //foreach (var child in grid.Children.OfType<TextBlock>())
            //{
            //    var brush = new SolidColorBrush() { Color = (Color)ColorConverter.ConvertFromString("#f16623") };
            //    (child as TextBlock).Foreground = brush;
            //}
        }

        private void Window1_SourceInitialized(object sender, EventArgs e)
        {
            var helper = new WindowInteropHelper(this);
            HwndSource source = HwndSource.FromHwnd(helper.Handle);
            source.AddHook(WndProc);
        }

        const int WM_SYSCOMMAND = 0x0112;
        const int SC_MOVE = 0xF010;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {

            switch (msg)
            {
                case WM_SYSCOMMAND:
                    int command = wParam.ToInt32() & 0xfff0;
                    if (command == SC_MOVE)
                    {
                        handled = true;
                    }
                    break;
                default:
                    break;
            }
            return IntPtr.Zero;
        }
        void timer_VisitorTick(object sender, EventArgs e)
        {
          
            Task.Run(() => DataReplicator.VisitorLog());
            Task.Run(() => DataReplicator.VisitorSpeedPass());
            Task.Run(() => SyncVisitorExit());

            Task.Run(() => SyncVisitorData());
        }

        void SyncVisitorData()
        {
         
            var store = App.Container.Resolve<VisitStorage>();

            var items = store.FindNotSynced(typeof(VisitModel).ToString());

            int count = 0;
            string setUri = string.Empty;

            foreach (var lng in items)
            {
                try
                {
                    var item = store.GetFromDatabase<VisitModel>(lng);

                    if (item != null)
                    {

                        var cmd = new RecordVisitorScan(Guid.NewGuid());
                        cmd.VisitorFirstName = item.FirstName;
                        cmd.VisitorLastName = item.LastName;
                        cmd.Street1 = item.Street1;
                        cmd.City = item.City;
                        cmd.State = item.State;
                        cmd.VisitorBirthDate = item.DateOfBirth;
                        cmd.VisitorLicenseNumber = item.Identification;

                        //cmd.ImageStream = ConvertBytes(new Uri(item.ImagePath));
                        cmd.DateRecorded = item.VisitEntryDate;
                        cmd.ReasonForVisit = item.ReasonForVisit;
                        //cmd.DateExited = item.VisitExitDate;
                        cmd.VisitEntryNumber = item.VisitEntryNumber;
                        cmd.SchoolId = Settings.Default.SchoolId;
                        cmd.Source = string.Format(Environment.MachineName);

                        var uri = $"{RemoteStorage.ApiUrl}/Station/Publish/VisitData";

                        using (var client = new JsonServiceClient(RemoteStorage.ApiUrl))
                        {

                            var json = client.Post<string>(uri, cmd);

                        }

                        /*
                        using (HttpClient client = new HttpClient())
                        {
                            //client.Timeout = TimeSpan.FromMilliseconds(1000);
                            var json = JsonConvert.SerializeObject(cmd);
                            var content = new StringContent(json, Encoding.UTF8, "application/json");
                            var response = await client.PostAsync(uri, content);
                            var respString = await response.Content.ReadAsStringAsync();

                        }*/

                        store.RecordDatabaseSynced<VisitModel>(lng, item.VisitEntryNumber, item.VisitEntryDate.Date);

                    }
                    else
                    {
                        Logger.WarnFormat("Removing invalid visit item from key value store {0}", lng);
                        store.RemoveFromSet(lng, setUri);
                    }
                }
                catch (Exception ex)
                {
                    Logger.ErrorFormat("An error occurred posting visit data: {0}", ex.Message);
                }
                finally
                {

                }
            }
        }

        void SyncVisitorExit()
        {

            var store = App.Container.Resolve<VisitExitStorage>();

            var items = store.FindNotSynced(typeof(VisitExit).ToString());

            int count = 0;
            string setUri = string.Empty;

            foreach (var lng in items)
            {
                try
                {
                    var item = store.GetFromDatabase<VisitExit>(lng);

                    if (item != null)
                    {

                        var cmd = new PostVisitExit(Guid.Parse(item.VisitId));
                        cmd.DateExited = item.DateExited;
                        cmd.VisitNumber = item.VisitNumber;
                        cmd.SchoolId = Settings.Default.SchoolId;
                        cmd.Source = string.Format(Environment.MachineName);

                        var uri = $"{RemoteStorage.ApiUrl}/Station/Publish/VisitExit";

                        using (var client = new JsonServiceClient(RemoteStorage.ApiUrl))
                        {
                            var json = client.Post<string>(uri, cmd);

                            store.RecordDatabaseSynced<VisitExit>(lng, item.VisitNumber, item.DateExited.Date);
                        }


                    }
                    else
                    {
                        Logger.WarnFormat("Removing invalid visit item from key value store {0}", lng);
                        store.RemoveFromSet(lng, setUri);
                    }
                }
                catch (Exception ex)
                {
                    Logger.ErrorFormat("An error occurred posting visit data: {0}", ex.Message);
                }
                finally
                {

                }
            }
        }


        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            try
            {
               
                // Enables WPF to mark edit field as supporting text pattern (Automation Concept)
                System.Windows.Automation.AutomationElement asForm =
                    System.Windows.Automation.AutomationElement.FromHandle(new WindowInteropHelper(this).Handle);

                // Windows 8 API to enable touch keyboard to monitor for focus tracking in this WPF application
                InputPanelConfigurationLib.InputPanelConfiguration inputPanelConfig =
                    new InputPanelConfigurationLib.InputPanelConfiguration();
                inputPanelConfig.EnableFocusTracking();

                //ShowMaxRestoreButton = false;
                //ShowMinButton = false;
                Loaded -= OnLoaded;
            }catch{}
        }

    }
}

     
