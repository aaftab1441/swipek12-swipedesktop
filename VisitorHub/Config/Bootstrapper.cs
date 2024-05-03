using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Integration.Mef;
using Common;
using ReactiveUI;
using Splat;
using SwipeDesktop.Api;
using SwipeDesktop.Common;
using SwipeDesktop.Modal;
using SwipeDesktop.Storage;
using SwipeDesktop.ViewModels;
using SwipeDesktop.Views;

namespace SwipeDesktop.Config
{
    public class Bootstrapper: ReactiveObject, IScreen
    {
    
        public RoutingState Router { get; private set; }

        public static IContainer Container{ get; set;}

        public Bootstrapper(IMutableDependencyResolver dependencyResolver = null, RoutingState testRouter = null)
        {

            Router = testRouter ?? new RoutingState();
            dependencyResolver = dependencyResolver ?? Locator.CurrentMutable;

            // Bind 
            RegisterParts(dependencyResolver);

            // TODO: This is a good place to set up any other app 
            // startup tasks, like setting the logging level
            LogHost.Default.Level = LogLevel.Debug;
            // Navigate to the opening page of the application
            
        }

        private void RegisterParts(IMutableDependencyResolver dependencyResolver)
        {
            dependencyResolver.RegisterConstant(this, typeof(IScreen));

            //launch window
            dependencyResolver.Register(() => new LaunchWindow(), typeof(IViewFor<LaunchScreenViewModel>));
            //vistor shell
            dependencyResolver.Register(() => new Shell(), typeof(IViewFor<MainViewModel>));
            dependencyResolver.Register(() => new ScanStation(App.Container.Resolve<ScanStorage>(), App.Container.Resolve<StaffScanStorage>(), App.Container.Resolve<RemoteStorage>(), App.Container.Resolve<LocalStorage>(), App.Container.Resolve<DetentionStorage>(), App.Container.Resolve<InOutStorage>(), App.Container.Resolve<DismissalStorage>(), App.Container.Resolve<FineStorage>(), App.Container.Resolve<IdCardStorage>(), App.Container.Resolve<AlertPrintedStorage>()), typeof(IViewFor<ScanStationViewModel>));
        }

        
        public IContainer GetContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<ViewModelsWireup>();
            builder.RegisterModule<ViewsWireup>();
            builder.RegisterModule<StorageWireup>();
            builder.RegisterModule<CommandDispatcherModule>();

            builder.RegisterType<PopupViewModelLocator>().As<IPopupViewModelLocator>();
            builder.RegisterMetadataRegistrationSources();
            return builder.Build();
        }
    }

}


        /*internal static void Run()
        {
            GetContainer().Resolve<MainWindow>().Show();
        }*/