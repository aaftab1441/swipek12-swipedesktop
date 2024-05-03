using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ReactiveUI;
using SwipeDesktop.ViewModels;

namespace SwipeDesktop.Views
{
    /// <summary>
    /// Interaction logic for LaunchWindow.xaml
    /// </summary>
    public partial class LaunchWindow : IViewFor<LaunchScreenViewModel>
    {
        public LaunchScreenViewModel ViewModel
        {
            get { return (LaunchScreenViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(LaunchScreenViewModel), typeof(LaunchWindow), new PropertyMetadata(null));

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (LaunchScreenViewModel)value; }
        }

        public LaunchWindow()
        {
            InitializeComponent();
        }
    }
}
