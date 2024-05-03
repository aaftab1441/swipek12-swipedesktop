using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SwipeDesktop.Controls
{
    public class NoInheritanceContentControl : ContentControl
    {
        public NoInheritanceContentControl()
        {
            InheritanceBehavior = InheritanceBehavior.SkipAllNow;
        }
    }
}
