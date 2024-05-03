using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using log4net;
using ReactiveUI;
using SwipeDesktop.Api;
using SwipeDesktop.Common;
using SwipeDesktop.ViewModels;
using Window = System.Windows.Window;

namespace SwipeDesktop.Views
{
    /// <summary>
    /// Interaction logic for Welcome.xaml
    /// </summary>
    public partial class Welcome : IViewFor<WelcomeViewModel>
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Welcome));


        public Welcome()
        {
            InitializeComponent();
            this.Events().KeyDown.Subscribe(x =>
            {
                var key = x;
                var vm = (DataContext) as WelcomeViewModel;
                vm.ShowPopup = true;
            });


            this.BindCommand(ViewModel, x => x.KeyCommand, x => x, "KeyDown");

            MessageBus.Current.Listen<Tuple<string, string>>().Subscribe(_ =>
            {
                var msg = _;

                if (msg.Item1 == MessageEvents.WelcomeImageChanged)
                {
                    LoadImage(msg.Item2);
                }
            });

            LoadImage(SwipeDesktop.Settings.Default.KioskImagePath);

            var t0 = Task.Run(() => DataReplicator.InitRemoteServer());
            Task.WhenAll(new[] { t0 }).ContinueWith((c) =>
            {
               
                Task.Run(() => DataReplicator.RemoteSnapshot());


            });
            //var window = Window.GetWindow(this);
            //window.KeyDown += HandleKeyPress;

            //this.LocationEnterButton.Text = SwipeDesktop.Settings.Default.KioskLocation + " IN";

            //this.LocationExitButton.Text = SwipeDesktop.Settings.Default.KioskLocation + " OUT";
        }

        private void HandleKeyPress(object sender, KeyEventArgs e)
        {
            //Do work
        }

        void LoadImage(string path)
        {

            try
            {
                WelcomeImage.Source = new BitmapImage(new Uri(path));
                
                //WelcomeImage.
            }
            catch (Exception ex)
            {
                Logger.Error($"Could not load welcome image {path}", ex);
            }


        }
        public WelcomeViewModel ViewModel
        {
            get { return (WelcomeViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(WelcomeViewModel), typeof(Welcome), new PropertyMetadata(null));

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (WelcomeViewModel)value; }
        }

    }

}
