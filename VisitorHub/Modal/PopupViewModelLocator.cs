using System;
using System.Collections.Generic;
using System.Linq;

using SwipeDesktop.Interfaces;
using SwipeDesktop.Modal;

namespace SwipeDesktop.Modal
{
    public class PopupViewModelLocator : IPopupViewModelLocator
    {
        private IEnumerable<Lazy<IViewModel, IDialogMetadata>> _dialogModels;
        //IComponentContext ContainerContext { get; set; }
        public PopupViewModelLocator(IEnumerable<Lazy<IViewModel, IDialogMetadata>> models)
        {
            _dialogModels = models;
        }

        public IViewModel LocateDialog(string dialogId)
        {
            try
            {
                return _dialogModels.First(a => a.Metadata.DialogName == dialogId).Value;
            }
            catch (Exception ex)
            {
                
            }

            return null;
        }

    }

}
