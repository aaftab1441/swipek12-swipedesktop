using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceLayer.Client;
using ServiceLayer.Common;

namespace SwipeDesktop.Common
{
    public class CommandBusDispatcher : RequestDispatcher
    {
        public CommandBusDispatcher(string address) : base(new ServiceFacadeProxy("ICommandService", address))
        {
        }

        protected override void DealWithUnknownException(ExceptionInfo exception)
        {

        }

        protected override void DealWithSecurityException(ExceptionInfo exceptionDetail)
        {

        }

    }
}
