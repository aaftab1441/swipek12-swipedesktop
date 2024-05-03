using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Collections.ObjectModel;
using WebCamControl.UI.Input;

namespace WebcamPlayer.UI.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Variables
        #endregion

        #region Constructor & destructor
        public MainWindow()
        {
            // Initialize component
            InitializeComponent();

            // Subscribe command bindings
            CommandBindings.Add(new CommandBinding(CaptureImageCommands.CaptureImage,
                new ExecutedRoutedEventHandler(CaptureImage_Executed), new CanExecuteRoutedEventHandler(CaptureImage_CanExecute)));
            CommandBindings.Add(new CommandBinding(CaptureImageCommands.RemoveImage,
                new ExecutedRoutedEventHandler(RemoveImage_Executed)));
            CommandBindings.Add(new CommandBinding(CaptureImageCommands.ClearAllImages,
                new ExecutedRoutedEventHandler(ClearAllImages_Executed)));

        }
        #endregion

        #region Command bindings
        /// <summary>
        /// Determines whether the CaptureImage command can be executed
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">EventArgs</param>
        private void CaptureImage_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            // Check if there is a valid webcam
            //e.CanExecute = (SelectedWebcam != null);
        }

        /// <summary>
        /// Invoked when the CaptureImage command is executed
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">EventArgs</param>
        private void CaptureImage_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //// Store current image in the webcam
            BitmapSource bitmap = null;// = webcamPlayer.CurrentBitmap;
            if (bitmap != null)
            {
                SelectedImages.Add(bitmap);
            }
        }

        /// <summary>
        /// Invoked when the RemoveImage command is executed
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">EventArgs</param>
        private void RemoveImage_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            // Store current image in the webcam
            BitmapSource bitmap = e.Parameter as BitmapSource;
            if (bitmap != null)
            {
                SelectedImages.Remove(bitmap);
            }
        }

        /// <summary>
        /// Invoked when the ClearAllImages command is executed
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">EventArgs</param>
        private void ClearAllImages_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            // Clear all images
            SelectedImages.Clear();
        }
        #endregion

        #region Properties
      
        /// <summary>
        /// Wrapper for the SelectedImages dependency property
        /// </summary>
        public ObservableCollection<BitmapSource> SelectedImages
        {
            get { return (ObservableCollection<BitmapSource>)GetValue(SelectedImagesProperty); }
            set { SetValue(SelectedImagesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedImages.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedImagesProperty = DependencyProperty.Register("SelectedImages", typeof(ObservableCollection<BitmapSource>),
            typeof(MainWindow), new UIPropertyMetadata(new ObservableCollection<BitmapSource>()));
        #endregion

        #region Methods
       
        #endregion
    }
}
