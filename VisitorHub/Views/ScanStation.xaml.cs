using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Media;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Autofac;
using Common.Models;
using log4net;
using log4net.Repository.Hierarchy;
using Newtonsoft.Json;
using ReactiveUI;
using ServiceStack.Messaging;
using Simple.Data;
using SwipeDesktop.Api;
using SwipeDesktop.Interop;
using SwipeDesktop.Models;
using SwipeDesktop.Storage;
using SwipeDesktop.ViewModels;
using System.Configuration;
using System.Data;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Reactive.Concurrency;
using ServiceStack;
using SwipeDesktop.Common;
using SwipeDesktop.Config;
using System.Windows.Controls.Primitives;
using MahApps.Metro.Controls;

using SwipeDesktop.DeviceLibrary;
using SwipeK12;
using Telerik.Windows.Controls;

namespace SwipeDesktop.Views
{
    /// <summary>
    /// Interaction logic for Shell.xaml
    /// </summary>
    public partial class ScanStation : IViewFor<ScanStationViewModel>
    {
        private static readonly bool SyncEnabled = !bool.Parse(ConfigurationManager.AppSettings["holdRecordsInQueue"]);
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ScanStation));
        private string badScan = SwipeDesktop.Settings.Default.SoundsFolder + "\\badscan.wav";

        private Port _serialPort1;
        private Port _serialPort2;

        private DispatcherTimer _timer;

        private bool _finePopupOpen;

        ScanStorage Scans { get; set; }

        DetentionStorage Detentions { get; set; }

        DismissalStorage Dismissals { get; set; }

        StaffScanStorage StaffScans { get; set; }

        FineStorage Fines { get; set; }

        InOutStorage LocationScans { get; set; }

        AlertPrintedStorage AlertsPrinted { get; set; }
        
        private static readonly LocalStorage Client = new LocalStorage();

        public RemoteStorage RemoteStorage { get; private set; }

        public LocalStorage LocalStorage { get; private set; }

        public IdCardStorage IdCards { get; private set; }

        internal static USBQueue UsbQueue{ get; set; }

        private static List<USBDeviceInfo> USBDevices = new List<USBDeviceInfo>();
        public static List<Port> ConnectedPorts = new List<Port>();

        private void BootstrapScanStation()
        {
         

            //LocalStorage.ImportDefaultIdCardTemplates();

            MessageBus.Current.Listen<Tuple<string>>().Subscribe(_ =>
            {
                var msg = _;

                if (msg.Item1 == "SetFocusStudentList")
                {
                    //var item = StudentsList.SelectedItem;
                    if (StudentsList.SelectedIndex > -1)
                    {
                        ListViewItem item = StudentsList.ItemContainerGenerator.ContainerFromIndex(StudentsList.SelectedIndex) as ListViewItem;

                        if (item != null)
                            item.Focus();
                    }
                    //var focusedControl = FocusManager.GetFocusedElement(this);
                }

                if (msg.Item1 == "FineAdded")
                {
                    SearchBox.Clear();
                    SearchBox.Focus();
                }

                if (_.Item1 == "SetFocusSearchBox")
                {
                    SearchBox.Focus();
                    Keyboard.Focus(SearchBox);
                }

              
               
            });

            //this.Events()
            
            StudentsList.Events().KeyDown.Where(k => k.Key == Key.P).Subscribe(x =>
            {
                var vm = (DataContext) as ScanStationViewModel;
                var source = x.KeyboardDevice.Target as ListViewItem;

                if (vm != null && source != null)
                {
                    var student = source.Content as StudentModel;
                    vm.SelectedStudent = student;
                    vm.RaiseCameraPopup(source, student);
                }
            });

            /*
            StudentsList.Events().KeyDown.Where(k => k.Key == Key.F).Subscribe(x =>
            {
                var vm = (DataContext) as ScanStationViewModel;
                var source = x.KeyboardDevice.Target as ListViewItem;

                if (vm != null && source != null)
                {
                    var student = source.Content as StudentModel;
                    vm.SelectedStudent = student;
                    vm.RaiseStudentAltId(student);
                    
                }
            });
            */

            StudentsList.Events().KeyDown.Where(k => k.Key == Key.I).Subscribe(x =>
            {
                var vm = (DataContext) as ScanStationViewModel;
                var source = x.KeyboardDevice.Target as ListViewItem;

                if (vm != null && source != null)
                {
                    var student = source.Content as StudentModel;

                    if (student != null)
                    {
                        vm.SelectedStudent = student;
                        vm.RaiseFinePopup(student, source);
                    }
                    else
                    {
                        var person = source.Content as PersonModel;
                        vm.SelectedStaff = person ;
                        vm.RaiseStaffPopup(person, source);
                    }

                }
            });

            if (SyncEnabled)
            {
                Observable.Interval(TimeSpan.FromSeconds(60), Scheduler.Default)
                    .ObserveOnDispatcher(DispatcherPriority.Background)
                    .Subscribe(async x =>
                    {
                        var rslt = await SyncSwipeStationData();
                        var rslt2 = await UploadStaffScans();
                        var rslt3  = await SyncLocationData();
                       
                    });

                Observable.Interval(TimeSpan.FromSeconds(240), Scheduler.Default)
                    .ObserveOnDispatcher(DispatcherPriority.Background)
                    .Subscribe(x =>
                    {
                        SyncFineData();
                        SyncDismissalData();
                        SyncIdCardData();
                        SyncAlertPrintedData();
                    });


                Observable.Interval(TimeSpan.FromSeconds(120), Scheduler.Default)
                    .ObserveOnDispatcher(DispatcherPriority.Background)
                    .Subscribe(x =>
                    {
                        SyncDetentionData();
                    });

            }

            //var items = LocationScans.GetItemsByDate(DateTime.Today);
            RecoverNotSynced();
            Task.Run(()=>DownloadIdCards());
        }

        private void RightScans_Added(object sender, NotifyCollectionChangedEventArgs e)
        {
            ScrollViewer scrollViewer = GetScrollViewer(RightScans) as ScrollViewer;

            if(scrollViewer != null)
                scrollViewer.ScrollToTop();

            //RightScans.ScrollIntoView(e.AddedItems[0]);
        }
        private void LeftScans_Added(object sender, NotifyCollectionChangedEventArgs e)
        {
            ScrollViewer scrollViewer = GetScrollViewer(LeftScans) as ScrollViewer;

            if (scrollViewer != null)
                scrollViewer.ScrollToTop();
            
        }

        void RecoverNotSynced()
        {
            try
            {
                var db = Database.OpenNamedConnection("ScanStation");

                dynamic data = db.RedisScans.FindAll(db.RedisScans.SyncTime == null);

                foreach (dynamic record in data)
                {
                    var id = record.Id;
                    var scan = db.RedisScans.FindById(id);
                    var redisId = record.RedisId;

                    if (redisId == 0)
                        redisId = id;

                    var type = scan.ObjectType;

                    if (type == typeof(LocationScan).ToString())
                    {
                        var json = JsonConvert.DeserializeObject<LocationScan>(scan.ObjectJson);

                        //var urn = string.Empty;
                        //var items = LocationScans.FindNotSynced(out urn);

                        var item = LocationScans.GetById((long) redisId);
                        if (item == null)
                        {

                            //if (json.Id == 0)
                            //{
                            json.Id = id;
                            //}
                            LocationScans.InsertObject(json, false);
                        }


                    }
                    if (type == typeof(ScanModel).ToString())
                    {
                        var json = JsonConvert.DeserializeObject<ScanModel>(scan.ObjectJson);

                        //var urn = string.Empty;
                        //var items = Scans.FindNotSynced(out urn);

                        var item = Scans.GetById((long) redisId);
                        if (item == null)
                        {
                            //if (json.Id == 0)
                            //{
                            json.Id = id;
                            //}
                            Scans.InsertObject(json, false);
                        }


                    }

                    if (type == typeof(StaffRecord).ToString())
                    {
                        var json = JsonConvert.DeserializeObject<StaffRecord>(scan.ObjectJson);

                        //var urn = string.Empty;
                        //var items = Scans.FindNotSynced(out urn);

                        var item = StaffScans.GetById((long) redisId);
                        if (item == null)
                        {
                            //if (json.Id == 0)
                            //{
                            json.Id = id;
                            //}
                            StaffScans.InsertObject(json, false);
                        }


                    }

                    if (type == typeof(Consequence).ToString())
                    {
                        var json = JsonConvert.DeserializeObject<Consequence>(scan.ObjectJson);

                        var item = Detentions.GetById((long) redisId);
                        if (item == null)
                        {

                            //if (json.Id == 0)
                            //{
                            json.Id = id;
                            //}
                            Detentions.InsertObject(json, false);
                        }


                    }

                    if (type == typeof(Dismissal).ToString())
                    {
                        var json = JsonConvert.DeserializeObject<Dismissal>(scan.ObjectJson);

                        var item = Dismissals.GetById((long) redisId);
                        if (item == null)
                        {

                            //if (json.Id == 0)
                            //{
                            json.Id = id;
                            //}
                            Dismissals.InsertObject(json, false);
                        }


                    }


                    if (type == typeof(AssessedFine).ToString())
                    {
                        var fine = JsonConvert.DeserializeObject<AssessedFine>(scan.ObjectJson);

                        var item = Fines.GetById((long) redisId);
                        if (item == null)
                        {

                            //if (fine.Id == 0)
                            //{
                            fine.Id = id;
                            //}
                            Fines.InsertObject(fine, false);
                        }


                    }

                    if (type == typeof(NewIdCard).ToString())
                    {
                        var card = JsonConvert.DeserializeObject<NewIdCard>(scan.ObjectJson);

                        var item = IdCards.GetById((long) redisId);
                        if (item == null)
                        {

                            //if (card.Id == 0)
                            //{
                            card.Id = id;
                            //}
                            IdCards.InsertObject(card, false);
                        }


                    }

                    if (type == typeof(AlertPrinted).ToString())
                    {
                        var json = JsonConvert.DeserializeObject<AlertPrinted>(scan.ObjectJson);

                        var item = AlertsPrinted.GetById((long) redisId);
                        if (item == null)
                        {

                            //if (json.Id == 0)
                            //{
                            json.Id = id;
                            //}
                            json.StudentImage = null;
                            AlertsPrinted.InsertObject(json, false);
                        }


                    }

                }

            }catch(Exception ex){
                Logger.Error(ex);
            }

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


        async Task<bool> SyncSwipeStationData()
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

            var dict = new Dictionary<string, ScanModel>();
            var store = Scans;

            string setUri = string.Empty;

            int countFailed = 0, countSent = 0;
            try
            {
                var items = store.FindNotSynced(typeof(ScanModel).ToString());

                foreach (var lng in items)
                {

                    try
                    {

                        //var item = store.GetById(lng);
                        var item = store.GetFromDatabase<ScanModel>(lng);
                       
                        var response = await RemoteStorage.SendScanAsync(item);

                        //store.RemoveFromSet(item.Id, setUri);
                        store.RecordDatabaseSynced<ScanModel>(lng, item.Barcode, item.EntryTime.Date);

                        
                        countSent++;
                    }
                    catch (Exception ex)
                    {
                        countFailed++;
                        Logger.Error("An error occurred syncing scan records", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                countFailed++;
            }

            dict.Clear();

            if(countFailed > 0)
                return false;

            return true;
            //ViewModel.QueuedScanRecords = Scans.CountNotSynced();
        }

        void SyncDetentionData()
        {
            if (!CheckInternet())
            {
                Logger.ErrorFormat("Network connection not detected, skipping detention sync at {0}.", DateTime.Now);
                return;
            }

            var dict = new Dictionary<int, Consequence>();
            var store = Detentions;

            string setUri = string.Empty;

            //var items = store.FindNotSynced(out setUri);
            var items = store.FindNotSynced(typeof(Consequence).ToString());


            int count = 0;

            foreach (var lng in items)
            {
                count++;

                try
                {
                    //var item = store.GetById(lng);
                    var item = store.GetFromDatabase<Consequence>(lng);

                    if (item != null)
                    {
                        //dict.Add(count, item);

                        //if (dict.Count > 0)
                        //{
                            var response = RemoteStorage.SendConsequence(item);

                            //store.RemoveFromSet(item.Id, setUri);
                            //store.MarkAsSynced<Consequence>(item.Id, item.StudentNumber, item.InfractionDate.Date);
                            store.RecordDatabaseSynced<Consequence>(lng, item.StudentNumber, item.InfractionDate.Date);

                        //}
                    }
                    else
                    {
                        Logger.WarnFormat("Removing invalid consequence item from key value store {0}", lng);
                        store.RemoveFromSet(lng, setUri);
                    }

                }
                catch (Exception ex)
                {
                    Logger.Error("An error occurred syncing consequence data.", ex);
                }
                
            }

            dict.Clear();

            //ViewModel.QueuedDetentionRecords = Detentions.CountNotSynced();
        }

        void SyncAlertPrintedData()
        {
            if (!CheckInternet())
            {
                Logger.ErrorFormat("Network connection not detected, skipping alert sync at {0}.", DateTime.Now);
                return;
            }

            var dict = new Dictionary<int, AlertPrinted>();
          
            string setUri = string.Empty;

            var items = AlertsPrinted.FindNotSynced(out setUri);

            int count = 0;

            foreach (var lng in items)
            {
                count++;
                var item = AlertsPrinted.GetById(lng);

                try
                {
                    if (item != null)
                    {
                        dict.Add(count, item);

                        if (dict.Count > 0)
                        {
                            var response = RemoteStorage.SendAlertPrinted(item);

                            AlertsPrinted.RemoveFromSet(item.Id, setUri);
                            AlertsPrinted.MarkAsSynced<AlertPrinted>(item.Id, item.StudentNumber, item.InfractionDate.Date);

                        }
                    }
                    else
                    {
                        Logger.WarnFormat("Removing invalid AlertPrinted item from key value store {0}", lng);
                        AlertsPrinted.RemoveFromSet(lng, setUri);
                    }

                }
                catch (Exception ex)
                {
                    Logger.Error("An error occurred syncing alerts printed data.", ex);
                }
            }

            dict.Clear();

            //ViewModel.QueuedAlertsAck = AlertsPrinted.CountNotSynced();
        }

        void SyncFineData()
        {
            if (!CheckInternet())
            {
                Logger.ErrorFormat("Network connection not detected, skipping fine sync at {0}.", DateTime.Now);
                return;
            }

            var dict = new Dictionary<int, AssessedFine>();
            var store = Fines;

            string setUri = string.Empty;

            var items = store.FindNotSynced(out setUri);

            int count = 0;

            foreach (var lng in items)
            {

                try
                {
                    count++;
                    var item = store.GetById(lng);

                    if (item != null)
                    {
                        dict.Add(count, item);

                        if (dict.Count > 0)
                        {
                            var response = RemoteStorage.SendFine(item);

                            store.RemoveFromSet(item.Id, setUri);
                            store.MarkAsSynced<AssessedFine>(item.Id, item.StudentNumber, item.FineDate.Date);
                        
                        }
                    }
                    else
                    {
                        Logger.WarnFormat("Removing invalid fine item from key value store {0}", lng);
                        store.RemoveFromSet(lng, setUri);
                    }

                }
                catch (Exception ex)
                {
                    Logger.Error("An error occurred syncing fine data.", ex);
                }
            }

            dict.Clear();

            //ViewModel.QueuedFines = Fines.CountNotSynced();
        }

        void SyncIdCardData()
        {
           
            if (!CheckInternet())
            {
                Logger.ErrorFormat("Network connection not detected, skipping id card sync at {0}.", DateTime.Now);
                return;
            }

            var dict = new Dictionary<int, NewIdCard>();
            var store = IdCards;

            string setUri = string.Empty;

            var items = store.FindNotSynced(out setUri);

            int count = 0;

            foreach (var lng in items)
            {

                try
                {
                    count++;
                    var item = store.GetById(lng);

                    if (item != null)
                    {
                        dict.Add(count, item);

                        if (dict.Count > 0)
                        {
                            var response = RemoteStorage.SendIdCardPrinted(item);
                            
                            store.RemoveFromSet(item.Id, setUri);
                            store.MarkAsSynced<NewIdCard>(item.Id, item.StudentNumber, item.PrintDate.Date);

                        }
                    }
                    else
                    {
                        Logger.WarnFormat("Removing invalid id card item from key value store {0}", lng);
                        store.RemoveFromSet(lng, setUri);
                    }

                }
                catch (Exception ex)
                {
                    Logger.Error("An error occurred syncing id card data.", ex);
                }
            }

            dict.Clear();

            //ViewModel.QueuedIdCards = IdCards.CountNotSynced();
        }

        void DownloadIdCards()
        {

            if (!CheckInternet())
            {
                Logger.ErrorFormat("Network connection not detected, skipping id card download at {0}.", DateTime.Now);
                return;
            }

            var idcardsBll = new SwipeCardBLL();
            var cards = idcardsBll.GetIDCards();

            //var schoolCards = cards.Any(x => x.SchoolID == SwipeDesktop.Settings.Default.SchoolId);

            //if (!schoolCards)
            //{
                var data = RemoteStorage.GetIdCards();
                foreach (var item in data)
                {
                    if (LocalStorage.FindCardByName(item.SchoolID, item.CardName) == null)
                    {
                        //cards.AddIDCardsRow(item);
                        var id = idcardsBll.AddIDCard(item.SchoolID, item.CardName, item.CardWidth, item.CardHeight,
                            item.StudentCard, item.TeacherCard, item.OtherCard, item.TempCard, item.FrontBackground,
                            item.FrontOpacity,
                            item.BackBackground, item.BackOpacity, item.DualSided, item.FrontPortrait, item.BackPortrait,
                            item.Fields, item.Active);

                        Logger.DebugFormat("Inserted ID Card {1} with ID {0}", id, item.CardName);
                    }
                    else
                    {
                      
                       
                        try
                        {
                            Logger.DebugFormat("Found ID Card with Name {0}", item.CardName);

                            idcardsBll.UpdateCardByIdAndSchool(item.FrontBackground, item.FrontOpacity,
                                item.BackBackground,
                                item.BackOpacity, item.DualSided, item.Fields, item.Active, item.CardID, item.SchoolID);
                        }
                        catch(Exception ex)
                        {
                            Logger.Error("Coult not update id card template", ex);
                        }

                    }
            }
            //}
        }

        async Task<bool> SyncLocationData()
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
           
            var dict = new Dictionary<long, LocationScan>();
            var store = LocationScans;

            string setUri = string.Empty;

            var items = store.FindNotSynced(out setUri);

            int countFailed = 0, countSent = 0;
            foreach (var lng in items)
            {   
                try
                {
                    var item = store.GetById(lng);

                    if (item != null)
                    {
                        dict.Add(item.Id, item);

                        if (dict.Count > 0)
                        {
                            var response = RemoteStorage.SendLocationScan(item);
                                
                            store.RemoveFromSet(item.Id, setUri);
                            store.MarkAsSynced<LocationScan>(item.Id, item.StudentNumber, item.SwipeTime.Date);

                            countSent++;
                        }
                    }
                    else
                    {
                        Logger.WarnFormat("Removing invalid location item from key value store {0}", lng);
                        store.RemoveFromSet(lng, setUri);
                    }

                }
                catch (Exception ex)
                {
                    countFailed++;
                    Logger.Error("An error occurred syncing location data.", ex);
                }
            }

            dict.Clear();

            if (countFailed > 0)
                return false;

            return true;
            //ViewModel.QueuedLocationScans = LocationScans.CountNotSynced();
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

        void SyncDismissalData()
        {
            if (!CheckInternet())
            {
                Logger.ErrorFormat("Network connection not detected, skipping dismissal sync at {0}.", DateTime.Now);
                return;
            }

            SyncDismissal();
            //ViewModel.QueuedDismissalRecords = Dismissals.CountNotSynced();
        }

        async Task<bool> UploadStaffScans()
        {
            if (!CheckInternet())
            {
                Logger.ErrorFormat("Network connection not detected, skipping staff scans upload at {0}.", DateTime.Now);
                return false;
            }

            var dict = new Dictionary<long, StaffRecord>();
            var store = StaffScans;

            string setUri = string.Empty;

            var items = store.FindNotSynced(typeof(StaffRecord).ToString());

            foreach (var lng in items)
            {

                try
                {
                    var item = store.GetFromDatabase<StaffRecord>(lng);

                    if (item != null)
                    {
                        dict.Add(item.Id, item);

                        if (dict.Count > 0)
                        {
                            var response = await RemoteStorage.SendStaffScanAsync(item);

                            //store.RemoveFromSet(item.Id, setUri);
                            store.MarkAsSynced<StaffRecord>(item.Id, item.Barcode, item.EntryTime.Date);

                        }
                    }

                }
                catch (Exception ex)
                {
                    Logger.Error("An error occurred syncing staff scans", ex);
                }
            }

            dict.Clear();

            return true;
            //ViewModel.QueuedDismissalRecords = Dismissals.CountNotSynced();
        }

        bool PopupIsOpen()
        {
            return ViewModel != null && ViewModel.CurrentDialogViewModel != null && ViewModel.CurrentDialogViewModel.ShowPopup;
        }

        async void port_BarcodeReceived(object sender, EventArgs e)
        {
            var _ = sender as dynamic;

            var student = await LocalStorage.SearchByBarcodeAsync(_.barcode);

            if(student == null)
                Logger.Warn(string.Format("Student Lookup failed on barcode {0}", _.barcode));

            await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.DataBind, new Action(() => OnBarcodeResult(new Tuple<PersonModel, Lane, bool, string>(student, _.lane, false, _.barcode))));
        }

        void InitSerialDrivers()
        {
            var deviceName = SwipeDesktop.Settings.Default.DeviceName;
            Logger.InfoFormat("Init Serial Drivers" + deviceName);

            try
            {
                USBDevices = USBHelper.GetUSBDevices(deviceName);


                Logger.InfoFormat("Found {0} devices for scans.", USBDevices.Count());

                foreach (var device in USBDevices)
                {

                    Logger.InfoFormat("Connecting Device {0} – {1}", device.Description, device.DevicePort);

                    var port = new Port(device.DevicePort);
                    port.BarcodeReceived += port_BarcodeReceived;
                    port.Connect();

                    ConnectedPorts.Add(port);
                }

            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("Could not enumerate USB Devices", ex);
            }

           
        }
        public ScanStation(ScanStorage scans, StaffScanStorage staffScans, RemoteStorage remoteStorage, LocalStorage localStorage, DetentionStorage detentions, InOutStorage locationScans, DismissalStorage dismissals, FineStorage fines, IdCardStorage cards, AlertPrintedStorage alerts)
        {
            InitializeComponent();

            UserError.RegisterHandler(async error =>
            {
                RxApp.MainThreadScheduler.Schedule(error, (scheduler, userError) =>
                {
                    // NOTE: this code really shouldn't throw away the MessageBoxResult
                    var result = MessageBox.Show(userError.ErrorMessage);
                    return Disposable.Empty;
                });

                return await Task.Run(() => RecoveryOptionResult.CancelOperation);
            });

            StaffScans = staffScans;
            RemoteStorage = remoteStorage;
            LocalStorage = localStorage;
            Detentions = detentions;
            Scans = scans;
            LocationScans = locationScans;
            Dismissals = dismissals;
            Fines = fines;
            IdCards = cards;
            AlertsPrinted = alerts;

            if (SwipeDesktop.Settings.Default.BarcodeDevice.ToLower().Contains("serial"))
            {
                InitSerialDrivers();
            }

            if (SwipeDesktop.Settings.Default.BarcodeDevice.ToLower().Contains("idinnovations"))
            {

                try
                {
                    UsbQueue = new USBQueue();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not initialize USB drivers.\n\n" + ex.Message);
                }

                if (!UsbQueue.Open())
                {
                    MessageBox.Show("There was a problem connecting to the ID Innovations device driver.\n\n ID Innovations barcode readers will be unavailable.");
                }

                Task.Run(() => ReadScans());
            }

            TaskScheduler.UnobservedTaskException += (s, e) => {
                Logger.Error(e.Exception);  //The Exception that went unobserved.
                
                e.SetObserved(); //Marks the Exception as "observed," thus preventing it from triggering exception escalation policy which, by default, terminates the process.
            };

            /*MessageBus.Current.Listen<ScanData>().Select(x=>Observable.Return(x.Data)).Switch()
                .ObserveOnDispatcher()
                .Select(Client.SearchByBarcode).Switch()
                .ObserveOnDispatcher()
                .Subscribe(OnBarcodeResult);*/
              
            this.WhenAnyValue(x => x.ViewModel).BindTo(this, x => x.DataContext);

            this.OneWayBind(ViewModel, vm => vm.ScanModeList, v => v.ScanModes.DataContext);

            this.OneWayBind(ViewModel, vm => vm.LeftLaneScans, v => v.LeftScans.ItemsSource);
            this.OneWayBind(ViewModel, vm => vm.LaneScans, v => v.RightScans.ItemsSource);

            this.OneWayBind(ViewModel, vm => vm.SearchResults, v => v.StudentsList.ItemsSource);
            this.OneWayBind(ViewModel, vm => vm.PeriodList, v => v.Periods.DataContext);
           
            this.OneWayBind(ViewModel, vm => vm.PeriodList, v => v.CafePeriods.DataContext);

            this.OneWayBind(ViewModel, vm => vm.AttendanceCodeList, v => v.AttendanceCodes.DataContext);
            this.OneWayBind(ViewModel, vm => vm.LocationList, v => v.Locations.DataContext);
            
            this.Bind(ViewModel, vm => vm.PrintPasses, v => v.PrintToggleSwitch.IsChecked);
            this.Bind(ViewModel, vm => vm.InOutPasses, v => v.InOutToggleSwitch.IsChecked);
            this.Bind(ViewModel, vm => vm.MarkAllPresent, v => v.MarkAllPresentToggleSwitch.IsChecked);
            this.Bind(ViewModel, vm => vm.Dismissal, v => v.DismissalToggleSwitch.IsChecked);
            
            this.Bind(ViewModel, vm => vm.Barcode, v => v.BarcodeText.Text);
            this.Bind(ViewModel, vm => vm.QueuedRecordsText, v => v.RecordsInQueue.Text);
            this.Bind(ViewModel, vm => vm.StudentRecordsText, v => v.StudentRecordsScanned.Text);

            this.Bind(ViewModel, vm => vm.SyncErrorsText, v => v.SyncErrors.Text);
            this.Bind(ViewModel, vm => vm.Connected, v => v.InternetConnectedText.Text);
            this.Bind(ViewModel, vm => vm.CurrentActivity, v => v.CurrentActivity.Text);
            //this.Bind(ViewModel, vm => vm.SearchText, v => v.SearchBox.Text);
            //this.Bind(ViewModel, vm => vm.SelectedStudent, v => v.StudentsList.SelectedItem);

            this.BindCommand(ViewModel, vm => vm.TestPrintCommand, v => v.TestPrint);
            this.BindCommand(ViewModel, vm => vm.ShowSettingsCommand, v => v.ShowSettings);
            this.BindCommand(ViewModel, vm => vm.ExitCommand, v => v.Exit);
            this.BindCommand(ViewModel, vm => vm.SyncNowCommand, v => v.SyncNow);

            /*
            this.WhenAnyValue(x => x.ViewModel.CurrentDialogViewModel.CurrentContent).Subscribe(_ =>
            {
                if (_.GetType() == typeof (StudentCardViewModel))
                {
                    _finePopupOpen = true;
                }
                else
                {
                    _finePopupOpen = false;
                }
            });*/

            var textchanges = Observable.FromEventPattern<KeyEventHandler, KeyEventArgs>(
                h => BarcodeText.KeyDown += h,
                h => BarcodeText.KeyDown -= h
                ).Where(x =>
                {
                    var source = x.Sender as TextBox;
                    return source != null && !PopupIsOpen();
                }).Select(x => new Tuple<Key,string>(x.EventArgs.Key, ((TextBox)x.Sender).Text));

            textchanges
                .Throttle(TimeSpan.FromMilliseconds(10))
                .Select(Enter) 
                .Switch()
                .ObserveOnDispatcher()
                .Select(Client.ObservableOnBarcode)
                .Switch()
                .ObserveOnDispatcher()
                .Subscribe(OnBarcodeResult);

            var studentSearchText = Observable.FromEventPattern<TextChangedEventHandler, TextChangedEventArgs>(
                h => SearchBox.TextChanged += h,
                h => SearchBox.TextChanged -= h
                ).Select(x => ((TextBox)x.Sender).Text);

            studentSearchText
                .Throttle(TimeSpan.FromMilliseconds(100))
                .Select(Client.SearchStudentsAsync)
                .Switch()
                .ObserveOnDispatcher()
                .Subscribe(OnSearchResult);

            
            this.Events().KeyDown.Where(k => k.Key == Key.Escape).Subscribe(x =>
            {
                StudentsList.SelectedItem = null;
                SearchBox.Clear();
        
                if (PopupIsOpen())
                {


                    ViewModel.CurrentDialogViewModel.HideAction(ViewModel.CurrentDialogViewModel);


                    if (ViewModel.CurrentDialogViewModel.CurrentContent.GetType() == typeof(StudentAlternateIdViewModel) || ViewModel.CurrentDialogViewModel.CurrentContent.GetType() == typeof(StudentCardViewModel))
                    {
                        SearchBox.Focus();
                    }

                }
            });
            
            this.Events().KeyDown.Where(k => k.Key == Key.F1).Subscribe(x =>
            {
                var vm = (DataContext) as ScanStationViewModel;
                vm.RaiseAddPersonPopup();
            });

            this.Events().KeyDown.Where(k => k.Key == Key.F7).Subscribe(x =>
            {
                var app = App.Current as SwipeDesktop.App;

                if (app != null)
                {
                    RefreshScannerLib();
                }

            });

            this.Events().KeyDown.Where(k => k.Key == Key.Enter).Subscribe(x =>
            {
                Control source = x.KeyboardDevice.Target as Control;

                if (PopupIsOpen())
                {
                    if (ViewModel.CurrentDialogViewModel.CurrentContent.GetType() == typeof(StudentAlternateIdViewModel))
                    {                     
                        return;
                    }

                    if (ViewModel.CurrentDialogViewModel.CurrentContent.GetType() == typeof(StudentCardViewModel))
                    {
                        ConfirmButton.Focus();
                        ViewModel.CurrentDialogViewModel.SaveAction(ViewModel.CurrentDialogViewModel);
                        return;
                    }

                }

               
                if (source != null && source.Name == "SearchBox")
                {

                    if (StudentsList.HasItems)
                    {
                        if (StudentsList.Items.Count == 1)
                        {
                            SelectStudent(StudentsList.Items[0] as StudentModel);

                            SearchBox.Focus();
                        }
                        else
                        {
                            StudentsList.SelectedItem = StudentsList.Items[0];
                            var item = StudentsList.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem;
                            if (item != null) item.Focus();
                        }

                        return;
                    }
                    
                }

                var type = source.GetType();
                if (source != null && type == typeof(ListViewItem))
                {
                    var list = source as ListViewItem;
                    if (list != null)
                    {
                        var student = list.Content as StudentModel;
                        if (student != null)
                        {
                            SelectStudent(student);
                        }

                        var person = list.Content as PersonModel;
                        if (person != null)
                        {
                            SelectPerson(person);
                        }
                    }
                }

               
            });

            this.Events().KeyUp.Subscribe(x =>
            {
                Control source = null;
                source = x.KeyboardDevice.Target as TextBox;
                if (x.Key == Key.Down && source != null && source.Name == "SearchBox")
                {

                    if (StudentsList.HasItems)
                    {
                        StudentsList.SelectedItem = StudentsList.Items[0];
                        var item = StudentsList.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem;
                        if (item != null) item.Focus();
                    }

                }

                source = x.KeyboardDevice.Target as ListViewItem;
                if (x.Key == Key.Up && source != null && StudentsList.SelectedIndex == 0)
                {
                    StudentsList.SelectedItem = null;
                    
                    SearchBox.Focus();
                }
                

            });

            Observable.Interval(TimeSpan.FromSeconds(300), Scheduler.Default).Subscribe(x =>
            {
                Logger.Warn("Syncing at hour : " + DateTime.Now.Hour);

                if (DateTime.Now.Hour < 7 || DateTime.Now.Hour > 9)
                {
                    var t0 = Task.Run(() => DataReplicator.InitRemoteServer());
                    Task.WhenAll(new[] {t0}).ContinueWith((c) =>
                    {
                        Task.Run(() => DataReplicator.SyncTardySwipeDiff());
                        //Task.Run(() => DataReplicator.TardySwipes());
                        Task.Run(() => DataReplicator.Transactional());

                       
                    });
                }
            });

            Observable.Interval(TimeSpan.FromSeconds(30)).ObserveOnDispatcher(DispatcherPriority.Background).Subscribe(async x => 
            {
                try
                {
                    var qry1 = await LocalStorage.GetErrorScanCountAsync(SwipeDesktop.Settings.Default.SchoolId);
                    //task1.Wait();

                    var qry2 = await LocalStorage.GetQueueCountAsync(SwipeDesktop.Settings.Default.SchoolId);
                    //task2.Wait();

                    var qry3 = await LocalStorage.GetStudentScanCountAsync(SwipeDesktop.Settings.Default.SchoolId);
                    //task3.Wait();

                    ViewModel.QueuedScanRecords = qry2;

                    ViewModel.StudentScanRecords = qry3;

                    ViewModel.SyncErrorRecords = qry1;

                }
                catch (Exception ex)
                {
                    Logger.Error("Could not refresh student scan stats.", ex);
                }

            });

            RemoteStorage.ImageList.ItemsRemoved.ObserveOnDispatcher(DispatcherPriority.ApplicationIdle).Subscribe(_ =>
            {
                if (RemoteStorage.ImageList.Any())
                {
                    ViewModel.CurrentActivity = string.Format("Downloading {0} student images", RemoteStorage.ImageList.Count());
                }
                else
                {
                    ViewModel.CurrentActivity = "Done downloading student images";
                }
            });

            RemoteStorage.ImageList.ShouldReset.ObserveOnDispatcher().Subscribe(_ => ViewModel.CurrentActivity = string.Format("Downloading {0} student images", RemoteStorage.ImageList.Count()));
        
            BootstrapScanStation();

            Dispatcher.BeginInvoke(DispatcherPriority.Input,
                new Action(delegate()
                        {
                            BarcodeText.Focus();         // Set Logical Focus
                            Keyboard.Focus(BarcodeText); // Set Keyboard Focus
                        }));


            ((INotifyCollectionChanged)RightScans.Items).CollectionChanged += RightScans_Added;
            ((INotifyCollectionChanged)LeftScans.Items).CollectionChanged += LeftScans_Added;
            //StudentsList.ItemContainerGenerator.StatusChanged += ItemContainerGeneratorOnStatusChanged;

        }

        void RefreshScannerLib()
        {
            var closed = UsbQueue.Close();

            if (closed)
            {
                UsbQueue = new USBQueue();

                if (!UsbQueue.Open())
                {
                    MessageBox.Show(
                        "There was a problem connecting to the barcode reader device driver.\n\n Barcode Readers may be unavailable.");
                }
            }
        }
        public void SelectStudent(StudentModel model)
        {

            if (model != null)
            {
                model.IsManualEntry = true;

                ScanLocation location = null;

                if (ViewModel.SwipeMode == SwipeMode.Location && !ViewModel.Dismissal)
                {
                    var locationString = ViewModel.LocationList.SelectedItem;

                    location = ViewModel.Locations.FirstOrDefault(l => l.RoomName == locationString);
                }
                else
                {

                    location =
                        ViewModel.Locations.FirstOrDefault(
                            l =>
                                l.AttendanceCode == ViewModel.AttendanceCodeList.SelectedItem &&
                                l.PeriodCode == ViewModel.PeriodList.SelectedItem) ??
                        ViewModel.Locations.FirstOrDefault(
                            l =>
                                l.RoomName == ViewModel.LocationList.SelectedItem);
                }

                //ViewModel.SelectedStudent = null;
                ViewModel.SearchResults.Clear();
                this.SearchBox.Clear();
                Keyboard.Focus(SearchBox);

                //todo: allow lane selection

                ViewModel.RecordScan(model, location, Lane.Right, true);


               
                OnManualEntry(model);
                
            }
        }

        public void SelectPerson(PersonModel model)
        { 

            if (model != null)
            {
                model.IsManualEntry = true;

                ScanLocation location = null;

                if (ViewModel.SwipeMode == SwipeMode.Location && !ViewModel.Dismissal)
                {
                    var locationString = ViewModel.LocationList.SelectedItem;

                    location = ViewModel.Locations.FirstOrDefault(l => l.RoomName == locationString);
                }
                else
                {

                    location =
                        ViewModel.Locations.FirstOrDefault(
                            l =>
                                l.AttendanceCode == ViewModel.AttendanceCodeList.SelectedItem &&
                                l.PeriodCode == ViewModel.PeriodList.SelectedItem) ??
                        ViewModel.Locations.FirstOrDefault(
                            l =>
                                l.RoomName == ViewModel.LocationList.SelectedItem);
                }

                //ViewModel.SelectedStudent = null;
                ViewModel.SearchResults.Clear();
                this.SearchBox.Clear();
                Keyboard.Focus(SearchBox);

                //todo: allow lane selection
                ViewModel.RecordScan(model, location, Lane.Right, true);


            }
        }
        private void OnManualEntry(StudentModel data)
        {
            if (ViewModel.SwipeMode == SwipeMode.Entry && !ViewModel.MarkAllPresent)
                ViewModel.RaiseFinePopup(data, null);
        }

        async void ReadScans()
        {
            int iSleepTime = 0;

            int countEx = 0;
            while (true)
            {
                
               
                    // Wait until we have data
                    var bDataReady = UsbQueue.waitForData(300);
                    if (bDataReady)
                    {
                        // If we have data then we need to read it from the queue
                        // and output it to the console.
                        var sDeviceData = UsbQueue.getDeviceData(40);
                        if (sDeviceData != "")
                        {
                            var lane = Lane.Right;

                            char etx = (char) 14;

                            byte[] asciiBytes = Encoding.ASCII.GetBytes(sDeviceData);
                            if (sDeviceData.Contains(etx))
                            {
                                lane = Lane.Left;
                                sDeviceData = sDeviceData.Substring(1, sDeviceData.Length - 1);
                            }

                            // output the data to the screen
                            try
                            {
                                //var task = 
                                //task.Wait();
                                
                                var student = await LocalStorage.SearchByBarcodeAsync(sDeviceData.Trim());

                                if(student == null)
                                    Logger.Warn($"Student Lookup failed on barcode {sDeviceData.Trim()}");

                                await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => OnBarcodeResult(new Tuple<PersonModel, Lane, bool, string>(student, lane, false, sDeviceData.Trim()))));
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(string.Format("Student Lookup failed on barcode {0}", sDeviceData), ex);
                            }
                            //MessageBox.Show("Data Read [ " + sDeviceData + " ]");
                        }
                    }

                    // Increment wether we had a timeout or we had a good read
                    iSleepTime++;
             
            }

        }

        private void OnBarcodeResult(Tuple<PersonModel,Lane, bool, String> data)
        {
            var barcode = data.Item4;
            var isManualEntry = data.Item3;



              ScanLocation  location = ViewModel.Locations.FirstOrDefault(x =>
                        x.AttendanceCode == ViewModel.AttendanceCodeList.SelectedItem &&
                        x.PeriodCode == ViewModel.PeriodList.SelectedItem);


              if (ViewModel.SwipeMode == SwipeMode.Location && !ViewModel.Dismissal)
              {
                  var locationString = ViewModel.LocationList.SelectedItem;

                  location = ViewModel.Locations.FirstOrDefault(l => l.RoomName == locationString);
              }

          
            if (data.Item1 != null)
            {
                var student = data.Item1;
                var lane = data.Item2;

                ViewModel.RecordScan(student, location, lane);
            }
            else
            {
               
                PlaySound(badScan);

                ViewModel.Swipe(new Scan() { SwipeLane = data.Item2, InvalidScan = true, Barcode = barcode, StudentName = string.Format("{0} INVALID SCAN", barcode), ScanLocation = location, EntryTime = DateTime.Now });
                //ViewModel.Barcode = string.Empty;
            }


            if (isManualEntry)
                ViewModel.Barcode = string.Empty;
        }


        private void PlaySound(string uri)
        {
                
            try
            {
                using (var player = new SoundPlayer(uri))
                {
                    player.Play();
                }
            }
            catch (Exception ex) { Logger.Error(ex); }
        }


        void SyncImages()
        {

            var images = Client.GetStudentImageNames();

            RemoteStorage.DownloadStudentImages(images.ToArray());
        }

        private void ItemContainerGeneratorOnStatusChanged(object sender, EventArgs eventArgs)
        {
            if (StudentsList.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                var index = StudentsList.SelectedIndex;
                if (index >= 0)
                {
                    var item = StudentsList.ItemContainerGenerator.ContainerFromIndex(index) as ListBoxItem;
                    if (item != null) item.Focus();
                }
            }
        }

        private void OnSearchResult(PersonModel[] list)
        {
            /*if (!list.Any())elapsed 
            {
                = new List<StudentModel>(new[] { new StudentModel() { StudentNumber = "No Students Found" } }).ToArray();
                return;
            }*/
            foreach(var e in list)
            {
                try
                {
                    string display = "Present";

                    Tuple<int, DateTime, string> sqlScan = null;

                    if (e.GetType() == typeof(StudentModel))
                    {
                        var redisScan = Scans.GetByStudent(e.UniqueId, DateTime.Today);

                        if (redisScan == null)
                            sqlScan = LocalStorage.CheckEntrySwipe((e as StudentModel).StudentId);

                        if (redisScan != null)
                        {
                            //TODO: add present/tardy selector text
                            //if(redisScan.EntryTime.TimeOfDay > )
                            e.CurrentStatus = string.Format("{0} ({1})", display.ToUpper(),
                                redisScan.EntryTime.ToString("hh:mm:ss tt"));
                        }
                        else if (sqlScan != null)
                        {
                            //TODO: add present/tardy selector text
                            //if(redisScan.EntryTime.TimeOfDay > )
                            e.CurrentStatus = string.Format("{0} ({1})", display.ToUpper(),
                                sqlScan.Item2.ToString("hh:mm:ss tt"));
                        }
                        else
                        {
                            e.CurrentStatus = "Absent".ToUpper();
                        }
                    }

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
         
            ViewModel.SearchResults = new ReactiveList<PersonModel>(list);
            //if (ViewModel.StudentsFound.Any())
                //ViewModel.SelectedStudent = ViewModel.StudentsFound[0];
        }
       
        IObservable<string> Enter(Tuple<Key, string> @event)
        {
            if (@event.Item1 == Key.Enter)
            {
                return Observable.Return(@event.Item2);
            }
            
            return Observable.Empty(string.Empty);
        }

        public ReactiveCommand<object> TransitionToScan { get; private set; }

        public ScanStationViewModel ViewModel
        {
            get { return (ScanStationViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(ScanStationViewModel), typeof(ScanStation), new PropertyMetadata(null));

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (ScanStationViewModel)value; }
        }

        public static DependencyObject GetScrollViewer(DependencyObject o)
        {
            if (o is ScrollViewer)
            { return o; }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
            {
                var child = VisualTreeHelper.GetChild(o, i);

                var result = GetScrollViewer(child);
                if (result == null)
                {
                    continue;
                }
                else
                {
                    return result;
                }
            }

            return null;
        }

        private void StudentsList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = e.Source as ListView;
            if (item?.SelectedItem != null)
            {
                SelectStudent(item.SelectedItem as StudentModel);
            }
        }

        private void StudentsList_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            //((ListViewItem)sender).Content as
            try
            {
                if (this.ViewModel.SwipeMode == SwipeMode.ClassroomTardy)
                {
                    var item = ((ListView) e.Source).SelectedItem as StudentModel;
                    if (item != null)
                    {
                        SelectStudent(item);
                    }
                }
            }catch(Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }
}
