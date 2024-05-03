using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Printing;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Gma.QrCodeNet.Encoding;
using Gma.QrCodeNet.Encoding.Windows.Render;
using log4net;
using log4net.Repository.Hierarchy;


namespace SwipeK12
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly string _dropIdentifier = "dropIdentifier";

        private Dictionary<string, string> addFieldsList = null;

        private SwipeCardBLL scBLL;

        private AdornerLayer aLayer;

        private bool _isDown;
        private bool _isDragging;
        private bool selected = false;
        private UIElement selectedElement = null;

        private Point _startPoint;
        private double _originalLeft;
        private double _originalTop;

        private int zIndex = 0;

        public MainWindow()
        {
            InitializeComponent();

            // Configure logging
            log4net.Config.XmlConfigurator.Configure();

            log.Info("Application Initialization Starting");

            cboFontSize.ItemsSource = new List<double>() { 8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 36, 48, 64, 72 };

            scBLL = new SwipeCardBLL();
            setCardDefaults();
        }


        // ===========================================================================================================================
        // ===========================================================================================================================
        // Getters & Setters
        // ===========================================================================================================================
        // ===========================================================================================================================


        // ===========================================================================================================================
        // ===========================================================================================================================
        // UI Event Handlers
        // ===========================================================================================================================
        // ===========================================================================================================================

        private void btnBackground_Click(object sender, RoutedEventArgs e)
        {
            string imgFile = SwipeUtils.OpenImageDialogForm();

            if (imgFile != null)
            {
                txtBackground.Text = imgFile;
                updateCanvasBackground(getActiveCanvas(), txtBackground.Text, 1);

                // Always default BG opacity to 100% initially
                sldOpacity.Value = 1;
            }
        }

        private void btnBatch_Click(object sender, RoutedEventArgs e)
        {
            //TODO
        }

        private void btnBringToFront_Click(object sender, RoutedEventArgs e)
        {
            CanvasUtils.BringToFront(getActiveCanvas(), selectedElement);
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete this card?", "Delete Card", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

            if (result.Equals(MessageBoxResult.Yes))
            {
                try
                {
                    scBLL.ArchiveIdCard(App.getCurrentCardID(), App.getCurrentSchoolID());
                    resetToBeginning();
                }
                catch (Exception ex)
                {
                    log.Error("Error archiving ID card", ex);
                    MessageBox.Show("An unexpected error occurred when attempting to delete the ID card. Please contact your system support. Error message returned: " + ex.Message, "Error Deleting Card", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void btnDeleteField_Click(object sender, RoutedEventArgs e)
        {
            if (selectedElement != null)
            {
                getActiveCanvas().Children.Remove(selectedElement);
                selectedElement = null;
                updateTextToolbar();
            }
        }

        private void btnDuplicate_Click(object sender, RoutedEventArgs e)
        {
            InputDialog inputDialog = new InputDialog("Duplicate Card", "New Card Name:", "");

            if (inputDialog.ShowDialog() == true)
            {
                int newCardId = -1;

                try
                {
                    newCardId = scBLL.DuplicateIDCard(App.getCurrentCardID(), inputDialog.Answer);
                    App.setCurrentCardID(newCardId);
                    loadIDCard(newCardId);
                    zIndex = CanvasUtils.GetMaxZindex(getActiveCanvas());

                    MessageBox.Show("Card '" + inputDialog.Answer + "' duplicated succesfully.", "Card Duplicated", MessageBoxButton.OK);
                }
                catch (Exception ex)
                {
                    log.Error("Error duplicating ID card", ex);
                    MessageBox.Show("An unexpected error occurred when attempting to duplicate ID card. Please contact your system support. Error message returned: " + ex.Message, "Error Duplicating Card", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            if (scBLL.GetActiveIDCardsCount() > 0)
            {
                bool gotoLoad = false;

                // Is an ID card currently loaded
                if (btnSave.IsEnabled)
                {
                    MessageBoxResult result = MessageBox.Show("Loading an ID card will cause any unsaved changes to current card to be lost, do you wish to continue?", "Load Existing Card", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

                    gotoLoad = result.Equals(MessageBoxResult.Yes);
                }
                else
                {
                    gotoLoad = true;
                }

                if (gotoLoad)
                {
                    OpenCard ocWindow = new OpenCard();
                    if (ocWindow.ShowDialog() == true)
                    {
                        App.setCurrentCardID(ocWindow.getCardID());
                        loadIDCard(ocWindow.getCardID());
                        zIndex = CanvasUtils.GetMaxZindex(getActiveCanvas());
                    }
                }
            }
            else
            {
                MessageBox.Show("No ID cards currently exist. Please create an ID card first.", "No ID Cards", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            bool gotoNew = false;

            // Is an ID card currently loaded
            if (btnSave.IsEnabled)
            {
                MessageBoxResult result = MessageBox.Show("Creating a new ID card will cause any unsaved changes to current card to be lost, do you wish to continue?", "Create New Card", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

                gotoNew = result.Equals(MessageBoxResult.Yes);
            }
            else
            {
                gotoNew = true;
            }

            if (gotoNew)
            {
                CreateCard ccWindow = new CreateCard();
                if (ccWindow.ShowDialog() == true)
                {
                    App.setCurrentCardID(ccWindow.getNewCardID());
                    loadIDCard(ccWindow.getNewCardID());
                    zIndex = 0;
                }
            }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog dialog = new PrintDialog();

                TextBlock txtBlk;

                if (Convert.ToBoolean(rbCardFront.IsChecked))
                {
                    txtBlk = txtBlkFrontOrientation;
                }
                else
                {
                    txtBlk = txtBlkBackOrientation;
                }

                if (txtBlk.Text.Equals(App.APP_CARD_ORIENTATION_LANDSCAPE))
                {
                    dialog.PrintTicket.PageOrientation = PageOrientation.Landscape;
                }
                else
                {
                    dialog.PrintTicket.PageOrientation = PageOrientation.Portrait;
                }

                if (dialog.ShowDialog() == true)
                {
                    dialog.PrintVisual(getActiveCanvas(), "Print ID Card");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Print Screen", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void btnRemoveBackground_Click(object sender, RoutedEventArgs e)
        {
            txtBackground.Text = "";
            getActiveCanvas().Background = new SolidColorBrush(Colors.White);
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            List<CardItem> cardItems = new List<CardItem>();
            buildCardItemList(cnvCardFront, cardItems, App.APP_CARD_FRONT);
            buildCardItemList(cnvCardBack, cardItems, App.APP_CARD_BACK);

            string frontBG = getCanvasBackgroundFile(cnvCardFront);
            string backBG = getCanvasBackgroundFile(cnvCardBack);

            var xml = Serialisation.SerializeObject<List<CardItem>>(cardItems);

            // Remove leading invalid characters that may exist due to encoding conflicts
            if (xml.Length > 0 && xml[0] != '<')
            {
                xml = xml.Substring(1, xml.Length - 1);
            }

            int recUpdated = scBLL.UpdateCardByIdAndSchool(frontBG, cnvCardFront.Background.Opacity, backBG, cnvCardBack.Background.Opacity, isCardDualSided(), xml, true, App.getCurrentCardID(), App.getCurrentSchoolID());

            if (recUpdated == 1)
            {
                MessageBox.Show("Card '" + txtBlkCardName.Text + "' saved succesfully.", "Card Saved", MessageBoxButton.OK);
            }
            else
            {
                MessageBox.Show("Card '" + txtBlkCardName.Text + "' save failed.", "Card Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            CardSettings csWindow = new CardSettings();
            if (csWindow.ShowDialog() == true)
            {
                if (csWindow.isSchoolChanged())
                {
                    // Update all data
                    loadIDCard(App.getCurrentCardID());
                }

                if (csWindow.isStudentChanged() && txtBlkCardType.Text.Equals(App.APP_CARD_TYPE_STUDENT))
                {
                    // Update data if Student card currently active
                    loadIDCard(App.getCurrentCardID());
                }

                if (csWindow.isTeacherChanged() && txtBlkCardType.Text.Equals(App.APP_CARD_TYPE_TEACHER))
                {
                    // Update data if Teacher card currently active
                    loadIDCard(App.getCurrentCardID());
                }
            }
        }

        private void Canvas_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(_dropIdentifier) || sender == e.Source)
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void Canvas_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(_dropIdentifier))
            {
                Point dropPoint = e.GetPosition(sender as Canvas);
                KeyValuePair<string, string> selectedField = (KeyValuePair<string, string>)e.Data.GetData(_dropIdentifier);
                DropOnCanvas(sender as Canvas, selectedField, dropPoint);
            }
        }

        // Handler for element selection on the canvas providing resizing adorner
        private void Canvas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            cancelCanvasSelections();

            // If any element except canvas is clicked, 
            // assign the selected element and add the adorner
            if (e.Source != (sender as Canvas))
            {
                // If the element on the canvas clicked is a 
                // textbox, give it focus so it can be edited
                if (e.Source is TextBox)
                {
                    TextBox t = (TextBox)e.Source;
                    if (!t.IsReadOnly)
                    {
                        t.Focus();
                    }
                }

                _isDown = true;
                _startPoint = e.GetPosition(sender as Canvas);

                selectedElement = e.Source as UIElement;
                updateTextToolbar();

                _originalLeft = Canvas.GetLeft(selectedElement);
                _originalTop = Canvas.GetTop(selectedElement);

                aLayer = AdornerLayer.GetAdornerLayer(selectedElement);
                aLayer.Add(new ResizingAdorner(selectedElement));
                selected = true;
                e.Handled = true;
            }
        }

        private void cboFontFamily_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboFontFamily.SelectedValue != null)
            {
                applyFont(cboFontFamily.SelectedValue.ToString(), selectedElement);
            }
        }

        private void cboFontSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboFontSize.SelectedValue != null)
            {
                try
                {
                    double fs = Convert.ToDouble(cboFontSize.SelectedValue);
                    applyFontSize(fs, selectedElement); 
                }
                catch (Exception ex)
                {
                    log.Error("Error parsing Font Size" + ex.Message);
                }
            }
        }

        private void cboFontSize_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void cellAlign_Click(object sender, RoutedEventArgs e)
        {
            var cellPosition = ((Button)sender).Tag;
            positionField(cellPosition.ToString());
        }

        private void cpFont_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            applyFontColor((Color)e.NewValue, selectedElement);
        }

        private void cpTextBG_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            applyTextHighlightColor((Color)e.NewValue, selectedElement);
        }

        private void lstAddFields_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // The initial mouse position
            _startPoint = e.GetPosition(null);
        }

        private void lstAddFields_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            // Get the current mouse position
            Point mousePos = e.GetPosition(null);
            Vector diff = _startPoint - mousePos;

            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                // Get the dragged ListBoxItem
                var listBox = sender as ListBox;
                if (listBox != null && listBox.SelectedValue != null)
                {
                    KeyValuePair<string, string> selectedField = (KeyValuePair<string, string>) listBox.SelectedValue;

                    // Initialize the drag & drop operation
                    DataObject dragData = new DataObject(_dropIdentifier, selectedField);
                    DragDrop.DoDragDrop(listBox, dragData, DragDropEffects.Move);
                }
            }
        }

        private void popup_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Primitives.Popup popup = sender as System.Windows.Controls.Primitives.Popup;
            if (popup != null)
                popup.IsOpen = false;
        }

        private void rbCardFront_Checked(object sender, RoutedEventArgs e)
        {
            if (BackCanvasBorder != null)
            {
                BackCanvasBorder.Visibility = Visibility.Collapsed;
            }
            if (cnvCardBack != null)
            {
                cnvCardBack.Visibility = Visibility.Collapsed;
            }
            if (FrontCanvasBorder != null)
            {
                FrontCanvasBorder.Visibility = Visibility.Visible;
            }
            if (cnvCardFront != null)
            {
                cnvCardFront.Visibility = Visibility.Visible;
            }
            cancelCanvasSelections();
            updateBackgroundControls();
        }

        private void rbCardBack_Checked(object sender, RoutedEventArgs e)
        {
            if (FrontCanvasBorder != null)
            {
                FrontCanvasBorder.Visibility = Visibility.Collapsed;
            }
            if (cnvCardFront != null)
            {
                cnvCardFront.Visibility = Visibility.Collapsed;
            }
            if (BackCanvasBorder != null)
            {
                BackCanvasBorder.Visibility = Visibility.Visible;
            }
            if (cnvCardBack != null)
            {
                cnvCardBack.Visibility = Visibility.Visible;
            }
            cancelCanvasSelections();
            updateBackgroundControls();
        }

        private void sldOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (getActiveCanvas().Background is ImageBrush)
            {
                Slider slider = sender as Slider;
                getActiveCanvas().Background.Opacity = slider.Value;
            }
        }

        protected void textBox_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = (TextBox)sender;

            // Get measured text size with 10 pixel padding added
            double textSize = SwipeUtils.MeasureTextSize(tb.Text, tb.FontFamily, tb.FontStyle, tb.FontWeight, tb.FontStretch, tb.FontSize).Width + 10;
            double textboxWidth = tb.Width;

            if (textSize >= textboxWidth)
            {
                // New textbox size can not exceed canvas size
                Canvas c = (Canvas)tb.Parent;
                double newWidth = Math.Ceiling(textSize);
                double left = Canvas.GetLeft(tb);

                // If the width of the textbox (based on position within canvas) exceeds 
                // canvas then no longer increase textbox size so it will start to wrap
                if ((left + newWidth) <= c.Width)
                {
                    tb.Width = newWidth;
                }
            }
            else
            {
                // See if textbox needs to be reduced in size
                double newWidth = Math.Ceiling(textSize);

                if (tb.IsReadOnly)
                {
                    // These are dB fields, therefore no min size
                    tb.Width = newWidth;
                }
                else
                {
                    // These are user generated dynamic fields, therefore apply min default size
                    if (newWidth < App.APP_DEFAULT_TEXTBOX_WIDTH)
                    {
                        tb.Width = App.APP_DEFAULT_TEXTBOX_WIDTH;
                    }
                    else
                    {
                        tb.Width = newWidth;
                    }
                }
            }
        }

        private void toggleBold_Checked(object sender, RoutedEventArgs e)
        {
            applyBold(true, selectedElement);
        }

        private void toggleBold_Unchecked(object sender, RoutedEventArgs e)
        {
            applyBold(false, selectedElement);
        }

        private void toggleCellAlign_Click(object sender, RoutedEventArgs e)
        {
            popupCellAlign.IsOpen = true;
            popupCellAlign.Closed += (senderClosed, eClosed) =>
            {
                toggleCellAlign.IsChecked = false;
            };
        }

        private void toggleCenter_Checked(object sender, RoutedEventArgs e)
        {
            applyTextAlignment(TextAlignment.Center, selectedElement);

            toggleLeft.IsChecked = false;
            toggleRight.IsChecked = false;
        }

        private void toggleItalic_Checked(object sender, RoutedEventArgs e)
        {
            applyItalic(true, selectedElement);
        }

        private void toggleItalic_Unchecked(object sender, RoutedEventArgs e)
        {
            applyItalic(false, selectedElement);
        }
        
        private void toggleLeft_Checked(object sender, RoutedEventArgs e)
        {
            applyTextAlignment(TextAlignment.Left, selectedElement);

            toggleCenter.IsChecked = false;
            toggleRight.IsChecked = false;
        }

        private void toggleRight_Checked(object sender, RoutedEventArgs e)
        {
            applyTextAlignment(TextAlignment.Right, selectedElement);

            toggleLeft.IsChecked = false;
            toggleCenter.IsChecked = false;
        }

        private void toggleUnderline_Checked(object sender, RoutedEventArgs e)
        {
            applyUnderline(true, selectedElement);
        }

        private void toggleUnderline_Unchecked(object sender, RoutedEventArgs e)
        {
            applyUnderline(false, selectedElement);
        }

        // Handler for drag stopping on leaving the window
        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            StopDragging();
            e.Handled = true;
        }

        // Handler for clearing element selection, adorner removal
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            cancelCanvasSelections();
        }

        // Handler for providing drag operation with selected element
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDown)
            {
                if ((_isDragging == false) &&
                    ((Math.Abs(e.GetPosition(getActiveCanvas()).X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance) ||
                    (Math.Abs(e.GetPosition(getActiveCanvas()).Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)))
                    _isDragging = true;

                if (_isDragging)
                {
                    Point position = Mouse.GetPosition(getActiveCanvas());
                    Canvas.SetTop(selectedElement, position.Y - (_startPoint.Y - _originalTop));
                    Canvas.SetLeft(selectedElement, position.X - (_startPoint.X - _originalLeft));
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Add event handlers to UI
            this.MouseLeftButtonDown += new MouseButtonEventHandler(Window_MouseLeftButtonDown);
            this.MouseLeftButtonUp += new MouseButtonEventHandler(DragFinishedMouseHandler);
            this.MouseMove += new MouseEventHandler(Window_MouseMove);
            this.MouseLeave += new MouseEventHandler(Window_MouseLeave);

            lstAddFields.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(lstAddFields_PreviewMouseLeftButtonDown);
            lstAddFields.PreviewMouseMove += new MouseEventHandler(lstAddFields_PreviewMouseMove);

            cnvCardFront.Drop += new DragEventHandler(Canvas_Drop);
            cnvCardFront.DragEnter += new DragEventHandler(Canvas_DragEnter);
            cnvCardFront.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(Canvas_PreviewMouseLeftButtonDown);
            cnvCardFront.PreviewMouseLeftButtonUp += new MouseButtonEventHandler(DragFinishedMouseHandler);

            cnvCardBack.Drop += new DragEventHandler(Canvas_Drop);
            cnvCardBack.DragEnter += new DragEventHandler(Canvas_DragEnter);
            cnvCardBack.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(Canvas_PreviewMouseLeftButtonDown);
            cnvCardBack.PreviewMouseLeftButtonUp += new MouseButtonEventHandler(DragFinishedMouseHandler);
        }

        // Handler for drag stopping on user choise
        private void DragFinishedMouseHandler(object sender, MouseButtonEventArgs e)
        {
            StopDragging();
            e.Handled = true;
        }

        // Method for stopping dragging
        private void StopDragging()
        {
            if (_isDown)
            {
                _isDown = false;
                _isDragging = false;

                CheckDragDropLocation(getActiveCanvas(), selectedElement);
            }
        }


        // ===========================================================================================================================
        // ===========================================================================================================================
        // Helper Methods
        // ===========================================================================================================================
        // ===========================================================================================================================

        private void addSavedItemToCanvas(Canvas canvas, CardItem cardItem)
        {
            var barcode = addFieldsList.ContainsKey("IdNumber") ? addFieldsList["IdNumber"] : null;

            if (string.IsNullOrEmpty(barcode))
            {
                barcode = addFieldsList.ContainsKey("Student Number") ? addFieldsList["Student Number"] : null;
            }

            switch (cardItem.FieldType)
            {
                case "BarcodeImage":

                    if (barcode == null)
                        barcode = "99999999";

                    Image img2 = new Image();
                    img2.Name = cardItem.FieldType;
                    //var bi = CanvasHelper.Draw2dBarcode(barcode);
                    //img2.Source = bi;
                    img2.DataContext = barcode;

                    Canvas.SetTop(img2, cardItem.Top);
                    Canvas.SetLeft(img2, cardItem.Left);
                    Canvas.SetZIndex(img2, cardItem.ZIndex);

                    canvas.Children.Add(img2);
                    canvas.UpdateLayout();

                    break;
                case "Bar_Code":
                    Label blbl = new Label();
                    blbl.Name = cardItem.FieldType;

                    if (barcode != null)
                        blbl.Content = "*" + barcode + "*";
                    else
                    {
                        blbl.Content = "*99999*";
                    }

                    //blbl.Content = "*" + addFieldsList["Student Number"] + "*";
                    blbl.FontFamily = new FontFamily(new Uri("pack://application:,,,/"), App.APP_FONT_BARCODE);
                    blbl.FontSize = cardItem.TextSize;
                    
                    if (cardItem.Width != 0)
                    {
                        blbl.Width = cardItem.Width;
                    }

                    if (cardItem.Height != 0)
                    {
                        blbl.Height = cardItem.Height;
                    }

                    Canvas.SetTop(blbl, cardItem.Top);
                    Canvas.SetLeft(blbl, cardItem.Left);
                    Canvas.SetZIndex(blbl, cardItem.ZIndex);

                    canvas.Children.Add(blbl);
                    canvas.UpdateLayout();

                    break;
                case "Photo_Image":
                case "Image":
                    string imgSource;

                    if (cardItem.FieldType.Equals("Image"))
                    {
                        imgSource = cardItem.Source;
                    }
                    else
                    {
                        imgSource = Path.Combine(SwipeUtils.getPhotoImageFolder(), addFieldsList["Photo Image"]);
                    }

                    Image img = new Image(); 
                    img.Name = cardItem.FieldType;

                    BitmapImage bitmapImg = SwipeUtils.getImageFile(imgSource);

                    if (bitmapImg != null)
                    {
                        img.Source = bitmapImg;
                        img.Width = cardItem.Width;
                        img.Height = cardItem.Height;

                        Canvas.SetTop(img, cardItem.Top);
                        Canvas.SetLeft(img, cardItem.Left);
                        Canvas.SetZIndex(img, cardItem.ZIndex);

                        canvas.Children.Add(img);
                        canvas.UpdateLayout();
                    }

                    break;

                case "Text_Entry":
                    TextBox te = new TextBox();
                    te.Name = cardItem.FieldType;
                    te.Text = cardItem.Text;

                    if (cardItem.Width != 0)
                    {
                        te.Width = cardItem.Width;
                    }

                    if (cardItem.Height != 0)
                    {
                        te.Height = cardItem.Height;
                    }

                    te.BorderThickness = new Thickness(0);
                    te.Foreground = new SolidColorBrush(SwipeUtils.ConvertHexStringToColour(cardItem.Foreground));
                    //te.Background = new SolidColorBrush(SwipeUtils.ConvertHexStringToColour(cardItem.Background));
                    
                    te.TextAlignment = (cardItem.Alignment.Equals("Center") ? TextAlignment.Center : (cardItem.Alignment.Equals("Right") ? TextAlignment.Right : TextAlignment.Left));
                    te.FontFamily = new FontFamily(cardItem.TextFont);
                    te.FontSize = cardItem.TextSize;
                    te.FontWeight = (cardItem.TextBold ? FontWeights.Bold : FontWeights.Regular);
                    te.FontStyle = (cardItem.TextItalic ? FontStyles.Italic : FontStyles.Normal);

                    if (cardItem.TextUnderline)
                    {
                        te.TextDecorations.Add(TextDecorations.Underline);
                    }

                    te.TextWrapping = TextWrapping.Wrap;
                    te.TextChanged += new System.Windows.Controls.TextChangedEventHandler(textBox_TextChanged);

                    Canvas.SetTop(te, cardItem.Top);
                    Canvas.SetLeft(te, cardItem.Left);
                    Canvas.SetZIndex(te, cardItem.ZIndex);

                    canvas.Children.Add(te);
                    canvas.UpdateLayout();

                    break;

                default:
                    TextBlock tb = new TextBlock();
                    tb.Name = cardItem.FieldType;

                    if (cardItem.Width != 0)
                    {
                        tb.Width = cardItem.Width;
                    }

                    if (cardItem.Height != 0)
                    {
                        tb.Height = cardItem.Height;
                    }

                    tb.Foreground = new SolidColorBrush(SwipeUtils.ConvertHexStringToColour(cardItem.Foreground));
                    tb.Background = new SolidColorBrush(SwipeUtils.ConvertHexStringToColour(cardItem.Background));
                    tb.TextAlignment = (cardItem.Alignment.Equals("Center") ? TextAlignment.Center : (cardItem.Alignment.Equals("Right") ? TextAlignment.Right : TextAlignment.Left));
                    tb.FontFamily = new FontFamily(cardItem.TextFont);
                    tb.FontSize = cardItem.TextSize;
                    tb.FontWeight = (cardItem.TextBold ? FontWeights.Bold : FontWeights.Regular);
                    tb.FontStyle = (cardItem.TextItalic ? FontStyles.Italic : FontStyles.Normal);

                    if (cardItem.TextUnderline)
                    {
                        tb.TextDecorations.Add(TextDecorations.Underline);
                    }

                    if (cardItem.FieldType.Equals("DOB"))
                    {
                        DateTime parsedDate;
                        DateTime.TryParseExact(addFieldsList["DOB"], "MM-dd-yyyy", null, DateTimeStyles.None, out parsedDate);
                        tb.Text = parsedDate.ToString("MM-dd-yyyy");
                    }
                    else
                    {
                        string key = cardItem.FieldType.Replace("_", " ");
                        if (!addFieldsList.ContainsKey(key))
                        {
                            return;
                        }
                        tb.Text = addFieldsList[key];
                    }

                    Canvas.SetTop(tb, cardItem.Top);
                    Canvas.SetLeft(tb, cardItem.Left);
                    Canvas.SetZIndex(tb, cardItem.ZIndex);

                    canvas.Children.Add(tb);
                    canvas.UpdateLayout();

                    break;
            }
        }

        private void applyBold(bool isBold, UIElement element)
        {
            if (element is TextBox)
            {
                TextBox tb = (TextBox)element;
                tb.FontWeight = (isBold ? FontWeights.Bold : FontWeights.Regular);
                textBox_TextChanged(tb, null);
            }
            else if (element is TextBlock)
            {
                TextBlock txtBlk = (TextBlock)element;
                txtBlk.FontWeight = (isBold ? FontWeights.Bold : FontWeights.Regular);
            }
            else if (element is Label)
            {
                Label lbl = (Label)element;
                lbl.FontWeight = (isBold ? FontWeights.Bold : FontWeights.Regular);
            }
        }

        private void applyItalic(bool isItalic, UIElement element)
        {
            if (element is TextBox)
            {
                TextBox tb = (TextBox)element;
                tb.FontStyle = (isItalic ? FontStyles.Italic : FontStyles.Normal);
                textBox_TextChanged(tb, null);
            }
            else if (element is TextBlock)
            {
                TextBlock txtBlk = (TextBlock)element;
                txtBlk.FontStyle = (isItalic ? FontStyles.Italic : FontStyles.Normal);
            }
            else if (element is Label)
            {
                Label lbl = (Label)element;
                lbl.FontStyle = (isItalic ? FontStyles.Italic : FontStyles.Normal);
            }
        }

        private void applyUnderline(bool isUnderline, UIElement element)
        {
            if (element is TextBox)
            {
                TextBox tb = (TextBox)element;
                if (isUnderline)
                {
                    tb.TextDecorations.Add(TextDecorations.Underline);
                }
                else
                {
                    tb.TextDecorations.Remove(TextDecorations.Underline[0]);
                }
            }
            else if (element is TextBlock)
            {
                TextBlock txtBlk = (TextBlock)element;
                if (isUnderline)
                {
                    txtBlk.TextDecorations.Add(TextDecorations.Underline);
                }
                else
                {
                    txtBlk.TextDecorations.Clear();
                }
            }
        }

        private void applyFont(string font, UIElement element)
        {
            if (element is TextBox)
            {
                TextBox tb = (TextBox)element;
                tb.FontFamily = new FontFamily(font);
                textBox_TextChanged(tb, null);
            }
            else if (element is TextBlock)
            {
                TextBlock txtBlk = (TextBlock)element;
                txtBlk.FontFamily = new FontFamily(font);
            }
        }

        private void applyFontColor(Color color, UIElement element)
        {
            if (element is TextBox)
            {
                TextBox tb = (TextBox)element;
                tb.Foreground = new SolidColorBrush(color);
            }
            else if (element is TextBlock)
            {
                TextBlock txtBlk = (TextBlock)element;
                txtBlk.Foreground = new SolidColorBrush(color);
            }
            else if (element is Label)
            {
                Label lbl = (Label)element;
                lbl.Foreground = new SolidColorBrush(color); 
            }
        }

        private void applyFontSize(double fontSize, UIElement element)
        {
            if (element is TextBox)
            {
                TextBox tb = (TextBox)element;
                tb.FontSize = fontSize;
                textBox_TextChanged(tb, null);
            }
            else if (element is TextBlock)
            {
                TextBlock txtBlk = (TextBlock)element;
                txtBlk.FontSize = fontSize;
            }
            else if (element is Label)
            {
                Label lbl = (Label)element;
                lbl.FontSize = fontSize;
            }
        }

        private void applyTextAlignment(TextAlignment align, UIElement element)
        {
            if (element is TextBox)
            {
                TextBox tb = (TextBox)element;
                tb.TextAlignment = align;
            }
            else if (element is TextBlock)
            {
                TextBlock txtBlk = (TextBlock)element;
                txtBlk.TextAlignment = align;
            }
        }

        private void applyTextHighlightColor(Color color, UIElement element)
        {
            if (element is TextBox)
            {
                TextBox tb = (TextBox)element;
                tb.Background = new SolidColorBrush(color); 
            }
            else if (element is TextBlock)
            {
                TextBlock txtBlk = (TextBlock)element;
                txtBlk.Background = new SolidColorBrush(color);
            }
            else if (element is Label)
            {
                Label lbl = (Label)element;
                lbl.Background = new SolidColorBrush(color); 
            }
        }

        private void buildCardItemList(Canvas c, List<CardItem> cardItems, string side)
        {
            foreach (UIElement uie in c.Children)
            {
                CardItem item = new CardItem();
                item.Side = side;
                item.Top = Canvas.GetTop(uie);
                item.Left = Canvas.GetLeft(uie);
                item.ZIndex = Canvas.GetZIndex(uie);

                if (uie is Label)
                {
                    Label lbl = (Label)uie;
                    item.ControlType = lbl.GetType().ToString();
                    item.FieldType = lbl.Name;

                    if (!Double.IsNaN(lbl.Width))
                    {
                        item.Width = lbl.Width;
                    }
                    if (!Double.IsNaN(lbl.Height))
                    {
                        item.Height = lbl.Height;
                    }

                    item.Foreground = SwipeUtils.GetColor(lbl.Foreground).ToString();
                    item.Background = SwipeUtils.GetColor(lbl.Background).ToString();
                    item.TextFont = lbl.FontFamily.Source;
                    item.TextSize = lbl.FontSize;
                    item.TextBold = lbl.FontWeight.Equals(FontWeights.Bold);
                    item.TextItalic = lbl.FontStyle.Equals(FontStyles.Italic);
                }
                else if (uie is Image)
                {
                    Image img = (Image)uie;
                    item.ControlType = img.GetType().ToString();
                    item.FieldType = img.Name;
                    item.Source = (img.Source as BitmapImage).UriSource.AbsoluteUri;

                    if (!Double.IsNaN(img.Width))
                    {
                        item.Width = img.Width;
                    }
                    if (!Double.IsNaN(img.Height))
                    {
                        item.Height = img.Height;
                    }
                }
                else if (uie is TextBox)
                {
                    TextBox txt = (TextBox)uie;
                    item.ControlType = txt.GetType().ToString();
                    item.FieldType = txt.Name;

                    if (!Double.IsNaN(txt.Width))
                    {
                        item.Width = txt.Width;
                    }
                    if (!Double.IsNaN(txt.Height))
                    {
                        item.Height = txt.Height;
                    }
                        
                    item.Foreground = SwipeUtils.GetColor(txt.Foreground).ToString();
                    item.Background = SwipeUtils.GetColor(txt.Background).ToString();
                    item.Text = txt.Text;
                    item.Alignment = txt.TextAlignment.ToString();
                    item.TextFont = txt.FontFamily.Source;
                    item.TextSize = txt.FontSize;
                    item.TextBold = txt.FontWeight.Equals(FontWeights.Bold);
                    item.TextItalic = txt.FontStyle.Equals(FontStyles.Italic);
                    item.TextUnderline = txt.TextDecorations.Contains(TextDecorations.Underline[0]);
                }
                else if (uie is TextBlock)
                {
                    TextBlock tb = (TextBlock)uie;
                    item.ControlType = tb.GetType().ToString();
                    item.FieldType = tb.Name;

                    if (!Double.IsNaN(tb.Width))
                    {
                        item.Width = tb.Width;
                    }
                    if (!Double.IsNaN(tb.Height))
                    {
                        item.Height = tb.Height;
                    }

                    item.Foreground = SwipeUtils.GetColor(tb.Foreground).ToString();
                    item.Background = SwipeUtils.GetColor(tb.Background).ToString();
                    item.Alignment = tb.TextAlignment.ToString();
                    item.TextFont = tb.FontFamily.Source;
                    item.TextSize = tb.FontSize;
                    item.TextBold = tb.FontWeight.Equals(FontWeights.Bold);
                    item.TextItalic = tb.FontStyle.Equals(FontStyles.Italic);
                    item.TextUnderline = tb.TextDecorations.Contains(TextDecorations.Underline[0]);
                }

                cardItems.Add(item);
            }
        }

        private void cancelCanvasSelections()
        {
            // Remove selection on clicking anywhere the window
            if (selected)
            {
                selected = false;
                if (selectedElement != null)
                {
                    // Remove the adorner from the selected element
                    aLayer.Remove(aLayer.GetAdorners(selectedElement)[0]);
                    selectedElement = null;
                    updateTextToolbar();
                }
            }
        }

        private void CheckDragDropLocation(Canvas cnv, UIElement element)
        {
            if (element != null)
            {
                double elementWidth = element.RenderSize.Width;
                double elementHeight = element.RenderSize.Height;

                double left = Canvas.GetLeft(element);
                double top = Canvas.GetTop(element);

                if (top < 0)
                {
                    // Item too far up
                    Canvas.SetTop(element, 0);
                }

                if ((top + elementHeight) > cnv.Height)
                {
                    // Item too far down
                    Canvas.SetTop(element, Math.Floor(cnv.Height - elementHeight));
                }

                if (left < 0)
                {
                    // Item too far left
                    Canvas.SetLeft(element, 0);
                }

                if ((left + elementWidth) > cnv.Width)
                {
                    // Item too far right
                    Canvas.SetLeft(element, Math.Floor(cnv.Width - elementWidth));
                }
            }
        }

        private void clearCanvasFields()
        {
            cancelCanvasSelections();
            cnvCardFront.Children.Clear();
            cnvCardBack.Children.Clear();
            cnvCardFront.Background = new SolidColorBrush(Colors.White);
            cnvCardBack.Background = new SolidColorBrush(Colors.White);
        }

        public void DropOnCanvas(Canvas targetCanvas, KeyValuePair<string, string> item, Point dropPoint)
        {
            switch (item.Key)
            {
                case "Bar Code":
                    Label blbl = new Label();
                    blbl.Name = item.Key.Replace(" ", "_");
                    blbl.Content = "*" + item.Value + "*";
                    blbl.FontFamily = new FontFamily(new Uri("pack://application:,,,/"), App.APP_FONT_BARCODE);
                    blbl.FontSize = 48;
                    
                    targetCanvas.Children.Add(blbl);
                    targetCanvas.UpdateLayout();

                    double bcTop = getActiveCanvas().Height - ((UIElement)blbl).RenderSize.Height;
                    double bcLeft = (getActiveCanvas().Width - ((UIElement)blbl).RenderSize.Width) / 2;

                    Canvas.SetTop(blbl, bcTop);
                    Canvas.SetLeft(blbl, bcLeft);
                    Canvas.SetZIndex(blbl, zIndex++);

                    break;
                case "2D Bar Code":
                    QrEncoder encoder = new QrEncoder(ErrorCorrectionLevel.M);

                    QrCode qrCode;
                   
                    encoder.TryEncode(item.Value, out qrCode);

                    WriteableBitmapRenderer wRenderer = new WriteableBitmapRenderer(new FixedModuleSize(2, QuietZoneModules.Two), Colors.Black, Colors.White);
                    WriteableBitmap wBitmap = new WriteableBitmap(70, 70, 96, 96, PixelFormats.Gray8, null);
                    wRenderer.Draw(wBitmap, qrCode.Matrix);

                    Image img2 = new Image();
                    img2.Source = wBitmap;
                   
                    targetCanvas.Children.Add(img2);
                    targetCanvas.UpdateLayout();

                    double bcTop2 = getActiveCanvas().Height - ((UIElement)img2).RenderSize.Height;
                    double bcLeft2 = (getActiveCanvas().Width - ((UIElement)img2).RenderSize.Width) / 2;

                    Canvas.SetTop(img2, bcTop2);
                    Canvas.SetLeft(img2, bcLeft2);
                    Canvas.SetZIndex(img2, zIndex++);


                    break;
                case "Photo Image":
                    Image photo = new Image();
                    photo.Name = item.Key.Replace(" ", "_");
                    
                    BitmapImage bmpImgPhoto = SwipeUtils.getImageFile(Path.Combine(SwipeUtils.getPhotoImageFolder(), item.Value));

                    if (bmpImgPhoto != null)
                    {
                        photo.Source = bmpImgPhoto;

                        double photoFactor = getImageScaleFactor(bmpImgPhoto, targetCanvas);
                        photo.Width = bmpImgPhoto.Width * photoFactor;
                        photo.Height = bmpImgPhoto.Height * photoFactor;

                        Canvas.SetTop(photo, dropPoint.Y);
                        Canvas.SetLeft(photo, dropPoint.X);
                        Canvas.SetZIndex(photo, zIndex++);

                        targetCanvas.Children.Add(photo);
                        targetCanvas.UpdateLayout();

                        CheckDragDropLocation(targetCanvas, photo);
                    }

                    break;

                case "<Image>":

                    string imgFile = SwipeUtils.OpenImageDialogForm();

                    if (imgFile != null)
                    {
                        Image img = new Image();
                        img.Name = "Image";

                        BitmapImage bmpImg = SwipeUtils.getImageFile(imgFile);
                        img.Source = bmpImg;

                        double imgFactor = getImageScaleFactor(bmpImg, targetCanvas);
                        img.Width = bmpImg.Width * imgFactor;
                        img.Height = bmpImg.Height * imgFactor;

                        Canvas.SetTop(img, dropPoint.Y);
                        Canvas.SetLeft(img, dropPoint.X);
                        Canvas.SetZIndex(img, zIndex++);

                        targetCanvas.Children.Add(img);
                        targetCanvas.UpdateLayout();

                        CheckDragDropLocation(targetCanvas, img);
                    }

                    break;

                case "<Text Entry>":
                    TextBox te = new TextBox();
                    te.Name = "Text_Entry";
                    te.Text = "<Text Entry>";
                    te.Width = App.APP_DEFAULT_TEXTBOX_WIDTH;
                    te.BorderThickness = new Thickness(0);
                    te.TextWrapping = TextWrapping.Wrap;
                    te.TextChanged += new System.Windows.Controls.TextChangedEventHandler(textBox_TextChanged);

                    Canvas.SetTop(te, dropPoint.Y);
                    Canvas.SetLeft(te, dropPoint.X);
                    Canvas.SetZIndex(te, zIndex++);

                    targetCanvas.Children.Add(te);
                    targetCanvas.UpdateLayout();

                    CheckDragDropLocation(targetCanvas, te);

                    break;

                case "DOB":
                    DateTime parsedDate;
                    DateTime.TryParseExact(item.Value, "MM-dd-yyyy", null, DateTimeStyles.None, out parsedDate);

                    TextBlock dob = new TextBlock();
                    dob.Name = item.Key.Replace(" ", "_");
                    dob.Text = parsedDate.ToString("MM-dd-yyyy");
                    dob.TextAlignment = TextAlignment.Left;
                    dob.Background = new SolidColorBrush(Colors.Transparent);
                    dob.Foreground = new SolidColorBrush(Colors.Black);

                    Canvas.SetTop(dob, dropPoint.Y);
                    Canvas.SetLeft(dob, dropPoint.X);
                    Canvas.SetZIndex(dob, zIndex++);

                    targetCanvas.Children.Add(dob);
                    targetCanvas.UpdateLayout();

                    CheckDragDropLocation(targetCanvas, dob);

                    break;

                default:
                    TextBlock tb = new TextBlock();
                    tb.Name = item.Key.Replace(" ", "_");
                    tb.Text = item.Value;
                    tb.TextAlignment = TextAlignment.Left;
                    tb.Background = new SolidColorBrush(Colors.Transparent);
                    tb.Foreground = new SolidColorBrush(Colors.Black);

                    Canvas.SetTop(tb, dropPoint.Y);
                    Canvas.SetLeft(tb, dropPoint.X);
                    Canvas.SetZIndex(tb, zIndex++);

                    targetCanvas.Children.Add(tb);
                    targetCanvas.UpdateLayout();

                    CheckDragDropLocation(targetCanvas, tb);

                    break;
            }
        }

        private Canvas getActiveCanvas()
        {
            if (Convert.ToBoolean(rbCardFront.IsChecked))
            {
                return cnvCardFront;
            }
            else
            {
                return cnvCardBack;
            }
        }

        private string getCanvasBackgroundFile(Canvas c)
        {
            if (c != null && c.Background is ImageBrush)
            {
                ImageBrush ib = (ImageBrush)c.Background;
                BitmapImage img = (BitmapImage)ib.ImageSource;
                return img.UriSource.AbsolutePath.Replace("%20", " ").Replace("/", "\\");
            }
            else
            {
                return null;
            }
        }

        private double getImageScaleFactor(BitmapImage bmpImg, Canvas c)
        {
            if (bmpImg.Width > c.Width || bmpImg.Height > c.Height)
            {
                // Get Width percentage scale
                double pctWidth = c.Width / bmpImg.Width;

                // Get Height percentage scale
                double pctHeight = c.Height / bmpImg.Height;

                // Get the smallest factor
                return Math.Min(pctWidth, pctHeight);
            }
            return 1;
        }

        private bool isCardDualSided()
        {
            return getCanvasBackgroundFile(cnvCardBack) != null || ((cnvCardFront.Children.Count > 0) && (cnvCardBack.Children.Count > 0));
        }

        private Dictionary<string, string> loadAddFieldsList(string cardType, bool tempCard)
        {
            int personID = 0;
            DataTable table = null;
            Dictionary<string, string> addFields = new Dictionary<string, string>();

            if (cardType.Equals(App.APP_CARD_TYPE_STUDENT))
            {
                personID = Convert.ToInt32(Application.Current.Properties[App.APP_KEY_STUDENT_ID]);

                if (tempCard)
                {
                    table = scBLL.GetTempStudentCard(personID);
                }
                else
                {
                    table = scBLL.GetStudentCard(personID);
                }
            }

            if (cardType.Equals(App.APP_CARD_TYPE_TEACHER))
            {
                personID = Convert.ToInt32(Application.Current.Properties[App.APP_KEY_TEACHER_ID]);
                table = scBLL.GetTeacherCard(personID);
            }

            switch (table.Rows.Count)
            {
                case 0:
                    log.Error("Error loading ID card field list. No rows returned.");
                    break;
                case 1:
                    string textEntry = "<Text Entry>";
                    addFields.Add(textEntry, textEntry);

                    string logoPicture = "<Image>";
                    addFields.Add(logoPicture, logoPicture);

                    DataRow row = table.Rows[0];

                    foreach (DataColumn col in row.Table.Columns)
                    {
                        addFields.Add(col.ToString(), row[col].ToString());
                    }

                    break;
                default:
                    log.Error("Error loading ID card field list. " + table.Rows.Count + " rows returned.");
                    break;
            }

            return addFields;
        }

        private void loadCardItems(List<CardItem> cardItems)
        {
            foreach (CardItem ci in cardItems)
            {
                try
                {
                    if (ci.Side.Equals(App.APP_CARD_FRONT))
                    {
                        addSavedItemToCanvas(cnvCardFront, ci);
                    }
                    else if (ci.Side.Equals(App.APP_CARD_BACK))
                    {
                        addSavedItemToCanvas(cnvCardBack, ci);
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ci.Name, ex);
                }
            }
        }

        private void loadIDCard(int cardID)
        {
            // Clear canvas first
            clearCanvasFields();

            // Clear background textbox
            txtBackground.Text = "";

            SwipeCard.IDCardsDataTable idTable = scBLL.GetCardById(cardID);
            updateControls(true);

            // Update Card Name in card information area
            txtBlkCardName.Text = idTable[0].CardName;

            // Update Card type in card information area
            if (idTable[0].StudentCard)
            {
                txtBlkCardType.Text = App.APP_CARD_TYPE_STUDENT;
            }
            else if (idTable[0].TeacherCard)
            {
                txtBlkCardType.Text = App.APP_CARD_TYPE_TEACHER;
            }
            else if (idTable[0].OtherCard)
            {
                txtBlkCardType.Text = App.APP_CARD_TYPE_OTHER;
            }
            else
            {
                txtBlkCardType.Text = "";
            }

            // Update Front Canvas layout and card information area
            if (idTable[0].FrontPortrait)
            {
                txtBlkFrontOrientation.Text = App.APP_CARD_ORIENTATION_PORTRAIT;

                cnvCardFront.Width = App.APP_CARD_SHORT_SIDE;
                FrontCanvasBorder.Width = App.APP_CARD_SHORT_SIDE + 2;

                cnvCardFront.Height = App.APP_CARD_LONG_SIDE;
                FrontCanvasBorder.Height = App.APP_CARD_LONG_SIDE + 2;
            }
            else
            {
                txtBlkFrontOrientation.Text = App.APP_CARD_ORIENTATION_LANDSCAPE;

                cnvCardFront.Width = App.APP_CARD_LONG_SIDE;
                FrontCanvasBorder.Width = App.APP_CARD_LONG_SIDE + 2;

                cnvCardFront.Height = App.APP_CARD_SHORT_SIDE;
                FrontCanvasBorder.Height = App.APP_CARD_SHORT_SIDE + 2;
            }

            // Update Back Canvas layout and card information area
            if (idTable[0].BackPortrait)
            {
                txtBlkBackOrientation.Text = App.APP_CARD_ORIENTATION_PORTRAIT;

                cnvCardBack.Width = App.APP_CARD_SHORT_SIDE;
                BackCanvasBorder.Width = App.APP_CARD_SHORT_SIDE + 2;

                cnvCardBack.Height = App.APP_CARD_LONG_SIDE;
                BackCanvasBorder.Height = App.APP_CARD_LONG_SIDE + 2;
            }
            else
            {
                txtBlkBackOrientation.Text = App.APP_CARD_ORIENTATION_LANDSCAPE;

                cnvCardBack.Width = App.APP_CARD_LONG_SIDE;
                BackCanvasBorder.Width = App.APP_CARD_LONG_SIDE + 2;

                cnvCardBack.Height = App.APP_CARD_SHORT_SIDE;
                BackCanvasBorder.Height = App.APP_CARD_SHORT_SIDE + 2;
            }

            // Always default to Front of card when first loaded
            rbCardFront.IsChecked = true;

            // Update Temporary Card option in card information area
            bool tempCard = idTable[0].TempCard;
            chkTempCard.IsChecked = tempCard;

            // Add appropriate data fields to listbox
            populateFieldList(txtBlkCardType.Text, tempCard);

            // Update Background Image and Opacity 
            if (idTable[0].FrontBackground != null)
            {
                updateCanvasBackground(cnvCardFront, idTable[0].FrontBackground, idTable[0].FrontOpacity);
                txtBackground.Text = idTable[0].FrontBackground;
                sldOpacity.Value = idTable[0].FrontOpacity;
            }
            if (idTable[0].BackBackground != null)
            {
                updateCanvasBackground(cnvCardBack, idTable[0].BackBackground, idTable[0].BackOpacity);
            }

            // Update card fields on canvases
            if (idTable[0].Fields != null)
            {
                List<CardItem> cardItems = (List<CardItem>)Serialisation.DeserializeObject<List<CardItem>>(idTable[0].Fields);
                loadCardItems(cardItems);
            }
        }

        private void populateFieldList(string cardType, bool tempCard)
        {
            // Get data and fill in local ArrayList
            addFieldsList = loadAddFieldsList(cardType, tempCard);

            // Bind ArrayList with the ListBox
            lstAddFields.ItemsSource = addFieldsList;
        }

        private void positionField(string postionTag)
        {
            if (selectedElement != null)
            {
                double top = 0;
                double left = 0;

                switch (postionTag)
                {
                    case "TL":
                    case "TC":
                    case "TR":
                        top = 0;
                        break;

                    case "ML":
                    case "MC":
                    case "MR":
                        top = (getActiveCanvas().Height - selectedElement.RenderSize.Height) / 2;
                        break;

                    case "BL":
                    case "BC":
                    case "BR":
                        top = getActiveCanvas().Height - selectedElement.RenderSize.Height;
                        break;
                }

                switch (postionTag)
                {
                    case "TL":
                    case "ML":
                    case "BL":
                        left = 0;
                        break;

                    case "TC":
                    case "MC":
                    case "BC":
                        left = (getActiveCanvas().Width - selectedElement.RenderSize.Width) / 2;
                        break;

                    case "TR":
                    case "MR":
                    case "BR":
                        left = getActiveCanvas().Width - selectedElement.RenderSize.Width;
                        break;
                }

                Canvas.SetTop(selectedElement, top);
                Canvas.SetLeft(selectedElement, left);
                getActiveCanvas().UpdateLayout();
            }
        }

        private void resetToBeginning()
        {
            // Clear front and back canvas
            clearCanvasFields();

            // Update buttons
            updateControls(false);

            // Reset labels
            txtBlkCardName.Text = "";
            txtBlkCardType.Text = "";
            txtBlkFrontOrientation.Text = "";
            txtBlkBackOrientation.Text = "";
            chkTempCard.IsChecked = false;

            // Always default to Front of card when first loaded
            rbCardFront.IsChecked = true;

            // Reset add fields list
            lstAddFields.ItemsSource = null;
        }

        private void updateBackgroundControls()
        {
            string bgFile = getCanvasBackgroundFile(getActiveCanvas());

            if (bgFile != null)
            {
                txtBackground.Text = bgFile;
                sldOpacity.Value = getActiveCanvas().Background.Opacity;
            }
            else
            {
                if (txtBackground != null)
                {
                    txtBackground.Text = "";
                }
            }
        }

        private void updateCanvasBackground(Canvas c, string uri, double opacity)
        {
            ImageBrush ib = new ImageBrush();
            ib.ImageSource = SwipeUtils.getImageFile(uri);
            c.Background = ib;
            c.Background.Opacity = opacity;
            c.UpdateLayout();
        }

        private void updateControls(bool isIDCardLoaded)
        {
            btnDuplicate.IsEnabled = isIDCardLoaded;
            btnDelete.IsEnabled = isIDCardLoaded;
            btnBatch.IsEnabled = isIDCardLoaded;
            btnSave.IsEnabled = isIDCardLoaded;
            btnPrint.IsEnabled = isIDCardLoaded;
            gbBackground.IsEnabled = isIDCardLoaded;
            spFrontBack.IsEnabled = isIDCardLoaded;
        }

        private void setCardDefaults()
        {
            int schoolID = 0;

            // Set default school to first in list
            Dictionary<int, string> schoolList = SwipeUtils.getSchoolsList();

            if (schoolList.Count > 0)
            {
                schoolID = schoolList.Keys.First<int>();
                App.setCurrentSchoolID(schoolID);

                // Set default student to first in list
                Dictionary<int, string> studentList = SwipeUtils.getStudentsList(schoolID);

                if (studentList.Count > 0)
                {
                    App.setCurrentStudentID(studentList.Keys.First<int>());
                }

                // Set default teacher to first in list
                Dictionary<int, string> teacherList = SwipeUtils.getTeachersList(schoolID);

                if (teacherList.Count > 0)
                {
                    App.setCurrentTeacherID(teacherList.Keys.First<int>());
                }
            }
        }

        private void updateTextToolbar()
        {
            if (selectedElement != null)
            {
                // Item is selected
                tbtFontStyle.IsEnabled = true;

                if (selectedElement is TextBox)
                {
                    TextBox tb = (TextBox)selectedElement;

                    toggleBold.IsChecked = tb.FontWeight.Equals(FontWeights.Bold);
                    toggleBold.IsEnabled = true;

                    toggleItalic.IsChecked = tb.FontStyle.Equals(FontStyles.Italic);
                    toggleItalic.IsEnabled = true;

                    toggleUnderline.IsChecked = tb.TextDecorations.Contains(TextDecorations.Underline[0]);
                    toggleUnderline.IsEnabled = true;

                    cboFontFamily.SelectedValue = tb.FontFamily;
                    cboFontFamily.IsEnabled = true;

                    cboFontSize.Text = tb.FontSize.ToString();
                    cboFontSize.IsEnabled = true;

                    toggleLeft.IsChecked = tb.TextAlignment.Equals(TextAlignment.Left);
                    toggleLeft.IsEnabled = true;

                    toggleCenter.IsChecked = tb.TextAlignment.Equals(TextAlignment.Center);
                    toggleCenter.IsEnabled = true;

                    toggleRight.IsChecked = tb.TextAlignment.Equals(TextAlignment.Right);
                    toggleRight.IsEnabled = true;

                    cpFont.SelectedColor = SwipeUtils.GetColor(tb.Foreground);
                    cpFont.IsEnabled = true;

                    cpTextBG.SelectedColor = SwipeUtils.GetColor(tb.Background);
                    cpTextBG.IsEnabled = true;
                }
                else if (selectedElement is TextBlock)
                {
                    TextBlock txtBlk = (TextBlock)selectedElement;

                    toggleBold.IsChecked = txtBlk.FontWeight.Equals(FontWeights.Bold);
                    toggleBold.IsEnabled = true;

                    toggleItalic.IsChecked = txtBlk.FontStyle.Equals(FontStyles.Italic);
                    toggleItalic.IsEnabled = true;

                    toggleUnderline.IsChecked = txtBlk.TextDecorations.Contains(TextDecorations.Underline[0]);
                    toggleUnderline.IsEnabled = true;

                    cboFontFamily.SelectedValue = txtBlk.FontFamily;
                    cboFontFamily.IsEnabled = true;

                    cboFontSize.Text = txtBlk.FontSize.ToString();
                    cboFontSize.IsEnabled = true;

                    toggleLeft.IsChecked = txtBlk.TextAlignment.Equals(TextAlignment.Left);
                    toggleLeft.IsEnabled = true;

                    toggleCenter.IsChecked = txtBlk.TextAlignment.Equals(TextAlignment.Center);
                    toggleCenter.IsEnabled = true;

                    toggleRight.IsChecked = txtBlk.TextAlignment.Equals(TextAlignment.Right);
                    toggleRight.IsEnabled = true;

                    cpFont.SelectedColor = SwipeUtils.GetColor(txtBlk.Foreground);
                    cpFont.IsEnabled = true;

                    cpTextBG.SelectedColor = SwipeUtils.GetColor(txtBlk.Background);
                    cpTextBG.IsEnabled = true;
                }
                else if (selectedElement is Label)
                {
                    // Only barcode is label
                    Label lbl = (Label)selectedElement;

                    toggleBold.IsChecked = false;
                    toggleBold.IsEnabled = false;

                    toggleItalic.IsChecked = false;
                    toggleItalic.IsEnabled = false;

                    toggleUnderline.IsChecked = false;
                    toggleUnderline.IsEnabled = false;

                    cboFontFamily.SelectedIndex = -1;
                    cboFontFamily.IsEnabled = false;

                    cboFontSize.Text = lbl.FontSize.ToString();
                    cboFontSize.IsEnabled = true;

                    toggleLeft.IsChecked = false;
                    toggleLeft.IsEnabled = false;

                    toggleCenter.IsChecked = false;
                    toggleCenter.IsEnabled = false;

                    toggleRight.IsChecked = false;
                    toggleRight.IsEnabled = false;

                    cpFont.SelectedColor = SwipeUtils.GetColor(lbl.Foreground);
                    cpFont.IsEnabled = false;

                    cpTextBG.SelectedColor = SwipeUtils.GetColor(lbl.Background);
                    cpTextBG.IsEnabled = false;
                }
            }
            else
            {
                // No items selected
                tbtFontStyle.IsEnabled = false;

                toggleBold.IsChecked = false;
                toggleItalic.IsChecked = false;
                toggleUnderline.IsChecked = false;

                cboFontFamily.SelectedIndex = -1;
                cboFontSize.SelectedIndex = -1;

                toggleLeft.IsChecked = false;
                toggleCenter.IsChecked = false;
                toggleRight.IsChecked = false;

                cpFont.IsEnabled = false;
                cpFont.SelectedColor = Colors.Black;

                cpTextBG.IsEnabled = false;
                cpTextBG.SelectedColor = Colors.White;
            }
        }

    }
}
