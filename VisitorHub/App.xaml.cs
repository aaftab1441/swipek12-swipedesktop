using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Printing;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Autofac;
using Common;
using log4net;
using log4net.Repository.Hierarchy;
using ReactiveUI;
using ServiceStack.Configuration;
using ServiceStack.Redis;
using SwipeDesktop.Api;
using SwipeDesktop.Common;
using SwipeDesktop.Config;
using SwipeDesktop.Interop;
using SwipeDesktop.Models;
using SwipeK12;
using SwipeK12.NextGen.ReadServices.Messages;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace SwipeDesktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Mutex mutex;

        // Application level variable key values
        public const string APP_KEY_CARD_ID = "CURRENT_CARD_ID";
        public const string APP_KEY_SCHOOL_ID = "DEFAULT_SCHOOL_ID";
        public const string APP_KEY_STUDENT_ID = "DEFAULT_STUDENT_ID";
        public const string APP_KEY_TEACHER_ID = "DEFAULT_TEACHER_ID";

        // Application level constants
        public const string APP_CARD_TYPE_STUDENT = "Student";
        public const string APP_CARD_TYPE_TEACHER = "Teacher";
        public const string APP_CARD_TYPE_OTHER = "Other";
        public const string APP_CARD_ORIENTATION_PORTRAIT = "Portrait";
        public const string APP_CARD_ORIENTATION_LANDSCAPE = "Landscape";
        public const string APP_CARD_FRONT = "Front";
        public const string APP_CARD_BACK = "Back";

        public const string APP_DB_FIELD_CARD_ID = "CardID";
        public const string APP_DB_FIELD_CARD_NAME = "CardName";
        public const string APP_DB_FIELD_SCHOOL_ID = "SchoolID";
        public const string APP_DB_FIELD_SCHOOL_NAME = "SchoolName";
        public const string APP_DB_FIELD_PERSON_ID = "PersonID";
        public const string APP_DB_FIELD_DISPLAY_NAME = "DisplayName";

        public const int APP_DEFAULT_TEXTBOX_WIDTH = 120;
        public const double APP_CARD_LONG_SIDE = 324.77480314960627;
        public const double APP_CARD_SHORT_SIDE = 215;

        public const string APP_FONT_BARCODE = "/Fonts/#Free 3 of 9 Extended";

        public const string APP_CONFIG_KEY_IMAGE_FOLDER = "PHOTO_IMAGE_FOLDER";

        static Process RedisProcess = new Process();

        private static readonly ILog Logger = LogManager.GetLogger(typeof(App));

        private static readonly StationMode StationMode = (StationMode)Enum.Parse(typeof(StationMode), ConfigurationManager.AppSettings["mode"]);
        private static readonly int Interval = int.Parse(ConfigurationManager.AppSettings["syncInterval"]);

        public static IContainer Container { get; set; }

        private static int swipes = 0;

        private bool GrantAccess(string fullPath)
        {
            DirectoryInfo dInfo = new DirectoryInfo(fullPath);
            DirectorySecurity dSecurity = dInfo.GetAccessControl();
            dSecurity.AddAccessRule(new FileSystemAccessRule(@"BUILTIN\Users", FileSystemRights.FullControl,
                                                             InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
                                                             PropagationFlags.InheritOnly, AccessControlType.Allow));
            dInfo.SetAccessControl(dSecurity);
            return true;

        }

        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Logger.Error("Handler Caught", e);
            Logger.Error(e.StackTrace);
            Logger.Error(string.Format("Runtime terminating: {0}", args.IsTerminating));
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += MyHandler;
            
            bool created;
            mutex = new Mutex(false, "SwipeDesktop-{912345}", out created);
            if (!created)
            {
                Application.Current.Properties["faulted"] = true;
                MessageBoxResult result = MessageBox.Show("It appears the Swipe Desktop is still running in the background. \n\nPlease check that no other instances of Swipe Desktop are running.", "Error", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK, System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                if (result == MessageBoxResult.OK)
                {
                    Application.Current.Shutdown();
                }
                return;
            }

            this.DispatcherUnhandledException += (o, args) =>
            {
                MessageBox.Show(args.Exception.Message + ".");
                Logger.Error(args.Exception);
                args.Handled = true;
            };

            var swipeIsRunning = Process.GetProcessesByName("SwipeDesktop");

            if (swipeIsRunning.Length > 1)
            {
                //throw new ApplicationException("Another instance of Swipe is running.");
            }
            
            log4net.GlobalContext.Properties["hostname"] = Environment.MachineName;
            log4net.Config.XmlConfigurator.Configure();

            var folder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var dbPath = folder + "\\App_Data";
            var logPath = folder + "\\Logs";

            AppDomain.CurrentDomain.SetData("DataDirectory", dbPath);

            Logger.Warn("Database Path: " + dbPath);

            Logger.Warn("API URL: " + Settings.Default.JsonUrl);

            Logger.Warn("Remote Database: " + Settings.Default.SqlMasterIp);

            var user = string.Format(@"{0}\{1}", Environment.UserDomainName, Environment.UserName);

            Logger.Warn("User: " + user + " School Id: " + Settings.Default.SchoolId);

            tryUpgradeSettings();

            try
            {
                GrantAccess(dbPath);
                GrantAccess(logPath);
            }
            catch(Exception ex)
            {
                /*no-op */
                Logger.Error("Could not grant access.", ex);
            }

            this.Exit += (o, args) =>
            {
                //if (RedisProcess!= null)
                //    RedisProcess.Kill();

           
                Settings.Default.Save();
			
            };

         
            try
            {
                // Create temp folder
                if (!Directory.Exists(Settings.Default.ImagesFolder))
                {
                    Directory.CreateDirectory(Settings.Default.ImagesFolder);
                }

            }
            catch (Exception ex)
            {
                Logger.Error("There was a problem starting the application. ", ex);
                MessageBox.Show("There was a problem starting the application. " + ex.Message);
                Application.Current.Shutdown();
            }


            var redisIsRunning = Process.GetProcessesByName("redis-server").IsRunning();

            if (!redisIsRunning)
            {
                redisIsRunning = StartRedis();
            }
            else
            {
                RedisProcess = Process.GetProcessesByName("redis-server").GetProcess();
                RedisProcess.Kill();

                redisIsRunning = StartRedis();
            }

            if (!redisIsRunning)
            {
                MessageBox.Show(
                    "There was a problem starting the application. The Database does not appear to be running.");

                Logger.Error("There was a problem starting the application.  The Database does not appear to be running.");

                throw new RedisException("The Redis cache was not running.  Please ensure the Redis Windows Service is installed and running.  The application will exit.");
            }
            try
            {
                var printers =
                        new LocalPrintServer().GetPrintQueues(new[] {EnumeratedPrintQueueTypes.Local});

                Properties["Printers"] = printers;

               var printer =
                    (printers as PrintQueueCollection).FirstOrDefault(x => x.Name.Contains(Settings.Default.PassPrinter)) ??
                    (printers as PrintQueueCollection).FirstOrDefault(x => x.Name == "Microsoft XPS Document Writer");

                Properties["PassPrintQueue"] = printer;


                var printer1 =
                    (printers as PrintQueueCollection).FirstOrDefault(x => x.Name.Contains(Settings.Default.TempIdPrinter)) ??
                    (printers as PrintQueueCollection).FirstOrDefault(x => x.Name == "Microsoft XPS Document Writer");

                Properties["TempIdPrintQueue"] = printer1;


                var printer2 =
                    (printers as PrintQueueCollection).FirstOrDefault(x => x.Name.Contains(Settings.Default.PvcPrinter)) ??
                    (printers as PrintQueueCollection).FirstOrDefault(x => x.Name == "Microsoft XPS Document Writer");

                Properties["PvcPrintQueue"] = printer2;
            }
            catch (Exception ex)
            {
                Logger.Error("There was a problem enumerating printers.", ex);
            }


            if (!Directory.Exists(Settings.Default.ImagesFolder))
            {
                Directory.CreateDirectory(Settings.Default.ImagesFolder);
            }

            if (!Directory.Exists(Settings.Default.SoundsFolder))
            {
                Directory.CreateDirectory(Settings.Default.SoundsFolder);
            }

            if (StationMode == StationMode.VisitorKiosk)
            {
               
                if (ConfigurationManager.AppSettings["school"] != null)
                {
                    Settings.Default.SchoolId = int.Parse(ConfigurationManager.AppSettings["school"]);
                    Settings.Default.Save();
                }
                //var locations = File.ReadAllLines(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\locations.txt");

                //Properties["Locations"] = locations;

                SyncIntervalNotifier.SyncTriggered += SyncTriggeredVisitor;
                var uiContext = TaskScheduler.FromCurrentSynchronizationContext();

                var version = Assembly.GetEntryAssembly().GetName().Version.ToString();

#if DEBUG
                version = version + ".D";
#else
                version = version + ".R";
#endif
                Task.Run(() => DataReplicator.InOutRooms());

                var a = Task.Run(() => DataReplicator.InitRemoteServer()).ContinueWith((cw) =>
                {
                    //Task.Run(() => DataReplicator.SyncTardySwipe());

                    Task.Run(() => DataReplicator.SyncSchoolSettings()).ContinueWith((cw2) =>
                    {
                        if (cw2.Result != null)
                        {
                            Settings.Default.School = cw2.Result.SchoolName;

                            var screen = Application.Current.Windows[0];
                            if (screen != null)
                                screen.Title = $"{Settings.Default.School} (Kiosk {version})";
                        }

                    }, uiContext);
                });

                Task.Run(DataReplicator.Full);

            }
            if (StationMode == StationMode.Station)
            {
                var sqlRunning = LocalStorage.CheckConnection();

                if (!sqlRunning)
                {
                    MessageBox.Show("Swipe Desktop could not connect to the database.  Please verify SQL Server is running.");
                    Application.Current.Shutdown();
                }

                var uiContext = TaskScheduler.FromCurrentSynchronizationContext();
                string status;

                //LocalStorage.EnsureSchoolRecordExists(schoolId);
              
                SyncIntervalNotifier.SyncTriggered += SyncTriggeredNotifierSyncTriggered;


                if (CheckInternet())
                {
                    Logger.Info("Station startup sync.");

                    Task.Run(() => DataReplicator.InOutRooms());
                    Task.Run(() => DataReplicator.Groups());
                    Task.Run(() => DataReplicator.CustomStartTimes());

                    Task.Run(() => DataReplicator.TardySwipes());

                    Task.Run(() => DataReplicator.StudentLunchTable());
                    Task.Run(() => DataReplicator.StudentStartTimes());


                    var a = Task.Run(() => DataReplicator.InitRemoteServer()).ContinueWith((cw) =>
                    {
                        //Task.Run(() => DataReplicator.SyncTardySwipe());

                        Task.Run(() => DataReplicator.SyncSchoolSettings()).ContinueWith((cw2) =>
                        {
                            if (cw2.Result != null)
                            {
                                Settings.Default.School = cw2.Result.SchoolName;
                                var version = Assembly.GetEntryAssembly().GetName().Version.ToString();

                                #if DEBUG
                                    version = version + ".D";
                                #else
                                    version = version + ".R";
                                #endif

                                var screen = Application.Current.Windows[0];
                                if (screen != null)
                                    screen.Title = string.Format("{0} (Build {1})             Calculating active students...", Settings.Default.School, version);
                            }

                        }, uiContext);

                        Task.Run(() => DataReplicator.SyncAlternateIds());

                        Task.Run(() => DataReplicator.SyncTimeTable()).ContinueWith((cw2) =>
                        {
                            //DataReplicator.SyncStudentLunchTable();
                            DataReplicator.RemoteSnapshot();
                        }).ContinueWith((cw3) =>
                        {
                            try
                            {
                                var db = App.Container.Resolve<LocalStorage>();
                                var stats = db.DatabaseStats();

                                //DataReplicator.Full();
                                var screen = Application.Current.Windows[0];
                                var indexOfVersionTextEnds = (screen.Title.IndexOf(")"));
                                if (screen != null) {
                                    screen.Title = screen.Title.Remove(indexOfVersionTextEnds+1, (screen.Title.Length - indexOfVersionTextEnds)-1);

                                    screen.Title += $"               Active Students: {stats.Single(i => i.Item1 == "Total Active Students").Item2}";
                                }

                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex);
                            }
                        }, uiContext);

                    });

                    Task.Run(() => DataReplicator.StationAlerts(true));

                    Task.Run(() => DataReplicator.Consequences());

                    Thread.Sleep(2000);
                }
                else
                {
                    Logger.Error("Network not connected.");
                }

            }

          
        }
     
        bool CheckInternet()
        {
            string status;
            var connected = InternetAvailability.IsInternetAvailable(out status);

            return connected;
        }

        async Task<bool> ApiIsNotAvailable()
        {
    
            return await InternetAvailability.ApiIsNotAvailable(Settings.Default.JsonUrl);

        }
        void tryUpgradeSettings()
        {

           
            try
            {
                if (Settings.Default.CallUpgrade)
                {
                    Logger.Warn("Upgrading Settings");

                    Settings.Default.Upgrade();
                    Settings.Default.CallUpgrade = false;
                    Settings.Default.Save();
                }
            }
            catch (ConfigurationErrorsException ex)
            {
                Logger.Error("Failed to Upgrade Settings");

                string fileName = "";
                if (!string.IsNullOrEmpty(ex.Filename))
                {
                    fileName = ex.Filename;
                }
                else
                {

                    var innerException = ex.InnerException as System.Configuration.ConfigurationErrorsException;
                    if (innerException != null && !string.IsNullOrEmpty(innerException.Filename))
                    {
                        fileName = innerException.Filename;
                    }
                }

                try
                {
                    if (System.IO.File.Exists(fileName))
                    {
                        System.IO.File.Delete(fileName);
                    }
                }
                catch (Exception file_ex)
                {
                    //Settings.Default.Upgrade();
                    Logger.Error(file_ex);
                }

            }
            catch (Exception ex)
            {
                //Settings.Default.Upgrade();
                Logger.Error(ex);
            }
        }

        void SyncTriggeredNotifierSyncTriggered(object sender, SyncTriggeredEventArgs e)
        {
            string status;

            MessageBus.Current.SendMessage(new Tuple<string>("NotifierSyncTriggered"));

            if (CheckInternet())
            {
                Logger.Warn("Interval Sync Triggered for data after " + e.LastSync);

                if (ApiIsNotAvailable().Wait(100))
                {
                    Logger.ErrorFormat("API not available, skipping swipe sync at {0}.", DateTime.Now);
                    return;
                }

                Logger.Warn("Syncing at hour : " + DateTime.Now.Hour);

                if (DateTime.Now.Hour < 8 || DateTime.Now.Hour > 10)
                {
                    
                    Task.Run(() => DataReplicator.Consequences());
                  
                    Task.Run(() => DataReplicator.StationAlerts(false));

                    Task.Run(() => DataReplicator.AltIds());
                    
                    Task.Run(() => DataReplicator.SyncIdCardTemplates());

                    var a = Task.Run(() => DataReplicator.InitRemoteServer()).ContinueWith((cw) =>
                    {
                        DataReplicator.SyncSchoolSettings();
                        //DataReplicator.SyncTimeTable();
                    }).ContinueWith((cw) =>
                    {
                        DataReplicator.RemoteSnapshot();
                    });
                }

                if (DateTime.Now.Hour < 8)
                {
                    
                    //Task.Run(() => DataReplicator.SnapshotSince(Interval));
                 
                }
            }
            else
            {
                Logger.Error("No network connection detected, skipping interval sync cycle.");
            }
        }

        void SyncTriggeredVisitor(object sender, SyncTriggeredEventArgs e)
        {
         
            if (CheckInternet())
            {
                Logger.Warn("Visitor Sync Triggered for data after " + e.LastSync);

                if (ApiIsNotAvailable().Wait(100))
                {
                    Logger.ErrorFormat("API not available, skipping swipe sync at {0}.", DateTime.Now);
                    return;
                }

                Logger.Warn("Visitor Syncing at hour : " + DateTime.Now.Hour);


            }
            else
            {
                Logger.Error("No network connection detected, skipping interval sync cycle.");
            }
        }

        private bool StartRedis()
        {
          
            var redisPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) +
                            @"\App_Data\redis-server.exe";

            try
            {
                RedisProcess.StartInfo.UseShellExecute = false;

                RedisProcess.StartInfo.FileName = redisPath;
                RedisProcess.StartInfo.CreateNoWindow = true;
                RedisProcess.Start();

            }
            catch (Exception e)
            {
                MessageBox.Show("There was a problem starting the application. The Database did not start.");

                Logger.Error("The Database did not start.", e);

                Application.Current.Shutdown(); 
            }

            return Process.GetProcessesByName("redis-server").IsRunning();
        }


        // ===========================================================================================================================
        // ===========================================================================================================================
        // Application Helper Methods
        // ===========================================================================================================================
        // ===========================================================================================================================

        public static string getRoot()
        {
            return Directory.GetCurrentDirectory();
        }


        public static int getCurrentCardID()
        {
            return Convert.ToInt32(Application.Current.Properties[App.APP_KEY_CARD_ID]);
        }

        public static void setCurrentCardID(int cardID)
        {
            Application.Current.Properties[App.APP_KEY_CARD_ID] = cardID;
        }


        public static int getCurrentSchoolID()
        {
            return Settings.Default.SchoolId;
        }

        public static int getCurrentIdCardStudentID()
        {
            return Convert.ToInt32(Application.Current.Properties[App.APP_KEY_STUDENT_ID]);
        }

        public static void setCurrentIdCardStudentID(int studentID)
        {
            Application.Current.Properties[App.APP_KEY_STUDENT_ID] = studentID;
        }


        public static int getCurrentIdCardTeacherID()
        {
            return Convert.ToInt32(Application.Current.Properties[App.APP_KEY_TEACHER_ID]);
        }

        public static void setCurrentIdCardTeacherID(int teacherID)
        {
            Application.Current.Properties[App.APP_KEY_TEACHER_ID] = teacherID;
        }
    }
}
