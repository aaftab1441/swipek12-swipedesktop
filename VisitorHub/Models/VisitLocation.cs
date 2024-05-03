using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

using SwipeDesktop.ViewModels;

namespace SwipeDesktop.Models
{
    public class VisitLocation : ReactiveObject
    {
        private readonly VisitLocationViewModel _viewModel;

        public VisitLocation(VisitLocationViewModel vm, string name)
        {
            _viewModel = vm;
            Name = name;

            SetLocation = this.WhenAny(x => x.Name, x => !string.IsNullOrEmpty(x.Value)).ToCommand();
            this.WhenAnyObservable(x => x.SetLocation).Subscribe(x =>
            {
                _viewModel.SelectedLocation = Name;
            });

        }

        public ReactiveCommand<object> SetLocation { get; private set; }

        private string _name;

        public string Name
        {
            get { return _name; }
            set { this.RaiseAndSetIfChanged(ref _name, value); }
        }
    }
}
