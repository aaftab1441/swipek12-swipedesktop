using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using SwipeDesktop.Models;
using SwipeDesktop.ViewModels;

namespace SwipeDesktop.Views
{
    /// <summary>
    /// Interaction logic for TardyPass.xaml
    /// </summary>
    public partial class TardyPass : UserControl
    {
        public TardyPass(bool showTardyStats = true)
        {
            InitializeComponent();
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                DataContext = new PrintModel<Scan>(new Scan(){TardyStats = new ReactiveList<TardyStat>(new[]{new TardyStat(){ Description = "Period 05", MonthToDate = 0, YearToDate = 0} })});
            }

            if (showTardyStats == false)
            {
                TardyStatsHeader.Visibility = Visibility.Collapsed;
                TardyStats.Visibility = Visibility.Collapsed;
            }
        }
    }
}
