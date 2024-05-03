using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Common.Events;
using log4net;
using SwipeDesktop.Common;
using SwipeDesktop.Models;
using SwipeDesktop.ViewModels;
//using MassTransit;

namespace SwipeDesktop
{
    class SocketServer
    {
        private TcpListener tcpListener;
        private Thread listenThread;
        private static string ListenAddress = ConfigurationManager.AppSettings["ListenAddress"];
        private static readonly int ListenPort = int.Parse(ConfigurationManager.AppSettings["ListenPort"]);
        private static readonly string Mode = ConfigurationManager.AppSettings["mode"];

        //private static IServiceBus _bus;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SocketServer));

        //private bool isTimeStampRequest = false;
        private Thread ClientThread;
        const string TIMESTAMP = "TIMESTAMP";

        private bool run = true;

        ScanStationViewModel MainViewModel;
        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
        public SocketServer()
        {
            if(ListenAddress == null)
            {
                ListenAddress = GetLocalIPAddress();
            }

            this.tcpListener = new TcpListener(IPAddress.Parse(ListenAddress), ListenPort);
        }

        public SocketServer(ScanStationViewModel viewModel) : this()
        {
            MainViewModel = viewModel;

        }

        /*
        public SocketServer(IServiceBus bus): this()
        {
             _bus = bus;
            
        }
        */

        public bool StartListening()
        {

            this.listenThread = new Thread(new ThreadStart(ListenForClients));
            this.listenThread.Start();

            Logger.InfoFormat("Scan Gateway Started. {0}:{1}", ListenAddress, ListenPort);

            return true;
        }

        public void Shutdown()
        {
            run = false;

            if (tcpListener != null)
                tcpListener.Stop();

        }

        private async void HandleClientComm(object client)
        {
            var tcpClient = (TcpClient) client;
            var clientStream = tcpClient.GetStream();

            var message = new byte[4096];

            while (true)
            {
                int bytesRead = 0;

                try
                {
                    //blocks until a client sends a message
                    bytesRead = clientStream.Read(message, 0, 4096);
                }
                catch
                {
                    //a socket error has occured
                    break;
                }

                if (bytesRead == 0)
                {
                    //the client has disconnected from the server
                    break;
                }

                //message has successfully been received
                var encoder = new ASCIIEncoding();
                var content = Encoding.ASCII.GetString(message, 0, bytesRead);
                Logger.DebugFormat("Data received {0}", content);

                if (content.IndexOf("\r") > -1)
                {
                    var barcode = content.Substring(0, content.IndexOf("\r"));
                    var student = await MainViewModel.LocalStorage.SearchByBarcodeAsync(barcode);

                    if (student != null)
                    {
                        await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.DataBind, new Action(() => MainViewModel.NetworkScan(student)));
                    }
                    else
                    {
                        PlaySound(badScan);
                        await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.DataBind, new Action(() =>
                             MainViewModel.Swipe(new Scan() { SwipeLane = Lane.Right, InvalidScan = true, Barcode = barcode, StudentName = string.Format("{0} INVALID SCAN", barcode), ScanLocation = null, EntryTime = DateTime.Now })
                        ));

                    }

                    Logger.InfoFormat("Done reading {0} bytes.", content.Length);
                }
            }

            //tcpClient.Close();
        }

        private string badScan = SwipeDesktop.Settings.Default.SoundsFolder + "\\badscan.wav";

        private void PlaySound(string uri)
        {

            try
            {
                using (var player = new SoundPlayer(uri))
                {
                    player.Play();
                }
            }
            catch (Exception ex) { Logger.Error(ex); }
        }
        private void writeData(NetworkStream networkStream, string dataToClient)
        {
            Logger.DebugFormat("Replying {0}", dataToClient);
            Byte[] sendBytes = null;
            try
            {
                sendBytes = Encoding.ASCII.GetBytes(dataToClient);
                networkStream.Write(sendBytes, 0, sendBytes.Length);
                networkStream.Flush();
                //networkStream.Close();
            }
            catch (SocketException e)
            {
                throw;
            }
        }

        void ReplyWithTS(TcpClient tcpClient)
        {
            Thread.Sleep(500);
            var date = DateTime.Now;

            bool isDaylight = TimeZoneInfo.Local.IsDaylightSavingTime(DateTime.Now);

            Logger.DebugFormat("Is Daylight Savings:{0}", isDaylight);

            if (!isDaylight)
                date = DateTime.Now.AddHours(1);

            writeData(tcpClient.GetStream(), date.ToString("yyMMddHHmmss") + "\r");
        }

        private void ListenForClients()
        {
            Logger.Info("Listening for connections...");

            try
            {

                this.tcpListener.Start();

                while (run)
                {
                    //blocks until a client has connected to the server
                    var client = this.tcpListener.AcceptTcpClient();

                    //create a thread to handle communication 
                    //with connected client
                    var thr = new Thread(HandleClientComm);
                    thr.Start(client);
                    
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Socket closed prematurely", ex);

                //MainViewModel?.SetOfflineMessage();
            }
        }
    }
}
