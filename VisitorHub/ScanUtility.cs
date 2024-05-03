using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Autofac;
using log4net;
using NetScanW;
using SwipeDesktop.Api;
using SwipeDesktop.Common;
using SwipeDesktop.Cssn;
using SwipeDesktop.Interfaces;
using SwipeDesktop.Storage;
using SwipeDesktop.ViewModels;
using MessageBox = System.Windows.MessageBox;

namespace SwipeDesktop
{
    public class ScanUtility
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ScanUtility));

        private bool _initialized;
        private static readonly string duplex = ConfigurationManager.AppSettings["duplex"];

        private static readonly string zoomFactor = ConfigurationManager.AppSettings["zoomFactor"];



        public bool _calibrationShown = false;
        public int ScanLibInit { get; private set; }

        public int ScanDataInit { get; private set; }
        public short ScannerType { get; private set; }

        NetScanW.SLibExClass _scanLib;
        NetScanWex.SLibExClass _scanLibEx;
        NetScanW.CImageClass _scanImage;
        NetScanW.IdDataClass _scanData;
        NetScanWex.IdDataClass _scanDataEx;
        NetScanWex.CImageClass _scanImageEx;
        private NetScanW.CBarCodeClass _barcode;

        private MainViewModel _mainViewModel;

        public ScanUtility(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;

            try
            {
                InitDriver();
            }
            catch (Exception ex)
            {
                Logger.Error("Could not initialize scanner library.",ex);
            }
        }

        public void DoScan()
        {
            int i, angle, Image_A_Assignement, angleA, angleB, state;
            string stateName;

            state = -1;
            Image_A_Assignement = 0;
            angleA = 0;
            angleB = 0;
           
            _scanLib.Resolution = 600;
            _scanLib.ScanHeight = -1;
            _scanLib.ScanWidth = -1;
            _scanLib.ScannerColorScheme = 1;

            var imageId = Guid.NewGuid();
            string imageA = Settings.Default.ImagesFolder + imageId + ".jpg";
            string imageB = Settings.Default.ImagesFolder + imageId + "-back.jpg";
           

            _scanLibEx.Duplex = 1;

            var result = _scanLib.ScanToFileEx(imageA);

            var barcodeImage = imageB;
            var frontImage = imageA;
            
            if (result < 0)
            {

                MessageBox.Show(ScanShellHelper.GetScannerClassError(result));
                return;
            }

            _scanDataEx.RegionSet(0);
         
            var faceImage = Settings.Default.ImagesFolder + "\\" + imageId + "_face.jpg";

            var scan = new VisitorScanViewModel(_mainViewModel, App.Container.Resolve<VisitStorage>(), App.Container.Resolve<RemoteStorage>(), App.Container.Resolve<LocalStorage>());
            if (!string.IsNullOrEmpty(duplex))
            {
              
                state = _scanDataEx.DetectProcessDuplexEX(string.Empty, string.Empty, state, ref Image_A_Assignement, ref angleA, ref angleB, 0);

                //if (Constants.GetScannerNameByType(_scanLib.ScannerType) == "ScanShell 800DX")
                //{
                //    state = _scanData.AutoDetectState(frontImage);

                //    var data = _scanData.ProcState(frontImage, state);
                //}
                if (Image_A_Assignement > 0)
                {
                    barcodeImage = imageA;
                    frontImage = imageB;
                }

                try
                {
                    var barcodeRslt = processBarcode(scan, barcodeImage, frontImage);
                    Logger.DebugFormat("Barcode Process Result: {0}", barcodeRslt);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
            else
            {
                state = _scanData.AutoDetectState(frontImage);

                var data = _scanData.ProcState(frontImage, state);

                if (data < 0)
                {

                    var msg = ScanShellHelper.GetDataClassError(data);

                    if (string.IsNullOrEmpty(duplex))
                    {
                        if (msg == "ID_ERR_STATE_NOT_SUPORTED")
                        {
                            msg = "Could not recognize Drivers License, please proceed to the front office.";
                        }

                        MessageBox.Show(msg);

                        return;
                    }
                }

                _scanData.RefreshData();

                scan.BitmapSource = new BitmapImage();
                scan.BitmapSource.Freeze();
                
                scan.FirstName = _scanData.NameFirst;
                scan.LastName = _scanData.NameLast;
                scan.Street1 = _scanData.Address;
                scan.City = _scanData.City;
                scan.State = _scanData.State;
                scan.Zip = _scanData.Zip;
                scan.Identification = _scanData.Id;
                scan.VisitEntryDate = DateTime.Now;
                scan.School = Settings.Default.SchoolId;
                scan.ImagePath = imageA;
                scan.BitmapSource = new BitmapImage(new Uri(imageA));
                DateTime parsed;
                if (DateTime.TryParse(_scanData.DateOfBirth, out parsed))
                {
                    scan.DateOfBirth = parsed;
                }
                result = _scanData.GetFaceImage(imageA, faceImage, state);

                if (result > 0)
                {
                    scan.ImagePath = faceImage;
                    scan.BitmapSource = new BitmapImage(new Uri(faceImage));
                    scan.BitmapSource.Freeze();
                }
            }

            _mainViewModel.CurrentView = scan;
        }
        public static Bitmap ConvertToBitmap(string fileName)
        {
            Bitmap bitmap;
            using (Stream bmpStream = System.IO.File.Open(fileName, System.IO.FileMode.Open))
            {
                Image image = Image.FromStream(bmpStream);

                bitmap = new Bitmap(image);
                bitmap.Save(fileName.Replace("jpg", "bmp"));
            }
            return bitmap;
        }

        public static string ProcessFaceImage(string frontImage, string stateCode)
        {
            var stateName = string.Empty;

            int cropOffsetX = 140, cropOffsetY = 340;

            if (Settings.Default.faceOffsetX > 0)
            {
                cropOffsetX = Settings.Default.faceOffsetX;
            }

            if (Settings.Default.faceOffsetY>0)
            {
                cropOffsetY = Settings.Default.faceOffsetY;
            }

            var bmp = ConvertToBitmap(frontImage);
            var bmpFileName = frontImage.Replace("jpg", "bmp");


            if (Settings.Default.faceZoom < 1)
            {
                Size newSize = new Size((int)(bmp.Width * Settings.Default.faceZoom), (int)(bmp.Height * Settings.Default.faceZoom));
                Bitmap newBmp = new Bitmap(bmp, newSize);

                bmp = newBmp;
            }
            //bmpCropped = null;
            //Bitmap bmpCrop = null;
            //if (stateCode.ToUpper() == "MD")
            //{
            var bmpCropped = bmp.cropAtRect(new Rectangle(cropOffsetX, cropOffsetY, 540, 650));
            bmpCropped.Save(bmpFileName);
            bmpCropped.Dispose();
            bmp.Dispose();
            //}

            return bmpFileName;
        }

        public void ProcessImage(string imagePath)
        {
            _scanLib.Resolution = 300;
            _scanLib.ScanHeight = -1;
            _scanLib.ScanWidth = -1;

            _scanImageEx.SetIrImage(imagePath);

            _scanDataEx.RegionSet(0);

            var stateId = _scanData.AutoDetectState(imagePath);

            //Console.WriteLine("State detected: " + stateId);

            var result = _scanData.ProcState(imagePath, stateId);

            _scanData.RefreshData();

            //MessageBox.Show(string.Format("Hello {0} {1}.", _scanData.NameFirst, _scanData.NameLast));
        }

        public bool CheckForPaper()
        {
             InitDriver();

             return _scanLib.PaperInTray == 1; 
        }
        // ---------------------------------------------------------------

        bool processBarcode(VisitorScanViewModel scan, string barcodeImage, string frontImage)
        {
            Bitmap face = null;
            //var scanRtnVal = _scanData.RefreshData();
            int procImageVal = 0;
            if (Constants.GetScannerNameByType(_scanLib.ScannerType) == "ScanShell 800DX")
            {
                procImageVal = _barcode.ProcImage(string.Empty);
            }
            else
            {
                procImageVal = _barcode.ProcImage(barcodeImage);
            }

            if (procImageVal < 0)
                return false;

            var readBarcode = _barcode.RefreshData();

            if (readBarcode < 0)
                return false;

            string faceImage = frontImage;


            string rawData;
            _barcode.GetRawData(out rawData);

            scan.BitmapSource = new BitmapImage();
            scan.BitmapSource.Freeze();
            scan.FirstName = _barcode.NameFirst;
            scan.LastName = _barcode.NameLast;
            scan.Street1 = _barcode.Address;
            scan.City = _barcode.City;
            scan.State = _barcode.State;
            scan.Zip = _barcode.Zip;
            scan.Identification = _barcode.license;
            scan.VisitEntryDate = DateTime.Now;
            scan.School = Settings.Default.SchoolId;
            DateTime parsed;
            if (DateTime.TryParse(_barcode.DateOfBirth, out parsed))
            {
                scan.DateOfBirth = parsed;
            }

            var croppedPath = ProcessFaceImage(faceImage, scan.State);
            //if (face != null)
            //{
                faceImage = croppedPath;
                scan.ImagePath = faceImage;
                scan.BitmapSource = new BitmapImage(new Uri(faceImage));
                scan.BitmapSource.Freeze();
            //}

            return true;
        }

        public bool CheckForScanner()
        {
            InitDriver();

            return _scanLib.IsScannerValid == 1; 
        }

        void InitDriver()
        {
            if (!_initialized)
            {
                _scanLib = new NetScanW.SLibExClass();

                _scanData = new NetScanW.IdDataClass();

                _scanLibEx = new NetScanWex.SLibExClass();
                _scanLibEx.DefaultScanner = Constants.CSSN_800DXN;

                _scanDataEx = new NetScanWex.IdDataClass();

                _scanImage = new NetScanW.CImageClass();

                _scanImageEx = new NetScanWex.CImageClass();

                _barcode = new NetScanW.CBarCodeClass();

                ScanLibInit = _scanLib.InitLibrary(Settings.Default.SDKLicense);
                if (ScanLibInit < 1)
                {

                    //MessageBox.Show("There was a problem connecting the scanner.");

                }
                ScanDataInit = _scanData.InitLibrary(Settings.Default.SDKLicense);
                var barcodeInit = _barcode.InitLibrary(Settings.Default.SDKLicense);

                _initialized = true;

                ScannerType = _scanLib.ScannerType;


                //if(Settings.Default.Calibrate)
                if (_scanLib.IsNeedCalibration == 1 && !_calibrationShown)
                {

                    MessageBox.Show("Please calibrate scanner to correct image quality issues.");
                    _calibrationShown = true;
                    //_scanLib.CalibrateScanner();
                }
            }
        }
    }
}
