using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using log4net;

namespace SwipeK12
{
    /// <summary>
    /// Interaction logic for OpenCard.xaml
    /// </summary>
    public partial class OpenCard : Window
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Dictionary<int, string> idCardsList = new Dictionary<int, string>();

        private int cardId = -1;

        public OpenCard()
        {
            InitializeComponent();

            SwipeCardBLL scBLL = new SwipeCardBLL();

            try
            {
                SwipeCard.IDCardsDataTable idCardsTable = scBLL.GetActiveIDCards();

                foreach (DataRow row in idCardsTable.Rows)
                {
                    idCardsList.Add(Convert.ToInt32(row[App.APP_DB_FIELD_CARD_ID]), row[App.APP_DB_FIELD_CARD_NAME].ToString());
                }

                cboCardName.ItemsSource = idCardsList;
                cboCardName.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                log.Error("Error loading ID card list", ex);
                MessageBox.Show("An unexpected error occurred when attempting to load ID card list. Please contact your system support. Error message returned: " + ex.Message, "Error Loading ID Cards", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            if (cboCardName.SelectedValue != null)
            {
                cardId = ((KeyValuePair<int, string>)cboCardName.SelectedValue).Key;
                this.DialogResult = cardId > 0;
            }
        }

        public int getCardID()
        {
            return cardId;
        }
    }
}
