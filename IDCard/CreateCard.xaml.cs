using System;
using System.Windows;
using log4net;

namespace SwipeK12
{
    /// <summary>
    /// Interaction logic for CreateCard.xaml
    /// </summary>
    public partial class CreateCard : Window
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private int newCardId = -1;

        public CreateCard()
        {
            InitializeComponent();
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            SwipeCardBLL scBLL = new SwipeCardBLL();

            try
            {
                newCardId = scBLL.AddIDCard(App.getCurrentSchoolID(), txtCardName.Text, 0, 0, isStudentCard(), isTeacherCard(), isOtherCard(), Convert.ToBoolean(chkTempCard.IsChecked), null, 0, null, 0, isDualSided(), isFrontPortrait(), isBackPortrait(), null, true);
            }
            catch (Exception ex)
            {
                log.Error("Error adding new ID card", ex);
                MessageBox.Show("An unexpected error occurred when attempting to add new ID card. Please contact your system support. Error message returned: " + ex.Message, "Error Creating Card", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            this.DialogResult = newCardId > 0;
        }

        private bool isStudentCard()
        {
            return cboCardType.Text.Equals(App.APP_CARD_TYPE_STUDENT);
        }

        private bool isTeacherCard()
        {
            return cboCardType.Text.Equals(App.APP_CARD_TYPE_TEACHER);
        }

        private bool isOtherCard()
        {
            return false;
        }

        private bool isFrontPortrait()
        {
            return cboFrontOrientation.Text.Equals(App.APP_CARD_ORIENTATION_PORTRAIT);
        }

        private bool isBackPortrait()
        {
            return cboBackOrientation.Text.Equals(App.APP_CARD_ORIENTATION_PORTRAIT);
        }

        private bool isDualSided()
        {
            return false;
        }

        public int getNewCardID()
        {
            return newCardId;
        }
    }
}
