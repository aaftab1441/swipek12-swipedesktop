using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using ReactiveUI;
using SwipeDesktop.Api;
using SwipeDesktop.Interfaces;
using SwipeDesktop.Models;
using SwipeDesktop.Storage;

namespace SwipeDesktop.ViewModels
{
    public class VisitorScanViewModel : ReactiveObject, IHostedViewModel
    {
     
        MainViewModel _main;
        public MainViewModel Main
        {
            get { return _main; }
            //set { this.RaiseAndSetIfChanged(ref f_name, value); }
        }

        public VisitModel CurrentScan { get; set; }

        public VisitorScanViewModel() { 
            //no-op
            CurrentScan = new VisitModel();
        }


        bool _isProcessing;
        public bool IsProcessing
        {
            get { return _isProcessing; }
            set { this.RaiseAndSetIfChanged(ref _isProcessing, value); }
        }

        public VisitorScanViewModel(MainViewModel main, VisitStorage storage, RemoteStorage api, LocalStorage localStorage): this()
        {

            Api = api;
            _main = main;
            //_main.DefaultPrinter = 

            VisitEntryDate = DateTime.Now;

            this.WhenAnyValue(x => x.DateOfBirth)
            .Select(x => x.HasValue ? x.Value.ToString("MM/dd/yyyy") : string.Empty)
            .ToProperty(this, x => x.FormattedDateOfBirth, out _formattedDateOfBirth);

            this.WhenAnyValue(x => x.VisitEntryDate)
            .Select(x => x.ToString("G"))
            .ToProperty(this, x => x.FormattedVisitDate, out _formattedVisitDate);


            this.WhenAnyValue(x => x.EntryNumber)
                .Select(x => $"Exit ID: {x}")
                .ToProperty(this, x => x.FormattedVisitEntryNumber, out _formattedVisitEntryNumber);

            this.WhenAnyValue(x => x.FirstName, x=> x.LastName)
            .Select(x => $"{x.Item1} {x.Item2}")
            .ToProperty(this, x => x.FullName, out _fullName);

            this.WhenAnyValue(x => x.VisitEntryDate, x => x.LastName).Where(x=>!string.IsNullOrEmpty(x.Item2))
                .Select(x => $"{x.Item1:HHmmss}") //x.Item2.Substring(0, 1)}
                .ToProperty(this, x => x.EntryNumber, out _entryNumber);

            TransitionToLocations = this.WhenAny(x => x.FullName, x => !String.IsNullOrWhiteSpace(x.Value)).ToCommand();

            this.WhenAnyObservable(x => x.TransitionToLocations).Subscribe(async x =>
            {
                Main.CurrentView = new VisitLocationViewModel(Main, Application.Current.Properties["Locations"] as string[], storage, api, localStorage);
                //await CheckAlerts();

            });

            TransitionToWelcome = this.WhenAny(x => x.Main, x => x.Value != null).ToCommand();
            this.WhenAnyObservable(x => x.TransitionToWelcome).Subscribe(x => { Main.CurrentView = new WelcomeViewModel(Main); });

            //copy to model for save
            this.WhenAnyValue(x => x.FirstName).BindTo(this, x => x.CurrentScan.FirstName);
            this.WhenAnyValue(x => x.LastName).BindTo(this, x => x.CurrentScan.LastName);
            this.WhenAnyValue(x => x.Street1).BindTo(this, x => x.CurrentScan.Street1);
            this.WhenAnyValue(x => x.City).BindTo(this, x => x.CurrentScan.City);
            this.WhenAnyValue(x => x.State).BindTo(this, x => x.CurrentScan.State);
            this.WhenAnyValue(x => x.Zip).BindTo(this, x => x.CurrentScan.Zip);
            this.WhenAnyValue(x => x.Identification).BindTo(this, x => x.CurrentScan.Identification);
            this.WhenAnyValue(x => x.DateOfBirth).BindTo(this, x => x.CurrentScan.DateOfBirth);
            this.WhenAnyValue(x => x.VisitEntryDate).BindTo(this, x => x.CurrentScan.VisitEntryDate);
            this.WhenAnyValue(x => x.VisitLocation).BindTo(this, x => x.CurrentScan.ReasonForVisit);
            this.WhenAnyValue(x => x.School).BindTo(this, x => x.CurrentScan.School);
            this.WhenAnyValue(x => x.EntryNumber).BindTo(this, x => x.CurrentScan.VisitEntryNumber);
        }

        public ReactiveCommand<object> TransitionToLocations { get; private set; }
        public ReactiveCommand<object> TransitionToWelcome { get; private set; }

       
        private string f_name;
        public string FirstName
        {
            get { return f_name; }
            set { this.RaiseAndSetIfChanged(ref f_name, value); }
        }

        private string l_name;
        public string LastName
        {
            get { return l_name; }
            set { this.RaiseAndSetIfChanged(ref l_name, value); }
        }


        private string _street1;
        public string Street1
        {
            get { return _street1; }
            set { this.RaiseAndSetIfChanged(ref _street1, value); }
        }

        private string _city;
        public string City
        {
            get { return _city; }
            set { this.RaiseAndSetIfChanged(ref _city, value); }
        }

        private string _state;
        public string State
        {
            get { return _state; }
            set { this.RaiseAndSetIfChanged(ref _state, value); }
        }

        private string _zip;
        public string Zip
        {
            get { return _zip; }
            set { this.RaiseAndSetIfChanged(ref _zip, value); }
        }

        private DateTime? _dob;
        public DateTime? DateOfBirth
        {
            get { return _dob; }
            set { this.RaiseAndSetIfChanged(ref _dob, value); }
        }

        private string _identification;
        public string Identification
        {
            get { return _identification; }
            set { this.RaiseAndSetIfChanged(ref _identification, value); }
        }

        private DateTime _visitEntryDate;
        public DateTime VisitEntryDate
        {
            get { return _visitEntryDate; }
            set { this.RaiseAndSetIfChanged(ref _visitEntryDate, value); }
        }

        public string Id { get; set; }

        private readonly ObservableAsPropertyHelper<string> _formattedDateOfBirth;
        public string FormattedDateOfBirth => _formattedDateOfBirth.Value;

        private readonly ObservableAsPropertyHelper<string> _formattedVisitDate;
        public string FormattedVisitDate => _formattedVisitDate.Value;

        private readonly ObservableAsPropertyHelper<string> _formattedVisitEntryNumber;
        public string FormattedVisitEntryNumber => _formattedVisitEntryNumber.Value;

        private readonly ObservableAsPropertyHelper<string> _fullName;
        public string FullName => _fullName.Value;

        private readonly ObservableAsPropertyHelper<string> _entryNumber;
        public string EntryNumber => _entryNumber.Value;

        string _location;
        public string VisitLocation
        {
            get { return _location; }
            set { this.RaiseAndSetIfChanged(ref _location, value); }
        }

        int _school;
        public int School
        {
            get { return _school; }
            set { this.RaiseAndSetIfChanged(ref _school, value); }
        }

        private BitmapSource _bitmap;

        //[JsonIgnore]
        public BitmapSource BitmapSource { get { return _bitmap; } set { this.RaiseAndSetIfChanged(ref _bitmap, value); } }


        private string _imagePath;
        public string ImagePath { get { return _imagePath; } set { _imagePath = value; this.RaiseAndSetIfChanged(ref _imagePath, value); } }


        private RemoteStorage Api;

        public async Task CheckAlerts(bool checkRegistry = true)
        {
           

            var entry = new Messages.VisitorEntry()
            {
                EntryTime = VisitEntryDate,
                Location = VisitLocation,
                SchoolId = Settings.Default.SchoolId,
                VisitorFirstName = FirstName,
                VisitorState = State,
                VisitorLastName = LastName,
                VisitorDob = DateOfBirth ?? DateTime.MinValue,
                CheckOffender = checkRegistry
            };

            await Api.SendVisitEntryAsync(entry);
        }
    }
}
