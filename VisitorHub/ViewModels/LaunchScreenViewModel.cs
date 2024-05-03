using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ReactiveUI;
using SwipeDesktop.Views;

namespace SwipeDesktop.ViewModels
{
    public class LaunchScreenViewModel {
        public IScreen HostScreen { get; protected set; }

        public LaunchScreenViewModel(IScreen screen)
        {
            HostScreen = screen;
        }
    }
}
