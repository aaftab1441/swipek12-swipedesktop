using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Common;
using log4net;
using ReactiveUI;
using ServiceStack.Common.Extensions;
using SwipeDesktop.Api;
using SwipeDesktop.Common;
using SwipeDesktop.Interfaces;
using SwipeDesktop.Modal;
using SwipeDesktop.Models;

namespace SwipeDesktop.ViewModels
{
    public class BatchPrintViewModel: PopupViewModel, IViewModel
    {
        private LocalStorage Storage;
        private RemoteStorage RemoteStorage;

        private static readonly ILog Logger = LogManager.GetLogger(typeof(BatchPrintViewModel));

        static readonly ReactiveList<string> _batchModes = new ReactiveList<string>(new[] { "Grade", "Homeroom" });

        static readonly ReactiveList<string> _sortModes = new ReactiveList<string>(new[] { "Last Name", "Homeroom" });

        static readonly ReactiveList<Tuple<string, int>> _idTemplates = new ReactiveList<Tuple<string, int>>(new[] { new Tuple<string, int>("Template1", -1), new Tuple<string, int>("Template2", -1) });

        ReactiveList<CheckedItem> _batchItems = new ReactiveList<CheckedItem>(new[] { new CheckedItem() {Item = "One", IsChecked = false}, new CheckedItem() { Item = "One", IsChecked = false } });

        public BatchPrintViewModel(LocalStorage localDbStorage, RemoteStorage remoteStorage)
        {
            Storage = localDbStorage;
            RemoteStorage = remoteStorage;

            this.WhenAnyValue(x => x.BatchModeList.SelectedItem).Where(x => x != null).Subscribe(x =>
            {
              
                var batchBy = x;

                if (x.ToLower() == "homeroom")
                {
                    var list = _sortModes;
                    list.Remove("Homeroom");
                    SortModeList.ItemsSource = list;
                    SortModeList.SelectedItem = list.FirstOrDefault();
                }
                else
                {
                    SortModeList.ItemsSource = _sortModes;
                    SortModeList.SelectedItem = _sortModes.FirstOrDefault();
                }

                //MessageBox.Show("View loaded.");
                Storage.GetBatchPrintItems(Settings.Default.SchoolId, batchBy).ObserveOnDispatcher().Subscribe(OnItemsReturned);


            });

            this.WhenAnyValue(x=>x.Storage).Subscribe(x =>
            {
             
                //MessageBox.Show("View loaded.");
                Storage.GetIdCardTemplates(Settings.Default.SchoolId).ObserveOnDispatcher().Subscribe(OnIdCardTemplatesReturned);


            });
        }
       
        private void OnItemsReturned(IEnumerable<string> items)
        {
           BatchItems.Clear();
           items.ForEach(x =>
           {
               BatchItems.Add(new CheckedItem() { Item = x, IsChecked = false});
           });
          
        }

        private void OnIdCardTemplatesReturned(IEnumerable<Tuple<string, int>> items)
        {
            var array = items.ToArray();
            IdTemplateList.ItemsSource.Clear();
            IdTemplateList.ItemsSource.AddRange(array);
            IdTemplateList.SelectedItem = array.FirstOrDefault();
        }

        private ReactiveDataSource<string> _batchModeList = new ReactiveDataSource<string>()
        {
            ItemsSource = _batchModes,
            SelectedItem = _batchModes.FirstOrDefault(),
            Label = "Print By:"
        };

        public ReactiveDataSource<string> BatchModeList
        {
            get { return _batchModeList; }
            set
            {
                this.RaiseAndSetIfChanged(ref _batchModeList, value);
            }
        }


        private ReactiveDataSource<string> _sortModesList = new ReactiveDataSource<string>()
        {
            ItemsSource = _sortModes,
            SelectedItem = _sortModes.FirstOrDefault(),
            Label = "Sort By:"
        };

        private ReactiveDataSource<Tuple<string, int>> _idTemplateList = new ReactiveDataSource<Tuple<string, int>>()
        {
            ItemsSource = _idTemplates,
            SelectedItem = _idTemplates.FirstOrDefault(),
            Label = "Template:"
        };

        public ReactiveDataSource<Tuple<string, int>> IdTemplateList
        {
            get { return _idTemplateList; }
            set { this.RaiseAndSetIfChanged(ref _idTemplateList, value); }
        }

        public ReactiveList<CheckedItem> BatchItems
        {
            get { return _batchItems; }
            set { this.RaiseAndSetIfChanged(ref _batchItems, value); }
        }
        public ReactiveDataSource<string> SortModeList
        {
            get { return _sortModesList; }
            set
            {
                this.RaiseAndSetIfChanged(ref _sortModesList, value);
            }
        }

        bool _onlyWithImages;
        public bool OnlyWIthImages
        {
            get { return _onlyWithImages; }
            set
            {
                this.RaiseAndSetIfChanged(ref _onlyWithImages, value);
            }
        }


    }
}
