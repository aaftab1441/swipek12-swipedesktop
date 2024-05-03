using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
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
    /// Interaction logic for StudentAlternateId.xaml
    /// </summary>
    public partial class StudentAlternateId : UserControl, IViewFor<StudentAlternateIdViewModel>
    {
      

        public StudentAlternateId()
        {
            InitializeComponent();
            this.WhenAnyValue(x => x.ViewModel).Where(vm=>vm != null).Subscribe(_ =>
            {
                AlternateIdTextbox.Clear();
                AlternateIdTextbox.Focus();
                //Keyboard.Focus(AlternateIdTextbox);
            });

            this.Bind(ViewModel, vm => vm.NewAltId, v => v.AlternateIdTextbox.Text);
            this.Bind(ViewModel, vm => vm.NewAltId, v => v.NewAltIdTextblock.Text);
            this.Bind(ViewModel, vm => vm.ShowNewId, v => v.NewAltIdTextblock.Visibility);
            //Loaded += StudentAlternateId_Loaded;
            //this.LayoutUpdated += StudentAlternateId_LayoutUpdated;
        }

        void StudentAlternateId_LayoutUpdated(object sender, EventArgs e)
        {
            reset();
        }

        void StudentAlternateId_Loaded(object sender, RoutedEventArgs e)
        {
            reset();
        }

        void reset()
        {
            AlternateIdTextbox.Clear();
            AlternateIdTextbox.Focus();         // Set Logical Focus
            Keyboard.Focus(AlternateIdTextbox); // Set Keyboard Focus
        }

       public StudentAlternateIdViewModel ViewModel
        {
            get { return (StudentAlternateIdViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(StudentAlternateIdViewModel), typeof(StudentAlternateId), new PropertyMetadata(null));

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (StudentAlternateIdViewModel)value; }
        }
    }
}
