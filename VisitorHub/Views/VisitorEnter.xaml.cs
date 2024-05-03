using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
using ReactiveUI;
using SwipeDesktop.ViewModels;

namespace SwipeDesktop.Views
{
    /// <summary>
    /// Interaction logic for VisitorEnter.xaml
    /// </summary>
    public partial class VisitorEnter : UserControl, IViewFor<VisitorEntryViewModel>
    {
        public VisitorEnter()
        {
            InitializeComponent();

            this.VisitorId.GotFocus += TouchEnabledTextBox_GotFocus;
            VisitorId.TextChanged += VisitorId_TextChanged;
        }

        private void VisitorId_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;

            if(tb.Text.Length == 0)
            {
                var vm = this.DataContext as VisitorEntryViewModel;
                //Video.Stop();

                if (vm != null)
                {
                    vm.ErrorMessage = string.Empty;
                }
            }
        }

        public VisitorEntryViewModel ViewModel
        {
            get { return (VisitorEntryViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(VisitorEntryViewModel), typeof(VisitorEnter), new PropertyMetadata(null));

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (VisitorEntryViewModel)value; }
        }

        private void TouchEnabledTextBox_GotFocus(object sender, RoutedEventArgs eventArgs)
        {
            var vm = this.DataContext as VisitorEntryViewModel;
            //Video.Stop();

            if (vm != null)
            {
                vm.HideKeyboard = false;
                vm.HideVideo = true;
            }
        }

        private void Video_OnMediaEnded(object sender, RoutedEventArgs e)
        {
            ((MediaElement)sender).Position = TimeSpan.FromSeconds(0);
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
                    if (VisitorId.Text.Length > 0)
                        VisitorId.Text = VisitorId.Text.Remove(VisitorId.Text.Length - 1);
                    break;

                default:
                    VisitorId.Text += button.Content.ToString();
                    break;
            }
        }
    }
}
