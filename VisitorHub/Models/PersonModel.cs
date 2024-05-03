using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using log4net;
using ReactiveUI;
using SwipeDesktop.Validations;
using Telerik.Windows.Media.Imaging;

namespace SwipeDesktop.Models
{
    public class PersonModel : ReactiveObject//, IDataErrorInfo
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(PersonModel));

        private static readonly Uri NullPersonImageUri = new Uri("pack://siteoforigin:,,,/Resources/nullpersonblue.png");

        public PersonModel()
        {
           
            CurrentStatus = "N/A";
            //DateOfBirth = new DateTime(1900,1,1);

            this.WhenAnyValue(x => x.LastName, x => x.FirstName)
            .Select(x => {   if (string.IsNullOrEmpty(x.Item1) || string.IsNullOrEmpty(x.Item2))
                             {
                                 return String.Format("{0}{1}", x.Item1, x.Item2);
                             }
                             else
                             {
                                 return String.Format("{0}, {1}", x.Item1, x.Item2);
                             }
            }).ToProperty(this, x => x.DisplayName, out _displayText);

            this.WhenAnyValue(x => x.PhotoPath)
            .Select(GetImage)
            .ToProperty(this, x => x.Image, out _bitmap);

            this.WhenAnyValue(x => x.DisplayName, x => x.IdNumber).Where(x=>x.Item2 != null)
            .Select(x => String.Format("{0} ({1})", x.Item1, x.Item2.Trim()))
            .ToProperty(this, x => x.LongDisplayName, out _longDisplayText);

            this.WhenAnyValue(x=>x.PersonId).Select(x=>x > 0).ToProperty(this, x => x.IsStaff, out _isStaff);
            //this.WhenAnyValue(x => x.ImageCapture).Where(x => x.Bitmap != null).Select(x=>x.Bitmap).ToProperty(this, x => x.Image, out _bitmap);

            #region Validations

            this.WhenAnyValue(x => x.FirstName).Subscribe(_ =>
            {
                var fNameError =  new FieldValidator().Validate(_, CultureInfo.CurrentCulture).ErrorContent;
                if (fNameError == null)
                    FirstNameError = string.Empty;
                else
                {
                    FirstNameError = fNameError.ToString();
                }

            });

            this.WhenAnyValue(x => x.LastName).Subscribe(_ =>
            {
                var lNameError = new FieldValidator().Validate(_, CultureInfo.CurrentCulture).ErrorContent;
                if (lNameError == null)
                    LastNameError = string.Empty;
                else
                {
                    LastNameError = lNameError.ToString();
                }

            });

            this.WhenAnyValue(x => x.IdNumber).Subscribe(_ =>
            {
                var idError = new IdNumberValidator().Validate(_, CultureInfo.CurrentCulture).ErrorContent;

                if (idError == null)
                    IdNumberError = String.Empty;
                else
                {
                    IdNumberError = idError.ToString();
                }

            });

            this.WhenAnyValue(x => x.DateOfBirth).Subscribe(_ =>
            {
                var dobError = new BirthDateValidator().Validate(_, CultureInfo.CurrentCulture).ErrorContent;
                if (dobError == null)
                {
                    //DateOfBirth = DateTime.Parse(_).ToString("d");
                    DateOfBirthError = string.Empty;
                }
                else
                {
                    DateOfBirthError = dobError.ToString();
                }

            });

            #endregion
        }

        string _fnameError;
        public string FirstNameError
        {
            get { return _fnameError; }
            set { this.RaiseAndSetIfChanged(ref _fnameError, value); }
        }

        string _lnameError;
        public string LastNameError
        {
            get { return _lnameError; }
            set { this.RaiseAndSetIfChanged(ref _lnameError, value); }
        }

        string _idError;
        public string IdNumberError
        {
            get { return _idError; }
            set { this.RaiseAndSetIfChanged(ref _idError, value); }
        }

        string _dobError;
        public string DateOfBirthError
        {
            get { return _dobError; }
            set { this.RaiseAndSetIfChanged(ref _dobError, value); }
        }


        public BitmapSource GetImage(string imageName)
        {
            var fileName = string.Format("{0}\\{1}", Settings.Default.ImagesFolder, imageName);
            if (File.Exists(fileName))
            {
                try
                {
                    var imageBytes = File.ReadAllBytes(fileName);
                    var stream = new MemoryStream(imageBytes);
                    stream.Seek(0, SeekOrigin.Begin);
                    var img = new BitmapImage();

                    img.BeginInit();
                    img.StreamSource = stream;
                    img.EndInit();

                    return img;
                  
                }
                catch (Exception ex)
                {
                    Logger.Warn(string.Format("File could not be loaded {0}.", imageName), ex);
                }

            }
        
            return new BitmapImage(NullPersonImageUri);
        }

     
        readonly ObservableAsPropertyHelper<string> _displayText;
        public string DisplayName
        {
            get { return _displayText.Value; }
        }

        protected ObservableAsPropertyHelper<bool> _isStaff;
        public bool IsStaff
        {
            get { return _isStaff.Value; }
        }

        readonly ObservableAsPropertyHelper<BitmapSource> _bitmap;
        public BitmapSource Image
        {
            get { return _bitmap.Value; }
        }
 
        readonly ObservableAsPropertyHelper<string> _longDisplayText;
        public string LongDisplayName
        {
            get { return _longDisplayText.Value; }
        }
   
        private string _firstName;

        public string FirstName
        {
            get { return _firstName; }
            set { this.RaiseAndSetIfChanged(ref _firstName, value); }
        }

        private RadBitmap _imageBitmapSource;

        public RadBitmap ImageCapture
        {
            get { return _imageBitmapSource; }
            set { this.RaiseAndSetIfChanged(ref _imageBitmapSource, value); }
        }
      
        private string _idNumber;

        public string IdNumber
        {
            get { return _idNumber; }
            set { this.RaiseAndSetIfChanged(ref _idNumber, value); }
        }

        public Guid UniqueId { get; set; }

        public string CurrentStatus { get; set; }

        public DateTime? CurrentEntry { get; set; }

        private string _lastName;

        public string LastName
        {
            get { return _lastName; }
            set { this.RaiseAndSetIfChanged(ref _lastName, value); }
        }

        public int PersonId { get; set; }

        private string _photoPath;

        public string PhotoPath
        {
            get { return _photoPath; }
            set { this.RaiseAndSetIfChanged(ref _photoPath, value); }
        }

        private bool _isManualEntry;

        public bool IsManualEntry
        {
            get { return _isManualEntry; }
            set { this.RaiseAndSetIfChanged(ref _isManualEntry, value); }
        }

        private bool _modelNeedsRefresh;

        public bool ModelNeedsRefresh
        {
            get { return _modelNeedsRefresh; }
            set { this.RaiseAndSetIfChanged(ref _modelNeedsRefresh, value); }
        }

        private string _dateOfBirth;

        public string DateOfBirth
        {
            get { return _dateOfBirth; }
            set { this.RaiseAndSetIfChanged(ref _dateOfBirth, value); }
        }


        private string _error;
        public string Error
        {
            get { return _error; }
            set { this.RaiseAndSetIfChanged(ref _error, value); }
        }

        public string this[string columnName]
        {
            get { throw new NotImplementedException(); }
        }
    }
}
