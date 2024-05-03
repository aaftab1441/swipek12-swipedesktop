using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using SwipeDesktop.ViewModels;

namespace SwipeDesktop.Config
{
    internal class ViewsWireup : Module
    {

        protected override void Load(ContainerBuilder builder)
        {
            //builder.RegisterType<MainWindow>()
            //    .OnActivated(e => e.Instance.DataContext = e.Context.Resolve<MainViewModel>());
        }

    }
}
