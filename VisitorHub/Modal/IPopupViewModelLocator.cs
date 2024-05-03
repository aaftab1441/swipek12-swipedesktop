using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SwipeDesktop.Interfaces;

namespace SwipeDesktop.Modal
{
    public interface IPopupViewModelLocator
    {
        IViewModel LocateDialog(string dialogName);
    }
}
