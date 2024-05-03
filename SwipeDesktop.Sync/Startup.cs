using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwipeDesktop.Sync
{
    internal class Startup2
    {
        public void Start()
        {

            Observable.Interval(TimeSpan.FromSeconds(480), Scheduler.Default).Subscribe(x => Task.Run(() =>
            {
                
            }));

        }

        public void Stop()
        {
            
        }
    }
}
