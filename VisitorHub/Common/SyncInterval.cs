using System.Configuration;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;


namespace SwipeDesktop.Common
{
    public static class SyncIntervalNotifier
    {
        private static Timer timer;
        private static DateTime lastSync;
        private static readonly int Interval = int.Parse(ConfigurationManager.AppSettings["syncInterval"]);

        static SyncIntervalNotifier()
        {
            timer = new Timer(GetSleepTime());
            timer.Elapsed += (o, e) =>
            {
                OnSync(DateTime.Now);
                timer.Interval = GetSleepTime();
            };
            timer.Start();

            SystemEvents.TimeChanged += SystemEvents_TimeChanged;
        }

        private static void SystemEvents_TimeChanged(object sender, EventArgs e)
        {
            timer.Interval = GetSleepTime();
        }

        private static double GetSleepTime()
        {
            var hours = DateTime.Now.AddMinutes(Interval);
            var differenceInMilliseconds = (hours - DateTime.Now).TotalMilliseconds;
            return differenceInMilliseconds;
        }

        private static void OnSync(DateTime now)
        {
            var handler = SyncTriggered;
            if (handler != null)
            {
                handler(null, new SyncTriggeredEventArgs(now, now.AddMinutes(-Interval)));
            }
        }

        public static event EventHandler<SyncTriggeredEventArgs> SyncTriggered;
    }

    public class SyncTriggeredEventArgs : EventArgs
    {
        public SyncTriggeredEventArgs(DateTime now, DateTime lastSync)
        {
            this.Now = now;
            LastSync = lastSync;
        }

        public DateTime Now { get; private set; }
        public DateTime LastSync { get; private set; }
    }
}
