using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using log4net;
using SwipeDesktop;

namespace SwipeK12
{
    class SwipeUtils
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static double getNewTextboxWidth(TextBox tb)
        {
            return Math.Ceiling(MeasureTextSize(tb.Text, tb.FontFamily, tb.FontStyle, tb.FontWeight, tb.FontStretch, tb.FontSize).Width + 10);
        }

        public static Size MeasureTextSize(string text, FontFamily fontFamily, FontStyle fontStyle, FontWeight fontWeight, FontStretch fontStretch, double fontSize)
        {
            FormattedText ft = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(fontFamily, fontStyle, fontWeight, fontStretch), fontSize, Brushes.Black);
            return new Size(ft.Width, ft.Height);
        }

        public static Color GetColor(Brush brush)
        {
            SolidColorBrush newBrush = (SolidColorBrush)brush;

            var blck = Brushes.White;
            
            return newBrush == null ? blck.Color : newBrush.Color;
        }

        public static Dictionary<int, string> getSchoolsList()
        {
            Dictionary<int, string> schoolList = new Dictionary<int, string>();

            try
            {
                SwipeCardBLL scBLL = new SwipeCardBLL();
                SwipeDesktop.IdCardUtils.SwipeCard.SchoolDataTable schoolTable = scBLL.GetAllSchools();

                foreach (DataRow row in schoolTable.Rows)
                {
                    schoolList.Add(Convert.ToInt32(row[App.APP_DB_FIELD_SCHOOL_ID]), row[App.APP_DB_FIELD_SCHOOL_NAME].ToString());
                }
            }
            catch (Exception ex)
            {
                log.Error("Error loading school list", ex);
                MessageBox.Show("An unexpected error occurred when attempting to load school list. Please contact your system support. Error message returned: " + ex.Message, "Error Loading Schools", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return schoolList;
        }

        public static Dictionary<int, string> getStudentsList(int schoolID)
        {
            Dictionary<int, string> studentsList = new Dictionary<int, string>();

            try
            {
                SwipeCardBLL scBLL = new SwipeCardBLL();
                SwipeDesktop.IdCardUtils.SwipeCard.PersonDataTable personTable = scBLL.GetAllActiveStudentsBySchool(schoolID);

                foreach (DataRow row in personTable.Rows)
                {
                    studentsList.Add(Convert.ToInt32(row[App.APP_DB_FIELD_PERSON_ID]), row[App.APP_DB_FIELD_DISPLAY_NAME].ToString());
                }
            }
            catch (Exception ex)
            {
                log.Error("Error loading students list", ex);
                MessageBox.Show("An unexpected error occurred when attempting to load students list. Please contact your system support. Error message returned: " + ex.Message, "Error Loading Students", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return studentsList;
        }

        public static Dictionary<int, string> getTeachersList(int schoolID)
        {
            Dictionary<int, string> teachersList = new Dictionary<int, string>();

            try
            {
                SwipeCardBLL scBLL = new SwipeCardBLL();
                SwipeDesktop.IdCardUtils.SwipeCard.PersonDataTable personTable = scBLL.GetAllActiveTeachersBySchool(schoolID);

                foreach (DataRow row in personTable.Rows)
                {
                    teachersList.Add(Convert.ToInt32(row[App.APP_DB_FIELD_PERSON_ID]), row[App.APP_DB_FIELD_DISPLAY_NAME].ToString());
                }
            }
            catch (Exception ex)
            {
                log.Error("Error loading teachers list", ex);
                MessageBox.Show("An unexpected error occurred when attempting to load teachers list. Please contact your system support. Error message returned: " + ex.Message, "Error Loading Teachers", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return teachersList;
        }
        public static Dictionary<int, string> getStaff(int schoolID)
        {
            Dictionary<int, string> teachersList = new Dictionary<int, string>();

            try
            {
                SwipeCardBLL scBLL = new SwipeCardBLL();
                SwipeDesktop.IdCardUtils.SwipeCard.PersonDataTable personTable = scBLL.GetStaff(schoolID);

                foreach (DataRow row in personTable.Rows)
                {
                    teachersList.Add(Convert.ToInt32(row[App.APP_DB_FIELD_PERSON_ID]), row[App.APP_DB_FIELD_DISPLAY_NAME].ToString());
                }
            }
            catch (Exception ex)
            {
                log.Error("Error loading staff list", ex);
                MessageBox.Show("An unexpected error occurred when attempting to load staff list. Please contact your system support. Error message returned: " + ex.Message, "Error Loading Teachers", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return teachersList;
        }

        public static Color ConvertHexStringToColour(string hexString)
        {
            byte a = 0;
            byte r = 0;
            byte g = 0;
            byte b = 0;

            if (hexString != null && hexString.StartsWith("#"))
            {
                hexString = hexString.Substring(1, 8);


                a = Convert.ToByte(Int32.Parse(hexString.Substring(0, 2), System.Globalization.NumberStyles.AllowHexSpecifier));
                r = Convert.ToByte(Int32.Parse(hexString.Substring(2, 2), System.Globalization.NumberStyles.AllowHexSpecifier));
                g = Convert.ToByte(Int32.Parse(hexString.Substring(4, 2), System.Globalization.NumberStyles.AllowHexSpecifier));
                b = Convert.ToByte(Int32.Parse(hexString.Substring(6, 2), System.Globalization.NumberStyles.AllowHexSpecifier));
            }

            return Color.FromArgb(a, r, g, b);
        }

        public static string OpenImageDialogForm()
        {
            var dlg = new OpenFileDialog();

            // Get valid image filters from system codecs
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            string sep = string.Empty;

            var revCodecs = codecs.Reverse();

            foreach (var c in revCodecs)
            {
                string codecName = c.CodecName.Substring(8).Replace("Codec", "Files").Trim();
                dlg.Filter = String.Format("{0}{1}{2} ({3})|{3}", dlg.Filter, sep, codecName, c.FilenameExtension);
                sep = "|";
            }

            dlg.Filter = String.Format("{0}{1}{2} ({3})|{3}", "(JPG Files)|*.jpg", sep, "All Files", "*.*");

            if (dlg.ShowDialog() == true)
            {
                return dlg.FileName;
            }
            else
            {
                return null;
            }
        }

        public static BitmapImage getImageFile(string uri)
        {
            try
            {
                if (!File.Exists(uri))
                    return null;

                return new BitmapImage(new Uri(@uri, UriKind.Absolute));
            }
            catch (FileNotFoundException ex)
            {
                log.Error("Image File Not Found", ex);
                //MessageBox.Show("The following image file was not found: " + uri, "Image File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }

        public static string getPhotoImageFolder()
        {
            try
            {
                return Settings.Default.ImagesFolder;
            }
            catch (Exception ex)
            {
                log.Error("Error Retrieving Photo Image Folder", ex);
                MessageBox.Show("An unexpected error occurred when attempting to retrieve photo image folder. Please contact your system support. Error message returned: " + ex.Message, "Error Retrieving Photo Image Folder", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return null;
        }

        public static bool updatePhotoImageFolder(string folder)
        {
            try
            {
                SwipeCardBLL scBLL = new SwipeCardBLL();
                int rowsUpdated = scBLL.UpdateIDCardsConfigByName(folder, App.APP_CONFIG_KEY_IMAGE_FOLDER);

                return rowsUpdated == 1;
            }
            catch (Exception ex)
            {
                log.Error("Error Updating Photo Image Folder", ex);
                MessageBox.Show("An unexpected error occurred when attempting to update photo image folder. Please contact your system support. Error message returned: " + ex.Message, "Error Updating Photo Image Folder", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return false;
        }
    }
}
