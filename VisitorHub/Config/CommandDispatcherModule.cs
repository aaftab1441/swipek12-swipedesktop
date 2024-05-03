
using System.Reflection;
using Autofac;
using Commands;
using ServiceLayer.Client;
using ServiceLayer.Common;
using ServiceLayer.DummyImpl;
using ServiceStack.Logging;
using SwipeDesktop.Common;
using SwipeDesktop.Modal;
using Module = Autofac.Module;

namespace SwipeDesktop.Config
{
    public class CommandDispatcherModule : Module
    {
        //static readonly ILog Logger = LogManager.GetLogger(typeof(CommandDispatcherModule));
        private static readonly string serviceUrl = System.Configuration.ConfigurationManager.AppSettings["serviceUrl"];

        protected override void Load(ContainerBuilder builder)
        {

            var assembly = Assembly.GetAssembly(typeof(EchoCommand));

            KnownTypesProvider.RegisterDerivedTypesOf<Request>(assembly);
            KnownTypesProvider.RegisterDerivedTypesOf<Response>(assembly);

            var assembly2 = Assembly.GetAssembly(typeof(RecordVisitorScan));

            KnownTypesProvider.RegisterDerivedTypesOf<Request>(assembly2);
            KnownTypesProvider.RegisterDerivedTypesOf<Response>(assembly2);

            builder.RegisterInstance(new CommandBusDispatcher(serviceUrl)).As<IRequestDispatcher>().SingleInstance();

        }
    }
}
