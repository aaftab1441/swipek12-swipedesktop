using System;
using System.Collections.Generic;
using System.Linq;
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
using Common;
using Common.Models.Access;
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
    public class LocationExitViewModel : StudentExitViewModel {
        public LocationExitViewModel(MainViewModel main) : base(main, true)
        {

            this.Title = $"Student Exit: {Settings.Default.KioskLocation}";
            IsScanLocation = true;
        }       
    }
    public class StudentExitViewModel : ReactiveObject, IHostedViewModel
    {
        private InOutStorage LocationScans;

        public ReactiveCommand<object> TransitionToWelcome { get; private set; }
        private static readonly ILog Logger = LogManager.GetLogger(typeof(StudentEnterViewModel));
        public ReactiveCommand<object> SearchByStudentId { get; private set; }
        public ReactiveCommand<object> SearchByPhoneNumber { get; private set; }
        public ReactiveCommand<object> SearchByName { get; private set; }
        public ReactiveCommand<object> FindByCriteria { get; private set; }
        public ReactiveCommand<object> CompleteExit { get; private set; }


        string _title = "Student Exit";
        public string Title
        {
            get { return _title; }
            set { this.RaiseAndSetIfChanged(ref _title, value); }
        }

        public bool IsSearchByBarcode
        {
            get { return SearchLabel == "Type or scan the student ID:"; }
        }


        public bool IsSearchByName
        {
            get { return SearchLabel == "Type the student name:"; }
        }


        bool _searchComplete;
        public bool SearchComplete
        {
            get { return _searchComplete; }
            set { this.RaiseAndSetIfChanged(ref _searchComplete, value); }
        }

        MainViewModel _main;
        public MainViewModel Main
        {
            get { return _main; }
            //set { this.RaiseAndSetIfChanged(ref f_name, value); }
        }


        bool _isScanLocation;
        public bool IsScanLocation
        {
            get { return _isScanLocation; }
            set { this.RaiseAndSetIfChanged(ref _isScanLocation, value); }
        }


        private DismissalStorage Dismissals;
        private LocalStorage Storage;

        public StudentExitViewModel(MainViewModel main, bool isLocation = false)
        {
            Storage = App.Container.Resolve<LocalStorage>();
            Dismissals = App.Container.Resolve<DismissalStorage>();
            LocationScans = App.Container.Resolve<InOutStorage>();
            _main = main;
            SearchComplete = false;

            CompleteExit = this.WhenAny(x => x.Main, x => x != null).ToCommand();
            this.WhenAnyObservable(x => x.CompleteExit).Subscribe(async x =>
            {
                var item = x as PersonModel;
                OnExit(item);
                //var student = await Storage.SearchByBarcodeAsync(item.IdNumber);

            });

            TransitionToWelcome = this.WhenAny(x => x.Main, x => x.Value != null).ToCommand();
            this.WhenAnyObservable(x => x.TransitionToWelcome).Subscribe(x => { Main.CurrentView = new WelcomeViewModel(Main); });

            SearchLabel = "Type or scan the student ID:";

            TransitionToWelcome = this.WhenAny(x => x.Main, x => x.Value != null).ToCommand();
            this.WhenAnyObservable(x => x.TransitionToWelcome).Subscribe(x => { Main.CurrentView = new WelcomeViewModel(Main); });
            
            SearchByStudentId = this.WhenAny(x => x.SearchLabel, x => x.Value != "Type or scan the student ID:").ToCommand();
            this.WhenAnyObservable(x => x.SearchByStudentId).Subscribe(x =>
            {
                SearchLabel = "Type or scan the student ID:";
            });

            SearchByPhoneNumber = this.WhenAny(x => x.SearchLabel, x => x.Value != "Type the student phone number:").ToCommand();
            this.WhenAnyObservable(x => x.SearchByPhoneNumber).Subscribe(x =>
            {
                SearchLabel = "Type the student phone number:";
            });

            SearchByName = this.WhenAny(x => x.SearchLabel, x => x.Value != "Type thesStudent name:").ToCommand();
            this.WhenAnyObservable(x => x.SearchByName).Subscribe(x =>
            {
                SearchLabel = "Type the student name:";
            });


            FindByCriteria = this.WhenAny(x => x.Criteria, x => !string.IsNullOrEmpty(x.Value)).ToCommand();
            this.WhenAnyObservable(x => x.FindByCriteria).Subscribe(x =>
            {
                //Client.SearchStudentsAsync
                PersonModel[] students = null;

                if (IsSearchByBarcode)
                {
                    var found = Storage.SearchByBarcode(Criteria);
                    if (found != null)
                        students = new[] { found };

                    SearchComplete = false;
                    //SearchResults = new ReactiveList<PersonModel>(students);
                    OnExit(found);
                }

                if (IsSearchByName)
                {
                    SearchComplete = true;

                    Storage.SearchStudents(Criteria)
                        .Subscribe(OnSearchResult);
                }


                Criteria = string.Empty;
            });

            if (isLocation)
                Title = "Student Exit " + Settings.Default.KioskLocation;
            else
            {
                Title = "Student Exit";
            }
        }

        async void OnExit(PersonModel person)
        {

            await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => OnBarcodeResult(person)));

            Main.CurrentView = new WelcomeViewModel(Main);
        }
        private void OnSearchResult(PersonModel[] list)
        {
            /*if (!list.Any())
            {
                = new List<StudentModel>(new[] { new StudentModel() { StudentNumber = "No Students Found" } }).ToArray();
                return;
            }*/
            foreach (var e in list)
            {
                try
                {
                    string display = "Present";

                    Tuple<int, DateTime, string> sqlScan = null;

                    if (e.GetType() == typeof(StudentModel))
                    {

                        e.CurrentStatus = "Absent".ToUpper();

                    }

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }

            SearchResults = new ReactiveList<PersonModel>(list);

        }



        private void OnBarcodeResult(PersonModel data)
        {
            bool allowPrint = false;

            if (data == null)
                return;

            //ViewModel.RecordScan(model, location, Lane.Right, true);
            var student = data as StudentModel;

           
            var scan = new Scan()
            {
                SwipeLane = Lane.Right,
                IsManualSwipe = true,
                SwipeMode = SwipeMode.Entry,
                StudentId = student.StudentId,
                StudentGuid = student.UniqueId,
                Barcode = student.IdNumber.Trim(),
                StudentName = student.DisplayName,
                ScanImage = student.Image,
                Homeroom = student.Homeroom,
                Grade = student.Grade,
                EntryTime = DateTime.Now
            };


            if (IsScanLocation)
            {

                allowPrint = Settings.Default.AllowKioskLocationPass;

                scan.Room = Settings.Default.KioskLocation;
                scan.ScanLocation = new ScanLocation() { RoomName = Settings.Default.KioskLocation, InOut = 1, Type = 0 };
                scan.IsLeavingLocation = true;
                LocationScans.InsertObject(new LocationScan()
                {
                    StudentNumber = scan.Barcode,
                    SwipeTime = DateTime.Now,
                    SwipedOut = true,
                    RoomName = Settings.Default.KioskLocation,
                    MarkAllPresent = scan.MarkAllPresentMode,
                    IsKiosk = true
                });

                if(allowPrint)
                    Application.Current.Dispatcher.Invoke(() => PrintLocationPass(scan, true), DispatcherPriority.DataBind);
            }
            else
            {
                allowPrint = Settings.Default.AllowKioskDismissalPass;

                var dismissal = Application.Current.Properties["DismissalLocation"] as ScanLocation;

                var code = "D";
                var reason = "Kiosk";

                if (dismissal != null)
                {
                    code = dismissal.AttendanceCode;
                    reason = dismissal.PeriodCode;
                }

                var record = new Dismissal()
                {
                    StudentGuid = scan.StudentGuid,
                    DismissalTime = scan.EntryTime,
                    StudentName = scan.StudentName,
                    StudentNumber = scan.Barcode,
                    StatusCode = code,
                    Reason = reason
                };

                scan.ScanLocation = new ScanLocation()
                {
                    RoomName = $"\"{reason}\" ({code})",
                    AttendanceCode = record.StatusCode,
                    Type = LocationType.Release
                };

                scan.IsStudentDismissed = true;
                DismissStudent(record);

                if (allowPrint)
                    Application.Current.Dispatcher.Invoke(() => PrintPass(scan, true), DispatcherPriority.DataBind);
            }
           
        }

        public void PrintPass(Scan model, bool printEnabled)
        {
            var queue = Application.Current.Properties["PassPrintQueue"] as PrintQueue;

            if (!printEnabled)
                return;

            var pass = new TardyPass(false);

            if (Settings.Default.SuppressIdOnPass)
                pass.BarcodeLabel.Visibility = Visibility.Hidden;

            var printModel = new PrintModel<Scan>(model);

            printModel.SchoolName = Settings.Default.School;

            pass.DataContext = printModel;

            try
            {

                if (queue != null)
                {

                    printVisual(queue, pass, "Dismissal Pass", new RotateTransform(90));

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

        public void PrintLocationPass(Scan model, bool printEnabled)
        {
            var queue = Application.Current.Properties["PassPrintQueue"] as PrintQueue;

            if (!printEnabled)
                return;

            var pass = new TardyPass(false);

            if (Settings.Default.SuppressIdOnPass)
                pass.BarcodeLabel.Visibility = Visibility.Hidden;

            var printModel = new PrintModel<Scan>(model);

            printModel.SchoolName = Settings.Default.School;

            pass.DataContext = printModel;

            try
            {

                if (queue != null)
                {

                    printVisual(queue, pass, "Dismissal Pass", new RotateTransform(90));

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


        public void DismissStudent(Dismissal dismiss)
        {

            Dismissals.InsertObject(dismiss);

        }

        void printVisual(PrintQueue queue, UserControl pass, string jobTitle, RotateTransform rotate)
        {
            bool isPassPrinter = false;

            try
            {
                pass.Width = 480;
                pass.Height = 270;

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

              
                if (jobTitle == "Alert Report")
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

                }

                pass.UpdateLayout();

                Logger.WarnFormat("Scale: {0}, OffsetY: {1}, OffsetX: {2}, Size: W {3}, H {4}", Settings.Default.PrintScaleFactor, Settings.Default.PrintOffsetY, Settings.Default.PrintOffsetX, sz.Width, sz.Height);
              
                var document = new FixedDocument();

                document.DocumentPaginator.PageSize = sz;

                document.PrintTicket = dialog.PrintTicket;

                var page = new FixedPage();
                page.Width = sz.Width;
                page.Height = sz.Height;

                var transform = new TransformGroup();
                var scale = Scale();
                if (scale != null)
                {
                    transform.Children.Add(scale);
                }
                transform.Children.Add(rotate);
                pass.LayoutTransform = transform;
              
                page.Children.Add(pass);
             
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

        ScaleTransform Scale()
        {
            var scaleFactor = Settings.Default.PrintScaleFactor;

            if (scaleFactor != 0)
                return new ScaleTransform(scaleFactor, scaleFactor);

            return null;
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



    }
}
