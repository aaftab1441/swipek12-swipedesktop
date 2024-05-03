using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwipeDesktop.Common
{
    public static class ProcessExtensions
    {
        public static bool IsRunning(this Process process)
        {
            if (process == null)
                throw new ArgumentNullException("process");

            try
            {
                Process.GetProcessById(process.Id);
            }
            catch (ArgumentException)
            {
                return false;
            }
            return true;
        }

        public static bool IsRunning(this Process[] processes)
        {
            if (processes.Length == 0)
                return false;

            var process = processes.FirstOrDefault();

            if (process == null)
                return false;

            try
            {
                Process.GetProcessById(process.Id);
            }
            catch (ArgumentException)
            {
                return false;
            }
            return true;
        }

        public static Process GetProcess(this Process[] processes)
        {
            if (processes.Length == 0)
                return null;

            return processes.FirstOrDefault();
        }
    }
}
