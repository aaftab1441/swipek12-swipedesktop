using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Printing;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using Autofac;
using log4net;
using ReactiveUI;
using SwipeDesktop.Api;
using SwipeDesktop.Common;
using SwipeDesktop.Interfaces;
using SwipeDesktop.Models;
using SwipeDesktop.Storage;
using SwipeDesktop.Views;

namespace SwipeDesktop.ViewModels
{
    public class StaffExitViewModel: ReactiveObject, IHostedViewModel
    {
        private static readonly string goodScan = Settings.Default.SoundsFolder + "\\ding.wav";
        private static readonly string badScan = Settings.Default.SoundsFolder + "\\badscan.wav";
        private StaffScanStorage StaffScans;
        private InOutStorage LocationScans;

        private static readonly ILog Logger = LogManager.GetLogger(typeof(StaffEnterViewModel));
        public ReactiveCommand<object> TransitionToWelcome { get; private set; }

        public ReactiveCommand<object> SearchByStaffId { get; private set; }
        
        public ReactiveCommand<object> SearchByName { get; private set; }

        public ReactiveCommand<object> FindByCriteria { get; private set; }
        public ReactiveCommand<object> CompleteEnter { get; private set; }

        public PrintQueue SelectedQueue
        {
            get { return Application.Current.Properties["PassPrintQueue"] as PrintQueue; }
            set { Application.Current.Properties["PassPrintQueue"] = value; }
        }


        string _title = "...";
        public string Title
        {
            get { return _title; }
            set { this.RaiseAndSetIfChanged(ref _title, value); }
        }

        public bool IsSearchByBarcode
        {
            get { return SearchLabel == "Type or scan your staff ID:"; }
        }


        public bool IsSearchByName
        {
            get { return SearchLabel == "Staff name:"; }
        }

        MainViewModel _main;
        public MainViewModel Main
        {
            get { return _main; }
            //set { this.RaiseAndSetIfChanged(ref f_name, value); }
        }

        private LocalStorage Storage;

        public StaffExitViewModel(MainViewModel main)
        {
            Storage = App.Container.Resolve<LocalStorage>();
            StaffScans = App.Container.Resolve<StaffScanStorage>();
            LocationScans = App.Container.Resolve<InOutStorage>();
            SearchComplete = false;

            _main = main;

            SearchLabel = "Type or scan your staff ID:";
            SetFocus = true;
            CompleteEnter = this.WhenAny(x => x.Main, x => x != null).ToCommand();
            this.WhenAnyObservable(x => x.CompleteEnter).Subscribe(async x =>
            {
                var item = x as PersonModel;

                //var student = await Storage.SearchByBarcodeAsync(item.IdNumber);

                OnBarcodeResult(item);

                Main.CurrentView = new WelcomeViewModel(_main);
            });

            TransitionToWelcome = this.WhenAny(x=>x.Main, x=>x.Value != null).ToCommand();
            this.WhenAnyObservable(x => x.TransitionToWelcome).Subscribe(x => { Main.CurrentView = new WelcomeViewModel(Main); });

            SearchByStaffId = this.WhenAny(x => x.SearchLabel, x => x.Value != "Type or scan your staff ID:").ToCommand();
            this.WhenAnyObservable(x => x.SearchByStaffId).Subscribe(x =>
            {
                SearchLabel = "Type or scan your staff ID:";
            });
          
            SearchByName = this.WhenAny(x => x.SearchLabel, x => x.Value != "Staff name:").ToCommand();
            this.WhenAnyObservable(x => x.SearchByName).Subscribe(x =>
            {
                SearchLabel = "Staff name:";
            });

            FindByCriteria = this.WhenAny(x => x.Criteria, x => !string.IsNullOrEmpty(x.Value)).ToCommand();
            this.WhenAnyObservable(x => x.FindByCriteria).Subscribe(async x =>
            {
                PersonModel[] staff = null;

                if (IsSearchByBarcode)
                {
                    var found = await Storage.SearchByBarcodeAsync(Criteria);
                    if (found != null)
                    {
                        //students = new[] {found};
                        OnBarcodeResult(found);
                        SearchComplete = false;
                        Main.CurrentView = new WelcomeViewModel(Main);
                        //SearchResults = new ReactiveList<PersonModel>(students);
                    }
                    else
                    {
                        MessageBox.Show($"Could not find a staff member by {Criteria}");
                    }
                }

                if (IsSearchByName)
                {
                    var data = await Storage.SearchStudentsAsync(Criteria);
                    OnSearchResult(data);

                    SearchComplete = true;
                }

                Criteria = string.Empty;
            });


            this.Title = $"Staff Exit";
           

        }


        private void OnSearchResult(PersonModel[] list)
        {
            var people = list.Where(x => x.GetType() == typeof(PersonModel));
            /*if (!list.Any())
            {
                = new List<StudentModel>(new[] { new StudentModel() { StudentNumber = "No Students Found" } }).ToArray();
                return;
            }*/
            foreach (var e in people)
            {
                try
                {
                    string display = "Present";

                    Tuple<int, DateTime, string> sqlScan = null;

                    if (e.GetType() == typeof(PersonModel))
                    {
                      
                            e.CurrentStatus = "Absent".ToUpper();
                       
                    }

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }

            SearchResults = new ReactiveList<PersonModel>(people);
           
        }


        
        private void OnBarcodeResult(PersonModel data)
        {
            bool allowPrint = false;

            bool splitPages = Settings.Default.SelectedPassType == "Separate Pass and Alert";

            var staff = data;

            var queue = SelectedQueue;

            PrintCapabilities capabilities = queue.GetPrintCapabilities(queue.DefaultPrintTicket);
            var sz = new Size(capabilities.PageImageableArea.ExtentWidth, capabilities.PageImageableArea.ExtentHeight);

            var scan = new StaffScan()
            {
                AlreadySwiped = false,
                SwipeLane = Lane.Right,
                Barcode = staff.IdNumber,
                StudentName = staff.DisplayName,
                ScanImage = staff.Image,
                EntryTime = DateTime.Now,
                PersonId = staff.PersonId,
                IsLeavingLocation = true,
                IsKiosk = true
            };

            Swipe(scan);
  
            PlaySound(goodScan);

            if (allowPrint)
            {
                var passes = GeneratePass(scan, true, sz, new RotateTransform(90));

                var pages = new List<FixedPage>();

                if (passes != null && passes.Length > 0)
                {
                    pages.AddRange(CreatePages(passes, sz));
                    /*
                    if (splitPages)
                    {
                        pages.AddRange(CreatePages(passes, sz));
                    }
                    else
                    {
                        pages.Add(CreateStackedPage(passes, sz));
                    }
                    */
                }

                if (pages.Any())
                {
                    Application.Current.Dispatcher.Invoke(() => PrintDocument(queue, pages.ToArray(), "Swipe Print"), DispatcherPriority.DataBind);
                }
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


        public void Swipe(StaffScan scan)
        {
            //var startTime = Default.SchoolSettings.DayStartTime.TimeOfDay;
            scan.IsStaffScan = true;

            bool recordSwipe = !(scan.StudentName.Contains("INVALID SCAN") || scan.StudentName.Contains("ALREADY SCANNED") || scan.AlreadySwiped);

            if (recordSwipe)
            {
                scan.EntryStatus = "PRE";
                scan.AttendanceCode = scan.EntryStatus;
                scan.MarkAllPresentMode = false;

                /*
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
                */
                scan.Room = "Staff Scan";

                var currentSwipeTime = scan.EntryTime;
                var locationsScanned = LocationScans.GetItemsByStudent(scan.Room, currentSwipeTime, scan.Barcode);

                StaffScans.InsertObject(scan.StaffScanModel);

            }
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


        public Control[] GeneratePass(Scan model, bool printEnabled, Size paperSize, RotateTransform rotate)
        {
            var list = new List<Control>();

            var pass = new TardyPass(false);

            if (Settings.Default.SuppressIdOnPass)
                pass.BarcodeLabel.Visibility = Visibility.Hidden;

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

            list.Insert(0, pass);

            return list.ToArray();
        }


        ScaleTransform Scale()
        {
            var scaleFactor = Settings.Default.PrintScaleFactor;

            if (scaleFactor != 0)
                return new ScaleTransform(scaleFactor, scaleFactor);

            return null;
        }

        private void PlaySound(string uri)
        {
            try
            {
                var player = new SoundPlayer(uri);
                player.LoadCompleted += delegate {
                    player.Play();
                };
                player.LoadAsync();
            }
            catch (Exception ex) { Logger.Error(ex); }
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

        bool _isProcessing;
        public bool IsProcessing
        {
            get { return _isProcessing; }
            set { this.RaiseAndSetIfChanged(ref _isProcessing, value); }
        }

        bool _setFocus;
        public bool SetFocus
        {
            get { return _setFocus; }
            set { this.RaiseAndSetIfChanged(ref _setFocus, value); }
        }

        string _criteria;
        public string Criteria
        {
            get { return _criteria; }
            set { this.RaiseAndSetIfChanged(ref _criteria, value); }
        }

        string _searchLabel;
        public string SearchLabel
        {
            get { return _searchLabel; }
            set { this.RaiseAndSetIfChanged(ref _searchLabel, value); }
        }

        bool _searchComplete;
        public bool SearchComplete
        {
            get { return _searchComplete; }
            set { this.RaiseAndSetIfChanged(ref _searchComplete, value); }
        }



    }
}
