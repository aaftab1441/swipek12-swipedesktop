using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ReactiveUI;
using SwipeDesktop.Interfaces;

namespace SwipeDesktop.ViewModels
{
    public class SuspendViewModel: ReactiveObject, IHostedViewModel
    {
        public ReactiveCommand<object> TransitionToWelcome { get; private set; }
        public ReactiveCommand<object> PasscodeEntry { get; private set; }

        public MainViewModel Main { 
            get;
            //set { this.RaiseAndSetIfChanged(ref f_name, value); }
        }

        public VisitLocationViewModel VisitVM { 
            get;
            //set { this.RaiseAndSetIfChanged(ref f_name, value); }
        }

        public SuspendViewModel(MainViewModel main, VisitLocationViewModel visitVm)
        {
            Main = main;
            VisitVM = visitVm;

            SchoolName = Settings.Default.School;

            TransitionToWelcome = this.WhenAny(x => x.SchoolName, x => x.Value != null).ToCommand();
            this.WhenAnyObservable(x => x.TransitionToWelcome).Subscribe(x => { Main.CurrentView = new WelcomeViewModel(); });


            PasscodeEntry = this.WhenAny(x => x.Criteria, x => !string.IsNullOrEmpty(x.Value)).ToCommand();
            this.WhenAnyObservable(x => x.PasscodeEntry).Subscribe(async x =>
            {

                string lastFour = Settings.Default.SchoolId.ToString().Substring(Settings.Default.SchoolId.ToString().Length -4);
                if (Criteria == lastFour)
                {
                    VisitVM.CompletedVisit(Main);
                }

                Criteria = string.Empty;
            });
        }

        bool _isProcessing;
        public bool IsProcessing
        {
            get { return _isProcessing; }
            set { this.RaiseAndSetIfChanged(ref _isProcessing, value); }
        }

        string _schoolName;
        public string SchoolName
        {
            get { return _schoolName; }
            set { this.RaiseAndSetIfChanged(ref _schoolName, value); }
        }


        string _criteria;
        public string Criteria
        {
            get { return _criteria; }
            set { this.RaiseAndSetIfChanged(ref _criteria, value); }
        }


    }
}
