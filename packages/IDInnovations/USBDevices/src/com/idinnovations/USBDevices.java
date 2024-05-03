package com.idinnovations;


public class USBDevices {
     // Declare the members of the class
     private boolean bOpen=false; // indicates if the device is currently open

     // Declare all the native functions that are located in the USBQueue DLL
     private native int usbQueueDataAvailable();
     private native String usbQueueGetData() throws OutOfMemoryError;
     private native boolean usbQueueWaitForData(int iMSWaitTime);
     private native void usbQueueClearData();
     private native boolean installUSBQueue();
     private native boolean removeUSBQueue();

     // Declare the static section that actually loads the USBQueue DLL
     // in either its 32 Bit or 64Bit variant.
     static {
          // Get the bitness of the JVM
          String jvmArch = System.getProperty("os.arch");
          String jvmDataModel = System.getProperty("sun.arch.data.model");

          // output the bitness
          if (Debug.ON)  {
               System.out.println("USBDevices(): JVM Arch: " + jvmArch);
               System.out.println("USBDevices(): JVM Data Model: " + jvmDataModel);
          }

          // Load the library based on the BitLevel of the JVM are we running
          // under 64bit or 32bit
          if (jvmArch.contains("64") || jvmDataModel.contains("64")) {
               if (Debug.ON)
                    System.out.println("USBDevices(): Loading 64bit library");

               try {
                    // load the 64 bit library
                    System.loadLibrary("USBQueueX64"); // 64bit
               } catch (UnsatisfiedLinkError e) {
                    // If for some reason we were unable to load the DLL
                    // try to load the 32 bit version just in case
                    System.loadLibrary("USBQueue"); // 32bit
               }
          }
          else {
               if (Debug.ON)
                    System.out.println("USBDevices(): Loading 32bit library");

               try {
                    // the proper loadLibrary() statement
                    System.loadLibrary("USBQueue"); // 32bit
               } catch (UnsatisfiedLinkError e) {
                    // If for some reason we were unable to load the 32bit dll
                    // try to load the 64 bit version just in case
                    System.loadLibrary("USBQueueX64");
               }
          }
     }
     
     /*
      * This method opens the queue and returns true if success or false in the case
      * of failure. The queue only needs to be opened one time for all USBDevices 
      * present. This function will fail if any other usbDevice has been already opened
      * as the DLL will only allow a single queue to be open at a time.
      * 
      * @return true if success, fales if not
      */
     public boolean open()
     {
          // PURPOSE: This function will call into the USBQueue dll and will
          // install the USBQueue, if the DLL sucessfully opens the queue the return
     	// value is true otherwise false
          boolean bReturnValue;

          // Make sure that we are not already open because if we are already
          // open then cannot open again
          if (bOpen == true)
               return (false); // cannot open when already open
          
          // call down into the DLL to perform the operation
          bReturnValue = installUSBQueue();
          
          // Update the status of the open
          bOpen = bReturnValue;

          // return the result
          return (bReturnValue);
     }

     /*
      * This method closes the queue and returns true for success or false for 
      * failure.
      */
     public boolean close()
     {
          // PURPOSE: This routine will remove the USBQueue from the program
          // by calling into the DLL
          boolean bReturnValue;

          // make sure that we are currently open
          if (bOpen == false)
               return (false); // we cannot close because we are not open

          // call down into the DLL
          bReturnValue = removeUSBQueue();

          // return the result
          return (bReturnValue);
     }

     /*
      * This method checks for any data available. No matter what the device that put the 
      * data in the queue, if data is available it will return the number of bytes ready.
      * 
      * @return int indicating the number of bytes available, 0 if none.
      */
     public int dataAvailable()
     {
          // PURPOSE: This routine will call down into the DLL and return the number
          // of bytes that are available in the QUEUE at the time of the call

          // make sure that we have the queue open before we call down into the DLL
          if (bOpen == false)
               return (0); // do not check for any data

          // return the number of bytes in the queue
          return (usbQueueDataAvailable());
     }

     /*
      * This method will return the String for the current data in the queue. This will
      * pull the String(s) out and in the order received. This function will first check
      * for any Data Available before pulling any Strings.
      * 
      * @return String containing the current data, "" empty string if no data is 
      * available.
      */
     public String getDeviceData()
     {
          // PURPOSE: This routine will attempt to get the data from the device
          // if there is not data available then it will return an empty string
          String returnString;

          // If we don't have data available or we don't have
          // the device open
          if ((bOpen == false) || (dataAvailable() == 0))
               return (""); // empty string

          // Get the string from the DLL
          try {
               // call down
               returnString = usbQueueGetData();
          }
          catch (OutOfMemoryError e) {
               // if we have debug on then print out
               if (Debug.ON)
                    System.out.println("USBDevices: getDeviceData() - OutOfMemoryError Exception.");
               // return and empty string
               return ("");
          }

          // All is good reutrn the value
          return (returnString);
     }

     /*
      * This method will wait the specified time (in Milliseconds) or until data is
      * available in the queue from any device.
      * 
      * @param int iMS - number of Milliseconds to wait for data
      * 
      * @return true if data is available, false if data is available and timeout has been 
      * reached.
      */
     public boolean waitForData(int iMS)
     {
          // PURPOSE: To wait iMS milliseconds or until we have data available in the
          // USBQueue. During this wait the thread will sleep for the specified MS

          // Make sure that we have the device open, if we don't then we don't
          // need to wait
          if (bOpen == false)
               return (false);

          // call down into the DLL and return the result
          return (usbQueueWaitForData(iMS));
     }
}
