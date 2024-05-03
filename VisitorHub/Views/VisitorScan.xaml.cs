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
using WindowsInput;

namespace SwipeDesktop.Views
{
    /// <summary>
    /// Interaction logic for VisitorScan.xaml
    /// </summary>
    public partial class VisitorScan : UserControl
    {
        //static readonly InputSimulator sim = new InputSimulator();

        public VisitorScan()
        {
            InitializeComponent();
            LastName.GotFocus += ControlGotFocus;
            FirstName.GotFocus += ControlGotFocus;
            City.GotFocus += ControlGotFocus;
            State.GotFocus += ControlGotFocus;
            Zip.GotFocus += ControlGotFocus;
            Street1.GotFocus += ControlGotFocus;
            ID.GotFocus += ControlGotFocus;
            DOB.GotFocus += ControlGotFocus;

        }


        private TextBox _currentTextbox;

        private void ControlGotFocus(object sender, RoutedEventArgs e)
        {
            if (e.Source as TextBox != null)
            {
                _currentTextbox = e.Source as TextBox;
            }
        }

        protected void button_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;

            if (_currentTextbox == null)
                return;

            switch (button.CommandParameter.ToString())
            {
                case "ESC":
                    //this.DialogResult = false;
                    break;

                case "RETURN":
                    //sim.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.RETURN);
                    break;

                case "BACK":
                    if (_currentTextbox.Text.Length > 0)
                        _currentTextbox.Text = _currentTextbox.Text.Remove(_currentTextbox.Text.Length - 1);
                    break;

                default:
                    _currentTextbox.Text += button.Content.ToString();
                    break;
            }
        }
    }
}
