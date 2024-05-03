using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SwipeDesktop.Common;

namespace SwipeDesktop.DeviceLibrary
{
    public delegate void BarcodeReadEventHandler(object sender, EventArgs e);
    public class Port : IDisposable
    {
        SerialPort _SerialPort;
        int _baudRate = 9600;
        string _portName;
        private string _terminator  = "\r";
        char leftPreamble = (char) 14;

        string strPreamble = "0E";

        public event BarcodeReadEventHandler BarcodeReceived;

        // Invoke the Changed event; called whenever list changes
        protected virtual void OnBarcode(string barcode, EventArgs e)
        {
            Lane lane = Lane.Right;

            if(barcode.Contains(leftPreamble) || barcode.StartsWith(strPreamble))
            {
                lane = Lane.Left;
                barcode = barcode.TrimStart(strPreamble.ToCharArray()).TrimStart(leftPreamble);
            }

            if (BarcodeReceived != null)
            {
                BarcodeReceived(new { lane, barcode }, e);
            }
              
        }

        public Port(string portName)
        {
            _portName = portName;
        }

        public Port(string portName, int baud) : this(portName)
        {
            _baudRate = baud;
        }

        public void Connect()
        {
            _SerialPort = new SerialPort(_portName, _baudRate);

            _SerialPort.DataReceived += _readSerialPort_DataReceived;
           
            _SerialPort.Open();
        }

        private string _buffer = string.Empty;

        void _readSerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var sp = (SerialPort)sender;
            
            /*
            var messageLength = sp.BytesToRead;
            var buffer = new byte[messageLength];
            sp.Read(buffer, 0, buffer.Length);

            _buffer += System.Text.Encoding.Default.GetString(buffer);
            */

            _buffer += sp.ReadExisting();

            if (_buffer.Contains("\r"))
            {
                /*
                var data = _buffer.Substring(0, _buffer.IndexOf('\r'));
                
                _buffer = _buffer.Substring(_buffer.IndexOf('\r'), _buffer.Length);
                */
                var data = _buffer.Substring(0, _buffer.IndexOf('\r'));
                _buffer = string.Empty;

                OnBarcode(data, EventArgs.Empty);
            }
        }

        public void Close()
        {
            _SerialPort.Close();
        }

        public void Dispose()
        {
            this.BarcodeReceived = null;

            if (_SerialPort != null)
            {
                _SerialPort.DataReceived -= _readSerialPort_DataReceived;
                _SerialPort.Close();
                _SerialPort = null;
            }
        }
    }
}
