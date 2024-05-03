using System;
using System.Collections.Generic;
using System.Linq;
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
using SwipeDesktop.ViewModels;

namespace SwipeDesktop.Views
{
    /// <summary>
    /// Interaction logic for Exit.xaml
    /// </summary>
    public partial class StudentExit : UserControl, IViewFor<StudentExitViewModel>
    {
        public StudentExit()
        {
            InitializeComponent();

            //this.WhenAnyValue(x => x.ViewModel).BindTo(this, x => x.DataContext);

            this.SearchBox.GotFocus += TouchEnabledTextBox_GotFocus;
            Dispatcher.BeginInvoke(DispatcherPriority.Input,
                new Action(delegate ()
                {
                    this.SearchBox.Focusable = true;
                    this.SearchBox.Focus();
                    Keyboard.Focus(SearchBox); // Set Keyboard Focus
                }));

        }

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
            var vm = this.DataContext as StudentExitViewModel;

            if (vm != null && vm.SearchComplete)
            {
                vm.SearchResults = new ReactiveList<Models.PersonModel>();
                vm.SearchComplete = false;
            }
        }


        public StudentExitViewModel ViewModel
        {
            get { return (StudentExitViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(StudentExitViewModel), typeof(StudentExit), new PropertyMetadata(null));

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { 
                ViewModel = (StudentExitViewModel)value;
            }
        }

    }
}
