using System;
using System.IO;
using System.Windows;
using log4net;

namespace SwipeK12
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // Application level variable key values
        public const string APP_KEY_CARD_ID = "CURRENT_CARD_ID";
        public const string APP_KEY_SCHOOL_ID = "DEFAULT_SCHOOL_ID";
        public const string APP_KEY_STUDENT_ID = "DEFAULT_STUDENT_ID";
        public const string APP_KEY_TEACHER_ID = "DEFAULT_TEACHER_ID";

        // Application level constants
        public const string APP_CARD_TYPE_STUDENT = "Student";
        public const string APP_CARD_TYPE_TEACHER = "Teacher";
        public const string APP_CARD_TYPE_OTHER = "Other";
        public const string APP_CARD_ORIENTATION_PORTRAIT = "Portrait";
        public const string APP_CARD_ORIENTATION_LANDSCAPE = "Landscape";
        public const string APP_CARD_FRONT = "Front";
        public const string APP_CARD_BACK = "Back";

        public const string APP_DB_FIELD_CARD_ID = "CardID";
        public const string APP_DB_FIELD_CARD_NAME = "CardName";
        public const string APP_DB_FIELD_SCHOOL_ID = "SchoolID";
        public const string APP_DB_FIELD_SCHOOL_NAME = "SchoolName";
        public const string APP_DB_FIELD_PERSON_ID = "PersonID";
        public const string APP_DB_FIELD_DISPLAY_NAME = "DisplayName";

        public const int APP_DEFAULT_TEXTBOX_WIDTH = 100;
        public const double APP_CARD_LONG_SIDE = 324.77480314960627;
        public const double APP_CARD_SHORT_SIDE = 204.13228346456691;

        public const string APP_FONT_BARCODE = "/Fonts/#Free 3 of 9 Extended";

        public const string APP_CONFIG_KEY_IMAGE_FOLDER = "PHOTO_IMAGE_FOLDER";


        // ===========================================================================================================================
        // ===========================================================================================================================
        // UI Event Handlers
        // ===========================================================================================================================
        // ===========================================================================================================================

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            log.Info("Application Stopped");
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            log.Error("An unhandled exception occurred: ", e.Exception);
            MessageBox.Show("An unhandled exception just occurred: " + e.Exception.Message, "Exception Sample", MessageBoxButton.OK, MessageBoxImage.Warning);
            e.Handled = true;
        }


        // ===========================================================================================================================
        // ===========================================================================================================================
        // Application Helper Methods
        // ===========================================================================================================================
        // ===========================================================================================================================

        public static string getRoot()
        {
            return Directory.GetCurrentDirectory();
        }


        public static int getCurrentCardID()
        {
            return Convert.ToInt32(Application.Current.Properties[App.APP_KEY_CARD_ID]);
        }

        public static void setCurrentCardID(int cardID)
        {
            Application.Current.Properties[App.APP_KEY_CARD_ID] = cardID;
        }


        public static int getCurrentSchoolID()
        {
            return Convert.ToInt32(Application.Current.Properties[App.APP_KEY_SCHOOL_ID]);
        }

        public static void setCurrentSchoolID(int schoolID)
        {
            Application.Current.Properties[App.APP_KEY_SCHOOL_ID] = schoolID;
        }


        public static int getCurrentStudentID()
        {
            return Convert.ToInt32(Application.Current.Properties[App.APP_KEY_STUDENT_ID]);
        }

        public static void setCurrentStudentID(int studentID)
        {
            Application.Current.Properties[App.APP_KEY_STUDENT_ID] = studentID;
        }


        public static int getCurrentTeacherID()
        {
            return Convert.ToInt32(Application.Current.Properties[App.APP_KEY_TEACHER_ID]);
        }

        public static void setCurrentTeacherID(int teacherID)
        {
            Application.Current.Properties[App.APP_KEY_TEACHER_ID] = teacherID;
        }
    }
}
