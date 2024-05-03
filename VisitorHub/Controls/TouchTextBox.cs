using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SwipeDesktop.Controls
{
    public class TouchEnabledTextBox : TextBox
    {
        //added field
        private Process _touchKeyboardProcess = null;


        public TouchEnabledTextBox()
        {
            this.GotTouchCapture += TouchEnabledTextBox_GotTouchCapture;

            this.LostFocus += TouchEnabledTextBox_LostFocus;

        }

        private void TouchEnabledTextBox_GotTouchCapture(
           object sender,
           System.Windows.Input.TouchEventArgs e)
        {
            string touchKeyboardPath =
               @"C:\Program Files\Common Files\Microsoft Shared\Ink\TabTip.exe";

            _touchKeyboardProcess = Process.Start(touchKeyboardPath);
        }

        private void TouchEnabledTextBox_LostFocus(object sender, RoutedEventArgs eventArgs)
        {
            if (_touchKeyboardProcess != null)
            {
                _touchKeyboardProcess.Kill();
                //nullify the instance pointing to the now-invalid process
                _touchKeyboardProcess = null;
            }
        }
    }
}

