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
    public class ProcessingViewModel: ReactiveObject, IHostedViewModel
    {
        public ReactiveCommand<object> TransitionToWelcome { get; private set; }

        MainViewModel _main;
        public MainViewModel Main
        {
            get { return _main; }
            //set { this.RaiseAndSetIfChanged(ref f_name, value); }
        }

        public ProcessingViewModel(MainViewModel main)
        {
            _main = main;
            SchoolName = Settings.Default.School;

            TransitionToWelcome = this.WhenAny(x => x.SchoolName, x => x.Value != null).ToCommand();
            this.WhenAnyObservable(x => x.TransitionToWelcome).Subscribe(x => { Main.CurrentView = new WelcomeViewModel(); });
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

    }
}
