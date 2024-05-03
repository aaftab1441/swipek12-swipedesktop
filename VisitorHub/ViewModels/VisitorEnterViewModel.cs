using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Autofac;
using ReactiveUI;
using SwipeDesktop.Api;
using SwipeDesktop.Interfaces;
using SwipeDesktop.Storage;

namespace SwipeDesktop.ViewModels
{
    public class VisitorEntryViewModel : ReactiveObject, IHostedViewModel
    {
        public ReactiveCommand<object> TransitionToWelcome { get; private set; }
        public ReactiveCommand<object> TransitionToEnter { get; private set; }

        private VisitStorage VisitStorage;
        private RemoteStorage RemoteStorage;
        private LocalStorage LocalStorage;

        MainViewModel _main;
        public MainViewModel Main
        {
            get { return _main; }
            //set { this.RaiseAndSetIfChanged(ref f_name, value); }
        }

        public VisitorEntryViewModel(MainViewModel main)
        {
            HideKeyboard = true;
            HideVideo = false;

            _main = main;
            LocalStorage = App.Container.Resolve<LocalStorage>();
            RemoteStorage = App.Container.Resolve<RemoteStorage>();
            VisitStorage = App.Container.Resolve<VisitStorage>();

            TransitionToWelcome = this.WhenAny(x => x.Main, x => x.Value != null).ToCommand();
            TransitionToEnter = this.WhenAny(x => x.VisitorId, x => !string.IsNullOrEmpty(x.Value)).ToCommand();

            this.WhenAnyObservable(x => x.TransitionToWelcome).Subscribe(x => {
                HideKeyboard = true;
                HideVideo = false;
                Main.CurrentView = new WelcomeViewModel(Main);
            });

            this.WhenAnyObservable(x => x.TransitionToEnter).Subscribe(async x =>
            {
                var speedPass = await LocalStorage.SearchForSpeedpassAsync(VisitorId);

                if (speedPass == null)
                {
                    //MessageBox.Show("");
                    ErrorMessage = "Not a valid frequent visitor id.";
                    return;
                }

                //var source = (BitmapImage)this.Resources["NullPerson"];

                var scanVM = new VisitorScanViewModel(main, VisitStorage, RemoteStorage, LocalStorage)
                {
                    FirstName = speedPass.FirstName,
                    LastName = speedPass.LastName,
                    //Street1 = "100 Main Street",
                    //City = "Balitmore",
                    //State = "MD",
                    //Zip = "21015",
                    Identification = speedPass.PassId,
                    School = Settings.Default.SchoolId,
                    VisitEntryDate = DateTime.Now,
                    //BitmapSource = source
                };

                if (speedPass.DateOfBirth > DateTime.MinValue)
                {
                    scanVM.DateOfBirth = speedPass.DateOfBirth;
                }

                if (speedPass.Image != null)
                {
                    //todo: load image from byte array
                    //scanVM.BitmapSource = speedPass.Image.LoadImage();
                }

                HideKeyboard = true;
                HideVideo = false;
                ErrorMessage = "";

                main.CurrentView = scanVM;
            });
        }

        public void Send(Key key)
        {
            if (Keyboard.PrimaryDevice != null)
            {
                if (Keyboard.PrimaryDevice.ActiveSource != null)
                {
                    var e = new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0, key)
                    {
                        RoutedEvent = Keyboard.KeyDownEvent
                    };
                    InputManager.Current.ProcessInput(e);

                    // Note: Based on your requirements you may also need to fire events for:
                    // RoutedEvent = Keyboard.PreviewKeyDownEvent
                    // RoutedEvent = Keyboard.KeyUpEvent
                    // RoutedEvent = Keyboard.PreviewKeyUpEvent
                }
            }
        }

        bool _isProcessing;
        public bool IsProcessing
        {
            get { return _isProcessing; }
            set { this.RaiseAndSetIfChanged(ref _isProcessing, value); }
        }

        string _visitorId;
        public string VisitorId
        {
            get { return _visitorId; }
            set { this.RaiseAndSetIfChanged(ref _visitorId, value); }
        }

        bool _hideVideo;
        public bool HideVideo
        {
            get { return _hideVideo; }
            set { this.RaiseAndSetIfChanged(ref _hideVideo, value); }
        }

        bool _hideKeyboard;
        public bool HideKeyboard
        {
            get { return _hideKeyboard; }
            set { this.RaiseAndSetIfChanged(ref _hideKeyboard, value); }
        }


        string _errorMessage;
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { this.RaiseAndSetIfChanged(ref _errorMessage, value); }
        }
    }
}
