
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Topshelf;

namespace SwipeDesktop.Sync
{
    class Program
    {
        public static readonly ILog Logger = LogManager.GetLogger(typeof(Program));

        //private static IContainer Container;

        static void Main(string[] args)
        {

            Logger.Info("Starting swipe cloud bus listener...");

            /*
            Container = CommandBus.Start();

            var bus = Container.Resolve<IServiceBus>();
            var locations = Container.Resolve<ScanLocations>();
            var connection = locations.OpenConnection();

            Logger.InfoFormat("Database: {0}", connection.Database);

            connection.Close();
            */

            var host = HostFactory.New(x => x.Service<Startup2>(sc =>
            {
                x.SetServiceName("SwipeDesktop.Sync");
                x.SetDescription("SwipeK12 Desktop Sync Service");
                x.StartAutomatically();
                x.EnableShutdown();
                //x.RunAsNetworkService();

                sc.ConstructUsing(() => new Startup2());

                // the start and stop methods for the service
                sc.WhenStarted(s => s.Start());
                sc.WhenStopped(s => s.Stop());

            }));

            host.Run();

        }
    }
}
