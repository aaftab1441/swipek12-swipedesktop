using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using SwipeDesktop.Models;
using SwipeDesktop.ViewModels;

namespace SwipeDesktop.Common
{
    public interface IReturnData
    {
        IEnumerable<Fine> Fines(int school, string filter = null);

        IObservable<PersonModel[]> SearchStudents(string filter);

        IObservable<IEnumerable<ScanLocation>> GetLocations(LocationType types);

        IObservable<bool> SendScan(ScanModel scan);
    }
}
