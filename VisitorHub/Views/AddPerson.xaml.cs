
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SwipeDesktop.Validations;

namespace SwipeDesktop.Views
{
    /// <summary>
    /// Interaction logic for AddPerson.xaml
    /// </summary>
    public partial class AddPerson : UserControl
    {
        public AddPerson()
        {
            InitializeComponent();

            Loaded += (sender, e) => MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }

        protected void TextBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            ((Control)sender).GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }

    }
}
