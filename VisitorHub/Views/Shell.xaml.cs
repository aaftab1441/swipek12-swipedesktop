using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
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

using SwipeDesktop.ViewModels;

namespace SwipeDesktop.Views
{
    /// <summary>
    /// Interaction logic for Shell.xaml
    /// </summary>
    public partial class Shell : IViewFor<MainViewModel>
    {

       
        public Shell()
        {
            InitializeComponent();

            this.WhenAnyValue(x => x.ViewModel).BindTo(this, x => x.DataContext);

            UserError.RegisterHandler(async error =>
            {
                RxApp.MainThreadScheduler.Schedule(error, (scheduler, userError) =>
                {
                    // NOTE: this code really shouldn't throw away the MessageBoxResult
                    var result = MessageBox.Show(userError.ErrorMessage);
                    return Disposable.Empty;
                });

                return await Task.Run(() => RecoveryOptionResult.CancelOperation);
            });

            this.Bind(ViewModel, vm => vm.Connected, v => v.InternetConnectedText.Text);

        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(Shell));



        public ReactiveCommand<object> TransitionToScan { get; private set; }

        public MainViewModel ViewModel
        {
            get { return (MainViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(MainViewModel), typeof(Shell), new PropertyMetadata(null));

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (MainViewModel)value; }
        }
    }
}
