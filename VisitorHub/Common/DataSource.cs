using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

namespace SwipeDesktop.Common
{
    public class ReactiveDataSource<T> :ReactiveObject
    {
        public ReactiveDataSource()
        {
            ItemsSource = new ReactiveList<T>();
        }
        public ReactiveList<T> ItemsSource { get; set; }

        private T _selectedItem;

        public T SelectedItem
        {
            get { return _selectedItem; }
            set { this.RaiseAndSetIfChanged(ref _selectedItem, value); }
        }

        private string _label;
        public string Label {
            get { return _label; }
            set { this.RaiseAndSetIfChanged(ref _label, value); }
        }
    }
}
