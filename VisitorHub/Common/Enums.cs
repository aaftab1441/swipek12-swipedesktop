using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwipeDesktop.Common
{
    public enum Lane
    {
        Left,

        Right
    }

    public enum SwipeMode
    {
        Entry,

        ClassroomTardy,

        Location,

        CafeEntrance,

        ReducedLunch,

        Group
    }

    public enum StationMode
    {
        Station,

        Visitor,

        VisitorKiosk
    }


    public class MessageEvents
    {
        public static readonly string WelcomeImageChanged = "WelcomeImageChanged";
    }
}
