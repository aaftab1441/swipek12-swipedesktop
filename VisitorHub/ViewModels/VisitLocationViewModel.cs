using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Printing;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using ReactiveUI;
using SwipeDesktop.Common;
using SwipeDesktop.Interfaces;
using SwipeDesktop.Models;
using SwipeDesktop.Storage;
using SwipeDesktop.Views;
using System.Linq;
using Swipe.Domain;
using SwipeDesktop.Api;
using Telerik.Windows.Controls;


namespace SwipeDesktop.ViewModels
{
    public class VisitLocationViewModel : ReactiveObject, IScanViewModel
    {
        private VisitStorage _storage;
        private VisitorScanViewModel _visitorScanView;
        private MainViewModel _mainView;
        private RemoteStorage Api;
        private LocalStorage LocalStorage;

        public VisitorScanViewModel VisitorScanView
        {
            get { return _visitorScanView; }
            //set { this.RaiseAndSetIfChanged(ref f_name, value); }
        }

        public PrintQueue SelectedQueue
        {
            get { return Application.Current.Properties["TempIdPrintQueue"] as PrintQueue; }
            set { Application.Current.Properties["TempIdPrintQueue"] = value; }
        }


        bool _isProcessing;
        public bool IsProcessing
        {
            get { return _isProcessing; }
            set { this.RaiseAndSetIfChanged(ref _isProcessing, value); }
        }

        public void CompletedVisit(MainViewModel main)
        {
            _visitorScanView.VisitEntryDate = DateTime.Now;
            _visitorScanView.VisitLocation = SelectedLocation;
            PrintPass();

            LocalStorage.InsertVisit(_visitorScanView.CurrentScan);
            _storage.InsertObject(_visitorScanView.CurrentScan);

            _mainView.CurrentView = new WelcomeViewModel(main) { SchoolName = Settings.Default.School };
        }

        public VisitLocationViewModel(MainViewModel main, IEnumerable<string> locations, VisitStorage storage, RemoteStorage api, LocalStorage localStorage)
        {
            Api = api;
            LocalStorage = localStorage;
            //SelectedQueue = Application.Current.Properties["TempIdPrintQueue"] as PrintQueue;
            _storage = storage;
            _mainView = main;
            _visitorScanView = (VisitorScanViewModel)main.CurrentView;

            _locations = new ObservableCollection<VisitLocation>(locations.Select(x=>new VisitLocation(this,x)));

            TransitionToScan = this.WhenAny(x => x.VisitorScanView, x => x.Value != null).ToCommand();

            this.WhenAnyObservable(x => x.TransitionToScan).Subscribe(x => { _mainView.CurrentView = _visitorScanView; });

            CompleteVisit = this.WhenAny(x => x.SelectedLocation, x => !string.IsNullOrEmpty(x.Value)).ToCommand();

            this.WhenAnyObservable(x => x.CompleteVisit).ObserveOn(new DispatcherScheduler(Dispatcher.CurrentDispatcher, DispatcherPriority.DataBind)).Subscribe(async x =>
            {
                //await CheckAlerts();

                bool _flagged = await FinalizeVisit();

                if (_flagged)
                {
                    _mainView.CurrentView = new SuspendViewModel(main, this) { SchoolName = Settings.Default.School };
                }
                else
                {

                    CompletedVisit(main);
                }

                //var offender = WatchdogHelper.Search(_visitorScanView);

                //if (!offender)
                //{
                //    var status = Notifications.SendNotification(_visitorScanView);

                //}

            });

            TransitionToWelcome = this.WhenAny(x => x.VisitorScanView, x => x.Value != null).ToCommand();
            this.WhenAnyObservable(x => x.TransitionToWelcome).Subscribe(x => { _mainView.CurrentView = new WelcomeViewModel(main); });

        }
     
        string _selectedLocation;
        public string SelectedLocation
        {
            get { return _selectedLocation; }
            set { this.RaiseAndSetIfChanged(ref _selectedLocation, value); }
        }

        public ReactiveCommand<object> SetLocation { get; private set; }

        public ReactiveCommand<object> TransitionToWelcome { get; private set; }
       
        public ReactiveCommand<object> CompleteVisit { get; private set; }

        /*
        public async Task CheckAlerts(bool checkRegistry = true)
        {


            var entry = new Messages.VisitorEntry()
            {
                EntryTime = _visitorScanView.VisitEntryDate,
                Location = _visitorScanView.VisitLocation,
                SchoolId = Settings.Default.SchoolId,
                VisitorFirstName = _visitorScanView.FirstName,
                VisitorState = _visitorScanView.State,
                VisitorLastName = _visitorScanView.LastName,
                VisitorDob = _visitorScanView.DateOfBirth ?? DateTime.MinValue,
                CheckOffender = checkRegistry
            };

            await Api.SendVisitEntryAsync(entry);
        }
        */
        public async Task<bool> FinalizeVisit()
        {
         

            var entry = new Messages.VisitorEntry()
            {
                EntryTime = _visitorScanView.VisitEntryDate,
                Location = SelectedLocation,
                SchoolId = Settings.Default.SchoolId,
                VisitorFirstName = _visitorScanView.FirstName,
                VisitorState     = _visitorScanView.State,
                VisitorLastName = _visitorScanView.LastName,
                VisitorDob = _visitorScanView.DateOfBirth ?? DateTime.MinValue,
                CheckOffender = true
            };

            return await Api.SendVisitEntryAsync(entry);
        }

        public ReactiveCommand<object> TransitionToScan { get; private set; }

        private ObservableCollection<VisitLocation> _locations = new ObservableCollection<VisitLocation>();
        public ObservableCollection<VisitLocation> Locations
        {
            get { return _locations; }
        }

        public void AddLocation(string name)
        {
            Locations.Add(new VisitLocation(this,name));
            this.RaiseAndSetIfChanged(ref _locations, Locations);
        }

        public void AddLocations(IEnumerable<string> locations)
        {
            this.RaiseAndSetIfChanged(ref _locations, new ObservableCollection<VisitLocation>(locations.Select(x => new VisitLocation(this, x))));
        }

        public void PrintPass()
        {
            var pass = new VisitorPass();

            var printModel = new PrintModel<VisitorScanViewModel>(_visitorScanView);
          
            printModel.SchoolName = Settings.Default.School;

            pass.DataContext = printModel;

            try
            {
                var queue = SelectedQueue;

                if (queue != null)
                {
                    var content = new PageContent();
                    var page = new FixedPage();
                    var dialog = new PrintDialog();
                    dialog.PrintQueue = queue;
                    dialog.PrintTicket = new PrintTicket();

                    dialog.PrintTicket.PageMediaSize = new PageMediaSize(215, 370);
                    dialog.PrintTicket.PageOrientation = PageOrientation.Portrait;
                    dialog.PrintTicket.PageBorderless = PageBorderless.Borderless;
                    dialog.PrintTicket.OutputQuality = OutputQuality.Photographic;

                    if (queue.Name.ToUpper().Contains("DYMO") || queue.Name.ToUpper().Contains("STAR") || queue.Name.ToUpper().Contains("XPS"))
                    {
                        var rotate = new RotateTransform(90);
                        pass.LayoutTransform = rotate;
                        pass.UpdateLayout();
                    }
                    else if (queue.Name.ToUpper().Contains("CITIZEN"))
                    {
                        page.Margin = new Thickness(10, 0, 0, 0);
                        dialog.PrintTicket.PageOrientation = PageOrientation.Landscape;

                        //var rotate = new RotateTransform(90);
                        //pass.LayoutTransform = rotate;

                        //pass.UpdateLayout();
                    }
                    else if (queue.Name.ToUpper().Contains("PRT"))
                    {
                        page.Margin = new Thickness(0, 25, 0, 0);
                        dialog.PrintTicket.PageOrientation = PageOrientation.Landscape;
                    }

                    var document = new FixedDocument();
                    document.DocumentPaginator.PageSize = new Size(dialog.PrintableAreaWidth, dialog.PrintableAreaHeight);
                    document.PrintTicket = dialog.PrintTicket;

                    page.Width = document.DocumentPaginator.PageSize.Width;
                    page.Height = document.DocumentPaginator.PageSize.Height;
                    page.Children.Add(pass);
                    document.Pages.Add(content);
                    ((IAddChild)content).AddChild(page);
                    dialog.PrintDocument(document.DocumentPaginator, "Visitor Pass");

                    #region old print visual code
                   
                    #endregion

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
    }
}
