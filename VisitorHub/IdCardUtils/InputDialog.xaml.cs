using System;
using System.Windows;

namespace SwipeK12
{
    /// <summary>
    /// Interaction logic for InputDialog.xaml
    /// </summary>
    public partial class IdCardInputDialog : Window
    {
        public IdCardInputDialog(string title, string question, string defaultAnswer = "")
        {
            InitializeComponent();
            lblQuestion.Content = question;
            txtAnswer.Text = defaultAnswer;
            Title = title;
        }

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            txtAnswer.SelectAll();
            txtAnswer.Focus();
        }

        public string Answer
        {
            get { return txtAnswer.Text; }
        }
    }
}
