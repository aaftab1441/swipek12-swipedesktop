using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SwipeDesktop.Models;

namespace SwipeDesktop.Interfaces
{
    public interface IViewModel
    {
    }

    public interface IHostedViewModel
    {
        bool IsProcessing { get; set; }
    }


    public interface IScanViewModel : IHostedViewModel
    {
       
        ObservableCollection<VisitLocation> Locations { get; }
    }
}
