using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using log4net;


namespace SwipeDesktop.Common
{
    using System;
    using System.Runtime;
    using System.Runtime.InteropServices;

    public class InternetAvailability
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(InternetAvailability));

        [DllImport("wininet.dll")]
        private static extern bool InternetGetConnectedState(out int description, int reservedValue);

        [DllImport("wininet.dll")]
        private static extern bool InternetCheckConnection(String url, int flag, int ReservedValue);

        public static bool IsInternetAvailable(out string message)
        {
            int description;
            var status = InternetGetConnectedState(out description, 0);

            message = status ? "Internet Connected" : "Network Not Connected";

            return status;
        }

        public static async Task<bool> ApiIsNotAvailable(string uri)
        {
            //var status = InternetCheckConnection(Settings.Default.JsonUrl + "/ping?station=" + Environment.MachineName, 1, 0);
            
            //message = status ? "SwipeK12 Is Available" : "SwipeK12 Is Not Available";
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    //var content = new StringContent(JsonConvert.SerializeObject(@event), Encoding.UTF8, "application/json");
                    var response = await client.GetStringAsync(uri);
                }
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return true;
        }
    }
}
