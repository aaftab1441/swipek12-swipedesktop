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
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace SwipeDesktop.ViewModels
{
    public class SelectedForPrintViewModel: PopupViewModel, IViewModel
    {
        private LocalStorage Storage;
        private RemoteStorage RemoteStorage;

        int _cardId;

        private static readonly ILog Logger = LogManager.GetLogger(typeof(BatchPrintViewModel));

        ReactiveList<CheckedItem> _batchItems = new ReactiveList<CheckedItem>(new[] { new CheckedItem() {Item = "No Students Found.", IsChecked = false}});

        public SelectedForPrintViewModel(LocalStorage localDbStorage, RemoteStorage remoteStorage)
        {
            Storage = localDbStorage;
            RemoteStorage = remoteStorage;

            this.WhenAnyValue(x => x.Storage).Subscribe(x =>
            {
                var templates = x.GetIdCardTemplates(Settings.Default.SchoolId).ObserveOnDispatcher().Subscribe(_ =>
                {

                    var tmpl = _.SingleOrDefault(Item => Item.Item1 == Settings.Default.StudentIdTemplateName);

                    if (tmpl != null)
                        _cardId = tmpl.Item2;

                });

            });
        }
       
        public void OnStudentsReturned(IEnumerable<StudentModel> items)
        {
            if (items.Any())
            {
                BatchItems.Clear();
            }

            items.ForEach(x =>
           {
               BatchItems.Add(new CheckedItem() { Item = $"{x.LastName}, {x.FirstName} [{x.IdNumber}] GR: {x.Grade} HR: {x.Homeroom}", IsChecked = true, ItemId = x.PersonId, ItemNumber = x.IdNumber});
           });
          
        }

        public ReactiveList<CheckedItem> BatchItems
        {
            get { return _batchItems; }
            set { this.RaiseAndSetIfChanged(ref _batchItems, value); }
        }

        public int CardId
        {
            get { return _cardId; }
            set { this.RaiseAndSetIfChanged(ref _cardId, value); }
        }

    }
}
