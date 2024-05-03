package com.MyTestCompany;

import com.idinnovations.USBDevices;

public class MyUSBTest {

	/**
	 * @param args
	 */
	public static void main(String[] args) {
		// Declare the needed USB Device
		USBDevices usbQueue;
		int iSleepTime=0;
		boolean bDataReady=false;
		String sDeviceData;
		
		// Create an instance of the queue
		usbQueue = new USBDevices();
		
		// First thing we need to do with the queue is 
		// to open it up.
		if (usbQueue.open() == false) 
			System.out.println("Error[Opening Queue] - Unable to open the queue");
		else {
			System.out.println("Ready to read data from the USB Devices. Will read up to 6 entries or timeout.");
			
			// Since we were able to open the queue we are going to read a few of the 
			// entries from the queue as they come in or until we timeout.
			while (iSleepTime < 6) {
				// Loop reading data from the readers until we hit 6 reads or
				// 3 minutes
				
				// Wait until we have data or until 30 seconds has passed
				bDataReady = usbQueue.waitForData(30000);
				if (bDataReady == true) {
					// If we have data then we need to read it from the queue
					// and output it to the console.
					sDeviceData = usbQueue.getDeviceData();
					if (sDeviceData != "") {
						// output the data to the screen
						System.out.println("Data Read [ " + sDeviceData + " ]");
					}
				}
				
				// Increment wether we had a timeout or we had a good read
				iSleepTime++;
			}
			
			// Since we are done reading from the device we need to close
			// the usbQueue
			usbQueue.close();
			System.out.println("Queue was closed");
		}
	}

}
