using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
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
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : UserControl, IViewFor<SettingsViewModel>
    {
        public Settings()
        {
            InitializeComponent();

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                DataContext = DesignTimeData;
            }

            //this.BindCommand(ViewModel, vm => vm.AddPersonCommand, v => v.AddPerson);
            //reactive view updates
            this.WhenAnyValue(x => x.SettingsTabs).Where(x=>x.Items.Count >0).Subscribe(x =>
            {
                if (ConfigurationManager.AppSettings["mode"] == "VisitorKiosk")
                {
                    ((TabItem)SettingsTabs.Items[2]).Visibility = Visibility.Hidden;
                    ((TabItem)SettingsTabs.Items[3]).Visibility = Visibility.Hidden;
                    ((TabItem)SettingsTabs.Items[4]).Visibility = Visibility.Hidden;
                    ((TabItem)SettingsTabs.Items[5]).Visibility = Visibility.Hidden;
                }
                else
                {
                    ((TabItem)SettingsTabs.Items[1]).Visibility = Visibility.Hidden;
                }
            });

            //bind viewModel
            //this.OneWayBind(ViewModel, vm => vm.Printers, v => v.Printers.ItemsSource);
            //this.Bind(ViewModel, vm => vm.PassPrintQueue, v => v.Printers.SelectedItem);

            //this.Bind(ViewModel, vm => vm.PrintPasses, v => v.PrintToggleSwitch.IsChecked);
            //this.OneWayBind(ViewModel, vm => vm.LocationList, v => v.Locations.DataContext);
        }


        public SettingsViewModel ViewModel
        {
            get { return (SettingsViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(SettingsViewModel), typeof(Settings), new PropertyMetadata(null));

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (SettingsViewModel)value; }
        }


        public static SettingsViewModel DesignTimeData
        {
            get
            {
                return new SettingsViewModel(null, null) { LocationList = new Common.ReactiveDataSource<string>() { SelectedItem = "Study Hall", ItemsSource = new ReactiveList<string>(new[] { "Bathroom", "Study Hall" }) }, SchoolName = "Swipe High School", Config = "Default", DatabaseStats = new ReactiveList<Tuple<string, int>>(new[]{new Tuple<string, int>("Row 1", 100)})};
            }
        }


        /*
        public SettingsViewModel ViewModel
        {
            get { return (SettingsViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(SettingsViewModel), typeof(Settings), new PropertyMetadata(null));

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (SettingsViewModel)value; }
        }*/

    }
}
