using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using log4net;
using SwipeDesktop.Common;

namespace SwipeDesktop.Interop
{
    public class USBHelper
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(USBHelper));
        internal static List<USBDeviceInfo> GetUSBDevices(string deviceName)
        {
            List<USBDeviceInfo> devices = new List<USBDeviceInfo>();

            ManagementObjectCollection collection;
            using (var searcher = new ManagementObjectSearcher("root\\CIMV2", @"Select * From Win32_PnPEntity"))
                collection = searcher.Get();

            try
            {
                foreach (var device in collection)
                {
                    Logger.InfoFormat("USBHelper Found: {0}, {1}, {2}", device["Caption"], device["description"],
                        device["PNPDeviceID"]);

                    if (device["Caption"] == null)
                        continue;

                    if (device["PNPDeviceID"].ToString().Contains("USBCDCACM") ||
                        device["Caption"].ToString().Contains(deviceName))
                    {

                        var caption = device["Caption"].ToString();

                        devices.Add(new USBDeviceInfo(
                            (string) device.GetPropertyValue("DeviceID"),
                            (string) device.GetPropertyValue("PNPDeviceID"),
                            (string) device.GetPropertyValue("Description"),
                            caption.IndexOf('(') > 0 ? caption.Substring(caption.IndexOf('(') + 1, 4) : "N/A"
                        ));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("There was a problem reading a list of USB devices.");
            }

            collection.Dispose();
            return devices.Where(x=>x.DevicePort != "N/A").ToList();
        }
    }
}
