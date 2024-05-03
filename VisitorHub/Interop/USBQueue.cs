using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SwipeDesktop.Interop
{
    internal class USBQueue
    {
        private bool _open = false;

        public bool IsOpen
        {
            get { return _open; }
        }

        [DllImport("USBQueue.dll")]
        private static extern int usbQueueDataAvailable();
        
        [DllImport("USBQueue.dll")]
        private static extern void usbQueueGetData(StringBuilder data, int size);
     
        [DllImport("USBQueue.dll")]
        private static extern bool usbQueueWaitForData(int iMSWaitTime);
     
        [DllImport("USBQueue.dll")]
        private static extern void usbQueueClearData();
     
        [DllImport("USBQueue.dll")]
        private static extern bool installUSBQueue();
     
        [DllImport("USBQueue.dll")]
        private static extern bool removeUSBQueue();

        public bool Open()
        {
            // PURPOSE: This function will call into the USBQueue dll and will
            // install the USBQueue, if the DLL sucessfully opens the queue the return
            // value is true otherwise false
            bool bReturnValue;

            // Make sure that we are not already open because if we are already
            // open then cannot open again
            if (_open)
                return (false); // cannot open when already open

            // call down into the DLL to perform the operation
            bReturnValue = installUSBQueue();

            // Update the status of the open
            _open = bReturnValue;

            // return the result
            return (bReturnValue);
        }

        public bool RemoveUSBQueue()
        {
            return removeUSBQueue();
        }
        public bool InstallUSBQueue()
        {
            return installUSBQueue();
        }
        public void ClearQueue()
        {
            usbQueueClearData();
        }

        public bool Close()
        {
            // PURPOSE: This routine will remove the USBQueue from the program
            // by calling into the DLL
            bool bReturnValue;

            // make sure that we are currently open
            if (_open == false)
                return (false); // we cannot close because we are not open

            // call down into the DLL
            bReturnValue = removeUSBQueue();

            // return the result
            return (bReturnValue);
        }

        public int dataAvailable()
        {
            // PURPOSE: This routine will call down into the DLL and return the number
            // of bytes that are available in the QUEUE at the time of the call

            // make sure that we have the queue open before we call down into the DLL
            if (_open == false)
                return (0); // do not check for any data

            // return the number of bytes in the queue
            return (usbQueueDataAvailable());
        }

        public string getDeviceData(int size)
        {
          // PURPOSE: This routine will attempt to get the data from the device
              // if there is not data available then it will return an empty string
              string returnString = string.Empty;

              var sb = new StringBuilder(size);

              // If we don't have data available or we don't have
              // the device open
              if ((_open == false) || (dataAvailable() == 0))
                   return (""); // empty string

              // Get the string from the DLL
              try {
                 
                  usbQueueGetData(sb, size);
              }
              catch (Exception e) {
                   // return an empty string
                   return ("");
              }

              // All is good return the value
              return (sb.ToString());
        }
        
         public bool waitForData(int iMS)
         {
              // PURPOSE: To wait iMS milliseconds or until we have data available in the
              // USBQueue. During this wait the thread will sleep for the specified MS

              // Make sure that we have the device open, if we don't then we don't
              // need to wait
              if (_open == false)
                   return (false);

              // call down into the DLL and return the result
              return (usbQueueWaitForData(iMS));
         }
    }
}
