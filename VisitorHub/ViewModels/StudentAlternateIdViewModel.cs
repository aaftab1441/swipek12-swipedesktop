using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ReactiveUI;
using SwipeDesktop.Interfaces;
using SwipeDesktop.Models;

namespace SwipeDesktop.ViewModels
{
    public class StudentAlternateIdViewModel : ReactiveObject, IViewModel
    {
        public StudentAlternateIdViewModel()
        {
            /*
            this.WhenAnyValue(x => x.NewAltId).Where(_=>!string.IsNullOrEmpty(_))
              .Select(x => Visibility.Visible)
              .ToProperty(this, x => x.ShowNewId, out _ShowNewId);

            this.WhenAnyValue(x => x.NewAltId).Where(string.IsNullOrEmpty)
             .Select(x => Visibility.Hidden)
             .ToProperty(this, x => x.ShowNewId, out _ShowNewId);*/

        }
        Visibility _showNewId;
        public Visibility ShowNewId
        {
            get { return _showNewId; }
            set { this.RaiseAndSetIfChanged(ref _showNewId, value); }
        }

        string _newAltId;
        public string NewAltId
        {
            get { return _newAltId; }
            set { this.RaiseAndSetIfChanged(ref _newAltId, value); }
        }

        StudentModel _selectedStudent;
        public StudentModel Student
        {
            get { return _selectedStudent; }
            set { this.RaiseAndSetIfChanged(ref _selectedStudent, value); }
        }

        PopupViewModel _popup;
        public PopupViewModel PopupWindow
        {
            get { return _popup; }
            set
            {
                this.RaiseAndSetIfChanged(ref _popup, value);
            }
        }

    }
}
