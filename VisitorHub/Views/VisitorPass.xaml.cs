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
using SwipeDesktop.Models;
using SwipeDesktop.ViewModels;

namespace SwipeDesktop.Views
{
    /// <summary>
    /// Interaction logic for VisitorPass.xaml
    /// </summary>
    public partial class VisitorPass : UserControl
    {
        public VisitorPass()
        {
            InitializeComponent();
           
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                var v = new VisitorScanViewModel();
                v.FirstName = "JOHN";
                v.LastName = "HANCOCK";
                v.VisitLocation = "Front Office";
                v.VisitEntryDate = DateTime.Now;
                //v.EntryNumber = "J920929";
                    
                var pass = new PrintModel<VisitorScanViewModel>(v);
                pass.SchoolName = "Swipe High School";

                this.DataContext = pass;
            }
        }
    }
}
