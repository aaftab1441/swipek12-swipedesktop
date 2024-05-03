using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Autofac;
using Messages;
using ReactiveUI;
using SwipeDesktop.Api;
using SwipeDesktop.Interfaces;
using SwipeDesktop.Models;
using SwipeDesktop.Storage;
using SwipeK12.NextGen.Messaging;

namespace SwipeDesktop.ViewModels
{
    public class VisitorExitViewModel: ReactiveObject, IHostedViewModel
    {
        public ReactiveCommand<object> TransitionToWelcome { get; private set; }
        public ReactiveCommand<object> FindByCriteria { get; private set; }

        public ReactiveCommand<object> Exit { get; private set; }


        bool _searchComplete;
        public bool SearchComplete
        {
            get { return _searchComplete; }
            set { this.RaiseAndSetIfChanged(ref _searchComplete, value); }
        }


        MainViewModel _main;
        private LocalStorage Database;
        private VisitExitStorage VisitExitStorage;

        public MainViewModel Main
        {
            get { return _main; }
            //set { this.RaiseAndSetIfChanged(ref f_name, value); }
           
        }

        public VisitorExitViewModel(MainViewModel main)
        {
            _main = main;
            Database = App.Container.Resolve<LocalStorage>();
            VisitExitStorage = App.Container.Resolve<VisitExitStorage>();

            TransitionToWelcome = this.WhenAny(x => x.Main, x => x.Value != null).ToCommand();
            this.WhenAnyObservable(x => x.TransitionToWelcome).Subscribe(x => { Main.CurrentView = new WelcomeViewModel(Main); });

            Exit = this.WhenAny(x => x.SearchResults, x => x.Value.Any()).ToCommand();
            this.WhenAnyObservable(x => x.Exit).Subscribe(async x =>
            {
                var exit = new VisitExit();
                exit.VisitId = SelectedVisit.Id;
                exit.VisitNumber = SelectedVisit.VisitNumber;
                exit.DateExited = DateTime.Now;
                exit.Source = Environment.MachineName;
              
                VisitExitStorage.InsertObject(exit);
                await Database.RecordVisitExit(exit);

                SelectedVisit = null;
                Main.CurrentView = new WelcomeViewModel(Main);
                //MessageBox.Show(SelectedVisit);
                //SelectedVisit = null;
            });

            FindByCriteria = this.WhenAny(x => x.Criteria, x => !string.IsNullOrEmpty(x.Value)).ToCommand();
            this.WhenAnyObservable(x => x.FindByCriteria).Subscribe(async x =>
            {
                //Client.SearchStudentsAsync
                if (Criteria.Length > 6)
                {
                    Criteria = Criteria.Substring(0, 6);
                }

                var record = await Database.SearchForVisitAsync(Criteria);
                if (record != null)
                {
                    if (record.ExitDate.HasValue)
                    {
                        Main.CurrentView = new WelcomeViewModel(Main);
                    }

                    SelectedVisit = record;
                    SearchResults = new ReactiveList<PersonModel>(new[]
                        {new PersonModel() {FirstName = record.VisitorFirstName, LastName = record.VisitorLastName}});

                    var exit = new VisitExit();
                    exit.VisitId = SelectedVisit.Id;
                    exit.VisitNumber = SelectedVisit.VisitNumber;
                    exit.DateExited = DateTime.Now;
                    exit.Source = Environment.MachineName;

                    VisitExitStorage.InsertObject(exit);
                    await Database.RecordVisitExit(exit);

                    SelectedVisit = null;
                    Criteria = null;
                    SearchComplete = true;
                    Main.CurrentView = new WelcomeViewModel(Main);
                }
                else
                {
                    MessageBox.Show($"No visit was found by {Criteria}");
                    Criteria = null;
                    SearchComplete = false;
                }
            });
        }

        string _criteria;
        public string Criteria
        {
            get { return _criteria; }
            set { this.RaiseAndSetIfChanged(ref _criteria, value); }
        }


        VisitLog _selectedVisit;
        public VisitLog SelectedVisit
        {
            get { return _selectedVisit; }
            set { this.RaiseAndSetIfChanged(ref _selectedVisit, value); }
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

    }
}
