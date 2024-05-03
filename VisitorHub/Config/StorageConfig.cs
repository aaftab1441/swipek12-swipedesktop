using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autofac;
using Common;
using ServiceStack;
using SwipeDesktop.Api;
using SwipeDesktop.Common;
using SwipeDesktop.Interop;
using SwipeDesktop.Models;
using SwipeDesktop.Storage;
using SwipeDesktop.ViewModels;

namespace SwipeDesktop.Config
{
    internal class StorageWireup : Module
    {
        private static readonly StationMode StationMode = (StationMode)Enum.Parse(typeof(StationMode), ConfigurationManager.AppSettings["mode"]);

        //readonly static string StoragePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\data.sq3";
        protected override void Load(ContainerBuilder builder)
        {
            //builder.RegisterType<MainWindow>()
            //    .OnActivated(e => e.Instance.DataContext = e.Context.Resolve<MainViewModel>());

           
            builder.Register(c => new RemoteStorage()).SingleInstance().AsSelf();
            builder.Register(c => new LocalStorage()).SingleInstance().AsSelf();
            builder.Register(c => new ScanStorage()).SingleInstance().AsSelf();
            builder.Register(c => new DismissalStorage()).SingleInstance().AsSelf();
            builder.Register(c => new InOutStorage()).SingleInstance().AsSelf();

            builder.Register(c => new StaffScanStorage()).SingleInstance().AsSelf();
            if (StationMode == StationMode.VisitorKiosk)
            {
               
                builder.Register(c => new VisitStorage()).SingleInstance().AsSelf();
                builder.Register(c => new VisitExitStorage()).SingleInstance().AsSelf();

            }

            if (StationMode == StationMode.Station)
            {
              
                builder.Register(c => new DetentionStorage()).SingleInstance().AsSelf();
               
                builder.Register(c => new FineStorage()).SingleInstance().AsSelf();
                builder.Register(c => new IdCardStorage()).SingleInstance().AsSelf();
                builder.Register(c => new AlertPrintedStorage()).SingleInstance().AsSelf();
            }
        }

    }
}
