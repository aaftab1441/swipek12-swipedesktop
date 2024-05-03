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
using System.Windows.Threading;
using ReactiveUI;
using SwipeDesktop.Api;
using SwipeDesktop.Models;
using SwipeDesktop.ViewModels;

namespace SwipeDesktop.Views
{
    /// <summary>
    /// Interaction logic for Exit.xaml
    /// </summary>
    public partial class StaffEnter : UserControl, IViewFor<StaffEnterViewModel>
    {
        private static readonly LocalStorage Client = new LocalStorage();

        protected void button_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            switch (button.CommandParameter.ToString())
            {
                case "ESC":
                    //this.DialogResult = false;
                    break;

                case "RETURN":
                    //this.DialogResult = true;
                    break;

                case "BACK":
                    if (SearchBox.Text.Length > 0)
                        SearchBox.Text = SearchBox.Text.Remove(SearchBox.Text.Length - 1);
                    break;

                default:
                    SearchBox.Text += button.Content.ToString();
                    break;
            }
        }
        private void TouchEnabledTextBox_GotFocus(object sender, RoutedEventArgs eventArgs)
        {
            var vm = this.DataContext as StaffEnterViewModel;

            if (vm != null && vm.SearchComplete)
            {
                vm.SearchResults = new ReactiveList<PersonModel>();
                vm.SearchComplete = false;
            }
        }

        public StaffEnter()
        {
            InitializeComponent();

            //this.WhenAnyValue(x => x.ViewModel).BindTo(this, x => x.DataContext);
            this.SearchBox.GotFocus += TouchEnabledTextBox_GotFocus;

            //this.Events().KeyDown.Where(k => k.Key == Key.Enter).Subscribe(x => { });

            Dispatcher.BeginInvoke(DispatcherPriority.Input,
                new Action(delegate ()
                {
                    this.SearchBox.Focusable = true;
                    this.SearchBox.Focus();
                    Keyboard.Focus(SearchBox); // Set Keyboard Focus
                }));

            /*
            var studentSearchText = Observable.FromEventPattern<TextChangedEventHandler, TextChangedEventArgs>(
                h => SearchBox.TextChanged += h,
                h => SearchBox.TextChanged -= h
            ).Select(x => ((TextBox)x.Sender).Text);

            studentSearchText
                .Throttle(TimeSpan.FromMilliseconds(100))
               
                .Select(Client.SearchStudentsAsync)
                .Switch()
                .ObserveOnDispatcher()
                .Subscribe(OnSearchResult);
            */

            //if (IsScanLocation)
            //    StudentEnterTitle.Text = "Student Enter " + SwipeDesktop.Settings.Default.KioskLocation;
        }

        private void OnSearchResult(PersonModel[] list)
        {
            /*if (!list.Any())
            {
                = new List<StudentModel>(new[] { new StudentModel() { StudentNumber = "No Students Found" } }).ToArray();
                return;
            }*/
        
            ViewModel.SearchResults = new ReactiveList<PersonModel>(list);
          
        }

        public StaffEnterViewModel ViewModel
        {
            get { return (StaffEnterViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(StaffEnterViewModel), typeof(StaffEnter), new PropertyMetadata(null));

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { 
                ViewModel = (StaffEnterViewModel)value;
            }
        }

    }
}
