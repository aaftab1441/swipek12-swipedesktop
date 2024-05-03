using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ReactiveUI;

using WebCamControl.UI.Controls.WebcamPlayer;
using WebCamControl.UI.Input;

namespace WebCamControl.UI.Controls
{
    /// <summary>
    /// Interaction logic for Capture.xaml
    /// </summary>
    public partial class Capture : UserControl
    {
        public Capture()
        {
            InitializeComponent();

            // Create default device
            SelectedWebcamMonikerString = (CapDevice.DeviceMonikers.Length > 0) ? CapDevice.DeviceMonikers[0].MonikerString : "";

            WebcamRotation = 180d;

            CommandBindings.Add(new CommandBinding(CaptureImageCommands.CaptureImage,
               new ExecutedRoutedEventHandler(CaptureImage_Executed), new CanExecuteRoutedEventHandler(CaptureImage_CanExecute)));

        }

        /// <summary>
        /// Determines whether the CaptureImage command can be executed
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">EventArgs</param>
        private void CaptureImage_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            // Check if there is a valid webcam
            e.CanExecute = (SelectedWebcam != null);
        }

        /// <summary>
        /// Invoked when the CaptureImage command is executed
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">EventArgs</param>
        private void CaptureImage_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //// Store current image in the webcam
            BitmapSource bitmap = webcamPlayer.CurrentBitmap;
            if (bitmap != null)
            {
                //SimpleMvvmToolkit.MessageBus.Default.Notify("CaptureImage",this, new NotificationEventArgs<BitmapSource>("SelectedImage",bitmap));

                MessageBus.Current.SendMessage(new Tuple<string, BitmapSource>("SelectedImage", bitmap));


            }
        }


        /// <summary>
        /// Wrapper for the WebcamRotation dependency property
        /// </summary>
        public double WebcamRotation
        {
            get { return (double)GetValue(WebcamRotationProperty); }
            set { SetValue(WebcamRotationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for WebcamRotation.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WebcamRotationProperty =
            DependencyProperty.Register("WebcamRotation", typeof(double), typeof(Capture), new UIPropertyMetadata(0d));

        /// <summary>
        /// Wrapper for the SelectedWebcam dependency property
        /// </summary>
        public CapDevice SelectedWebcam
        {
            get { return (CapDevice)GetValue(SelectedWebcamProperty); }
            set { SetValue(SelectedWebcamProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedWebcam.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedWebcamProperty =
            DependencyProperty.Register("SelectedWebcam", typeof(CapDevice), typeof(Capture), new UIPropertyMetadata(null));

        /// <summary>
        /// Wrapper for the SelectedWebcamMonikerString dependency property
        /// </summary>
        public string SelectedWebcamMonikerString
        {
            get { return (string)GetValue(SelectedWebcamMonikerStringProperty); }
            set { SetValue(SelectedWebcamMonikerStringProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedWebcamMonikerString.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedWebcamMonikerStringProperty = DependencyProperty.Register("SelectedWebcamMonikerString", typeof(string),
            typeof(Capture), new UIPropertyMetadata("", new PropertyChangedCallback(SelectedWebcamMonikerString_Changed)));


        /// <summary>
        /// Invoked when the SelectedWebcamMonikerString dependency property has changed
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">EventArgs</param>
        private static void SelectedWebcamMonikerString_Changed(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            // Get typed sender
            Capture typedSender = sender as Capture;
            if (typedSender != null)
            {
                // Get new value
                string newMonikerString = e.NewValue as string;

                // Update the device
                if (typedSender.SelectedWebcam == null)
                {
                    // Create it
                    typedSender.SelectedWebcam = new CapDevice("");
                }

                // Now set the moniker string
                typedSender.SelectedWebcam.MonikerString = newMonikerString;
            }
        }
    }
}
