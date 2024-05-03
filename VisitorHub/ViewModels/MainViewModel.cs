using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using log4net;
using ReactiveUI;
using SwipeDesktop.Commands;
using SwipeDesktop.Common;
using SwipeDesktop.Config;
using SwipeDesktop.Cssn;
using SwipeDesktop.Interfaces;
using SwipeDesktop.Modal;
using Telerik.Windows.Controls.TreeMap;

namespace SwipeDesktop.ViewModels
{

    public interface IMainViewModel : IRoutableViewModel, IViewModel
    {
        
    }

    public class MainViewModel : ReactiveObject, IMainViewModel
    {

        private static readonly ILog Logger = LogManager.GetLogger(typeof(MainViewModel));

        private ScanUtility _scanUtility;

        public string UrlPathSegment
        {
            get { return "main"; }
        }

        public IScreen HostScreen { get; protected set; }

        IHostedViewModel _currentContent;
        public IHostedViewModel CurrentView
        {
            get { return _currentContent; }
            set { this.RaiseAndSetIfChanged(ref _currentContent, value); }
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

        private string _versionInfo = string.Empty;

        public string VersionInformation
        {
            get { return _versionInfo; }
            set
            {
                this.RaiseAndSetIfChanged(ref _versionInfo, value);
            }
        }


        string _connected = "Not Connected";
        public string Connected
        {
            get { return _connected; }
            set
            {
                this.RaiseAndSetIfChanged(ref _connected, value);
            }
        }


        void CheckInternet()
        {
            string status;
            var connected = InternetAvailability.IsInternetAvailable(out status);

            if (connected && status != Connected)
                Logger.Warn(status);

            if (!Offline)
                Connected = status;
        }

        public IPopupViewModelLocator DialogService { get; private set; }

        public MainViewModel(IScreen screen, IPopupViewModelLocator dialogService = null)
        {
            DialogService = dialogService;
            HostScreen = screen;

            _displayName = Settings.Default.DisplayText;

            VersionInformation = Assembly.GetEntryAssembly().GetName().Version.ToString();
            
            try
            {
                _scanUtility = new ScanUtility(this);
                CheckScanner();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            var text = Constants.GetScannerNameByType(_scanUtility.ScannerType) + " is connected.";


            CurrentView = new WelcomeViewModel(this);


            this.WhenAnyValue(x => x.Offline).Subscribe(x =>
            {
                if (x)
                {
                    Connected = "Station Offline";
                }
            });


            Observable.Interval(TimeSpan.FromMilliseconds(500)).Subscribe(x => CheckPaperTray());

            Observable.Interval(TimeSpan.FromSeconds(30)).ObserveOnDispatcher(DispatcherPriority.Background).Subscribe(
              async x =>
              {
                  var priorStatus = Offline;
                  Offline = await InternetAvailability.ApiIsNotAvailable(Settings.Default.JsonUrl);

                  if (priorStatus != Offline)
                      Logger.Warn("API Connected: " + Offline);
              });

            Observable.Interval(TimeSpan.FromSeconds(60)).ObserveOnDispatcher(DispatcherPriority.Background).Subscribe(x => CheckInternet());

            //Observable.Interval(TimeSpan.FromMilliseconds(5000)).Subscribe(x => CheckScanner());

            /*this.WhenAnyValue(x => x.TrayIsLoaded)
                .Select(x => TrayIsLoaded ? "The tray is loaded." : "The tray is not loaded.")
                .ToProperty(this, x => x.TrayLoadedText, out loadedText);*/

            this.WhenAnyValue(x => x.ScannerIsConnected)
                .Select(x => ScannerIsConnected ? text : "Scanner Not Connected.")
                .ToProperty(this, x => x.ScannerConnectedText, out _connectedText);

            this.WhenAnyValue(x => x.TrayIsLoaded).Subscribe(_ =>
            {
                if (TrayIsLoaded && CurrentView.GetType() == typeof(VisitorEntryViewModel))
                {
                    DoScan();
                }
            });
        }

        public void PressedEscape()
        {
            EscapeIsPressed = true;
        }

        bool _escapeIsPressed;
        public bool EscapeIsPressed
        {
            get { return _escapeIsPressed; }
            private set { this.RaiseAndSetIfChanged(ref _escapeIsPressed, value); }
        }
      
        public bool TrayIsLoaded {
            get { return trayIsLoaded; }
            private set { this.RaiseAndSetIfChanged(ref trayIsLoaded, value); }
        }

        public string TrayLoadedText
        {
            get { return loadedText.Value; }
        }

        private bool _scannerIsConnected;
        public bool ScannerIsConnected
        {
            get { return _scannerIsConnected; }
            private set { this.RaiseAndSetIfChanged(ref _scannerIsConnected, value); }
        }

        private bool _showPopup;
        public bool ShowPopup
        {
            get { return _showPopup; }
            set { this.RaiseAndSetIfChanged(ref _showPopup, value); }
        }

        public string ScannerConnectedText
        {
            get { return _connectedText.Value; }
        }

        public string DisplayText
        {
            get { return _displayName; }
        }

        public void DoScan()
        {
            //Application.Current.Dispatcher.Invoke(() =>
            //{

                CurrentView = new ProcessingViewModel(this);
                _scanUtility.DoScan();

            //}, DispatcherPriority.Input);
            //return Task.Run();
        }

        public void CheckPaperTray()
        {
            if(ScannerIsConnected)
                TrayIsLoaded = _scanUtility.CheckForPaper();
        }

        public void CheckScanner()
        {
            ScannerIsConnected = _scanUtility.CheckForScanner();
        }

        public PrintQueue DefaultPrinter
        {
            get
            {
                return ((PrintQueueCollection)Application.Current.Properties["Printers"]).FirstOrDefault(x => x.Name.Contains(Settings.Default.PassPrinter));
            }
        }

        #region viewModel state holders
        bool trayIsLoaded;
       
        readonly ObservableAsPropertyHelper<string> loadedText;
        readonly ObservableAsPropertyHelper<string> _connectedText;
        readonly string _displayName;
        #endregion
    }
}
