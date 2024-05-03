using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace SwipeDesktop.Views
{
    /// <summary>
    /// Interaction logic for Keypad.xaml
    /// </summary>
    public partial class Keypad : UserControl, INotifyPropertyChanged
    {
      
        private string _result;
        public string Result
        {
            get { return _result; }
            private set { _result = value; this.OnPropertyChanged("Result"); }
        }

        public Keypad()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
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
                    if (Result.Length > 0)
                        Result = Result.Remove(Result.Length - 1);
                    break;

                default:
                    Result += button.Content.ToString();
                    break;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(String info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

    }
}
