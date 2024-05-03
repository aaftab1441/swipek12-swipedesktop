using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using Commands;
using Common;
using Common.Events;
using Common.Models;
using log4net;
using log4net.Repository.Hierarchy;
using Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using ServiceLayer.Common;
using ServiceStack;
using ServiceStack.Common;
using ServiceStack.ServiceClient.Web;
using ServiceStack.Text;
using Swipe.Common.Events;
using SwipeDesktop.Common;
using SwipeDesktop.Models;
using SwipeDesktop.ViewModels;
using SwipeK12;
using SwipeK12.NextGen.ReadServices;
using SwipeK12.NextGen.ReadServices.Messages;
using CardItem = SwipeK12.CardItem;
using StudentModel = SwipeDesktop.Models.StudentModel;
using SwipeCard = SwipeDesktop.IdCardUtils.SwipeCard;

namespace SwipeDesktop.Api
{
    public class RemoteStorage : ReactiveObject, IReturnData
    {   
        private static readonly Stopwatch Timer = new Stopwatch();

        private static readonly ILog Logger = LogManager.GetLogger(typeof(RemoteStorage));

        public static readonly string ApiUrl = Settings.Default.JsonUrl;

        private static readonly WebClient RemoteClient = new WebClient();

        public ReactiveList<string> ImageList { get; private set; }


        public RemoteStorage()
        {
            ImageList = new ReactiveList<string>();
        }
       
        public IObservable<PersonModel[]> SearchStudents(string filter)
        {
            if (string.IsNullOrEmpty(filter))
                return Observable.Return(new PersonModel[0]);

            IEnumerable<StudentModel> students = new List<StudentModel>();
            using (var client = new JsonServiceClient(ApiUrl))
            {

                var resp = client.Get<List<Models.Student>>(string.Format("{0}/Students/Find?criteria={1}", Settings.Default.SchoolId, filter));

                students = resp.Select(x => new StudentModel() { UniqueId = x.Guid, IdNumber = x.StudentNumber, LastName = x.LastName, FirstName = x.FirstName, Grade = string.IsNullOrEmpty(x.Grade) ? "N/A" : x.Grade, Homeroom = string.IsNullOrEmpty(x.Homeroom) ? "N/A" : x.Homeroom });

            }
            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return Observable.Return(students.ToArray());
        }

        public IObservable<IEnumerable<ScanLocation>> GetLocations(LocationType types)
        {
            IEnumerable<ScanLocation> locations = new List<ScanLocation>();
            using (var client = new JsonServiceClient(ApiUrl))
            {
                try
                {
                    var resp = client.Get<string>(string.Format("{0}/ScanLocations", Settings.Default.SchoolId));

                    var wrapper = resp.FromJson<Response<List<ScanLocation>>>();
                    locations = wrapper.data.Where(x=>x.Type == types);
                }
                catch (Exception ex)
                {
                    Logger.Error("There was a problem retrieving scan locations.",ex);
                }

            }
            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return Observable.Return(locations.ToArray());
        }

        public IEnumerable<Models.SchoolStartTime> GetSchoolStartTimes()
        {
            IEnumerable<Models.SchoolStartTime> times = new List<Models.SchoolStartTime>();
            using (var client = new JsonServiceClient(ApiUrl))
            {
                try
                {
                    var resp = client.Get<string>(string.Format("{0}/CustomStartTimes", Settings.Default.SchoolId));

                    var wrapper = resp.FromJson<Response<List<Models.SchoolStartTime>>>();
                    times = wrapper.data;
                }
                catch (Exception ex)
                {
                    Logger.Error("There was a problem retrieving start times.", ex);
                }

            }
            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return times;
        }

        public IObservable<IEnumerable<VisitorLocation>> GetVisitorLocations()
        {
            IEnumerable<VisitorLocation> locations = new List<VisitorLocation>();
            using (var client = new JsonServiceClient(ApiUrl))
            {
                try
                {
                    var resp = client.Get<string>(string.Format("{0}/visitorLocations", Settings.Default.SchoolId));

                    var wrapper = resp.FromJson<Response<List<VisitorLocation>>>();
                    locations = wrapper.data;
                }
                catch (Exception ex)
                {
                    Logger.Error("There was a problem retrieving scan locations.", ex);
                }

            }
            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return Observable.Return(locations.ToArray());
        }

        public IEnumerable<SwipeCard.IDCardsRow> GetIdCards()
        {
            IEnumerable<SwipeCard.IDCardsRow> cards = new List<SwipeCard.IDCardsRow>();
            using (var client = new JsonServiceClient(ApiUrl))
            {
                try
                {
                    var resp = client.Get<string>(string.Format("{0}/IdCards", Settings.Default.SchoolId));

                    var wrapper = resp.FromJson<Response<List<IdCardEnvelope>>>();

                    cards = wrapper.data.Select(x =>
                    {
                        var fields = MapToSource(x.Fields);
                        var xml = Serialisation.SerializeObject<CardItem[]>(fields);
                        if (xml.Length > 0 && xml[0] != '<')
                        {
                            xml = xml.Substring(1, xml.Length - 1);
                        }
                        var row = new SwipeCard.IDCardsDataTable().NewIDCardsRow();
                        row.Active = x.Active;
                        row.BackBackground = x.BackBackground;
                        row.BackPortrait = x.BackPortrait;
                        row.BackOpacity = x.BackOpacity;
                        row.CardName = x.CardName;
                        row.CardWidth = x.CardWidth;
                        row.CardHeight = x.CardHeight;
                        row.CreatedOn = x.CreatedOn;
                        row.DualSided = x.DualSided;
                        row.FrontBackground = x.FrontBackground;
                        row.FrontOpacity = x.FrontOpacity;
                        row.FrontPortrait = x.FrontPortrait;
                        row.OtherCard = x.OtherCard;
                        row.SchoolID = x.SchoolId;
                        row.StudentCard = x.StudentCard;
                        row.TeacherCard = x.TeacherCard;
                        row.TempCard = x.TempCard;
                        row.UpdatedOn = x.UpdatedOn;
                        row.Fields = xml;

                        return row;
                    });
                }
                catch (Exception ex)
                {
                    Logger.Error("There was a problem retrieving scan locations.", ex);
                }

            }
            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return cards.Where(x=>x.Active).ToArray();
        }

        public IObservable<bool> SendLocationScan(Models.LocationScan o)
        {
            var scan = new StationScan()
            {
                StudentNumber = o.StudentNumber,
                StationId = Environment.MachineName,
                SchoolId = Settings.Default.SchoolId,
                RoomName = o.RoomName,
                ScanType = ScanType.Location,
                ScanTime = o.SwipeTime,
                ProcessAttendance = Settings.Default.TakeAttendance,
                IsMarkPresentOverride = Settings.Default.MarkPresentInLocationMode
            };

            
            if(o.IsKiosk)
            {
                scan.KioskMode = true;
                scan.Isleaving = o.SwipedOut;
            }

            var client = new JsonServiceClient(ApiUrl);

            var cmd = new CommandEnvelope()
            {
                Command = scan
            };

            var resp = client.Send(cmd);
            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return Observable.Return(true);
        }

        CardItem[] MapToSource(SwipeK12.NextGen.ReadServices.CardItem[] source)
        {
            var items = new List<CardItem>();
            foreach (var item in source)
            {
                items.Add(new CardItem()
                {
                    Alignment = item.Alignment,
                    Background = item.Background,
                    ControlType = item.ControlType,
                    FieldType = item.FieldType,
                    Foreground = item.Foreground,
                    Height = item.Height,
                    Left = item.Left,
                    Name = item.Name,
                    Side = item.Side,
                    Source = item.Source,
                    Text = item.Text,
                    TextBold = item.TextBold,
                    TextItalic = item.TextItalic,
                    TextUnderline = item.TextUnderline,
                    TextFont = item.TextFont,
                    TextSize = item.TextSize,
                    Top = item.Top,
                    Width = item.Width,
                    ZIndex = item.ZIndex
                });
            }

            return items.ToArray();
        }
        public IObservable<bool> SendConsequence(Consequence o)
        {
            var url = "/Station/Publish/ConsequenceAssigned";

            var cmd = new ConsequenceAssigned
            {
                AggregateId= o.StudentGuid,
                StudentNumber = o.StudentNumber, 
                SchoolId = Settings.Default.SchoolId,
                DateAdded = o.InfractionDate,
                Text = o.Details,
                Units = o.Units,
                SubmitBy = Environment.MachineName,
                Type = o.OutcomeType.ToString(),
                ServeBy = o.ServeBy
                
            };

            using (var client = new JsonServiceClient(ApiUrl))
            {

                var json = client.Post<object>(string.Format(url), cmd);

                //var resp = json.FromJson<ExpandoObject>();

            }
            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return Observable.Return(true);
        }

        public IObservable<bool> SendAlertPrinted(AlertPrinted o)
        {
            var url = "/Station/Publish/DeactivateAlert";
          
            var cmd = new DeactivateAlert
            {
                Header = new MessageHeader() { Station = Environment.MachineName, SchoolId = Settings.Default.SchoolId },
                CorrelationId = o.CorrelationId
            };

            using (var client = new JsonServiceClient(ApiUrl))
            {

                var json = client.Post<object>(string.Format(url), cmd);

            }
           
            return Observable.Return(true);
        }

        public IObservable<bool> SendVisitorData(VisitModel o)
        {
            var url = "/Station/Publish/VisitData";

            using (var client = new JsonServiceClient(ApiUrl))
            {

                var json = client.Post<object>(string.Format(url), o);

                //var resp = json.FromJson<ExpandoObject>();

            }
            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return Observable.Return(true);
        }

        public IObservable<bool> SendVisitorExit(VisitExit o)
        {
            var url = "/Station/Publish/VisitExit";

            using (var client = new JsonServiceClient(ApiUrl))
            {

                var json = client.Post<object>(string.Format(url), o);

                //var resp = json.FromJson<ExpandoObject>();

            }
            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return Observable.Return(true);
        }

        public IObservable<bool> SendFine(AssessedFine o)
        {
            var url = "/Station/Publish/FineAssigned";

            var cmd = new FineAssigned
            {
                AggregateId = o.StudentGuid,
                StudentNumber = o.StudentNumber,
                DateAdded = o.FineDate,
                Text = o.Text,
                Amount = o.Amount,
                AmountPaid = o.AmountPaid,
                Header = new MessageHeader() { Station = Environment.MachineName, SchoolId = Settings.Default.SchoolId, Source = o.RecordedBy }
            };

            using (var client = new JsonServiceClient(ApiUrl))
            {

                var json = client.Post<object>(string.Format(url), cmd);

                //var resp = json.FromJson<ExpandoObject>();

            }
            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return Observable.Return(true);
        }

        public AddStudentResponse SendStudent(AddPersonViewModel vm)
        {
            var url = string.Format("/{0}/People/Add", Settings.Default.SchoolId);

            var person = vm.PersonToAdd;
            var student = vm.AdditionalDetails as StudentDetails;
            var cmd = new AddPerson
            {
                SchoolId = Settings.Default.SchoolId,
                IdNumber = person.IdNumber,
                FirstName = person.FirstName,
                LastName = person.LastName,
                DateOfBirth = DateTime.Parse(person.DateOfBirth),
                Grade = student.Grade,
                Homeroom = student.Homeroom,
                Bus = student.Bus,
                PersonType = PersonType.Student.ToString(),
                LunchCode = student.LunchCode,
                Header = new MessageHeader() { Station = Environment.MachineName, SchoolId = Settings.Default.SchoolId }
            };

            AddStudentResponse obj = null;
            try
            {
                using (var client = new JsonServiceClient(ApiUrl))
                {

                    var resp = client.Post<string>(string.Format(url), cmd);

                    obj = resp.FromJson<AddStudentResponse>();

                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                vm.PersonToAdd.Error = ex.Message;
                return null;
            }
            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return obj;
        }

        public AddStaffResponse SendStaff(AddPersonViewModel vm)
        {
            var url = string.Format("/{0}/People/Add", Settings.Default.SchoolId);
            var person = vm.PersonToAdd;
            var staff = vm.AdditionalDetails as StaffDetails;

            var cmd = new AddPerson
            {
                SchoolId = Settings.Default.SchoolId,
                IdNumber = person.IdNumber,
                FirstName = person.FirstName,
                LastName = person.LastName,
                DateOfBirth = DateTime.Parse(person.DateOfBirth),
                OfficeLocation = staff.OfficeLocation,
                JobTitle = staff.JobTitle,
                PersonType = PersonType.Staff.ToString(),
                Header = new MessageHeader() { Station = Environment.MachineName, SchoolId = Settings.Default.SchoolId }
            };

            AddStaffResponse obj = null;
            try
            {
                using (var client = new JsonServiceClient(ApiUrl))
                {

                    var resp = client.Post<string>(string.Format(url), cmd);
                    obj = resp.FromJson<AddStaffResponse>();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                vm.ValidationErrors = ex.Message;
                return null;
            }
            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return obj;
        }

        public IObservable<bool> SendIdCardPrinted(NewIdCard o)
        {
            var url = "/Station/Publish/IdCardPrinted";

            var cmd = new IdCardPrinted
            {
                AggregateId = o.StudentGuid,
                StudentNumber = o.StudentNumber,
                DatePrinted = o.PrintDate,
                Type = o.Type,
                Header = new MessageHeader() { Station = Environment.MachineName, SchoolId = Settings.Default.SchoolId, Source = o.RecordedBy }
            };

            using (var client = new JsonServiceClient(ApiUrl))
            {

                var json = client.Post<object>(string.Format(url), cmd);

                //var resp = json.FromJson<ExpandoObject>();

            }
            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return Observable.Return(true);
        }

        public IObservable<bool> SendDismissal(Dismissal o)
        {
            var url = "/Station/Publish/StudentDismissed";

            if(!o.ReEntryTime.HasValue) { 
                var cmd = new StudentDismissed
                {
                    AggregateId = o.StudentGuid,
                    StudentNumber = o.StudentNumber,
                    SchoolId = Settings.Default.SchoolId,
                    DismissalTime = o.DismissalTime,
                    StatusCode = o.StatusCode,
                    Reason = o.Reason,
                    SubmitBy = Environment.MachineName
                    //ProcessAttendance = Settings.Default.TakeAttendance
                };
            
                using (var client = new JsonServiceClient(ApiUrl))
                {

                    var json = client.Post<object>(string.Format(url), cmd);

                    //var resp = json.FromJson<ExpandoObject>();

                }
            }
            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return Observable.Return(true);
        }

        public async Task<bool> SendStaffScanAsync(Models.StaffRecord scan)
        {
            var url = string.Empty;

            dynamic @event = null;

            //if (scan.SwipeMode == SwipeMode.Entry)
            //{
                url = "/Station/Publish/StaffEntry";
                //url = string.Format("/{0}/InOutRecords/New", Settings.Default.SchoolId);

                @event = new
                {
                    scan.PersonId,
                    Source = string.Format("{0}", Environment.MachineName),
                    SchoolId = Settings.Default.SchoolId.ToString(CultureInfo.CurrentCulture),
                    ScanTime = scan.EntryTime,
                    AttendanceCode = scan.StatusCode,
                    scan.Location,
                    IsLeavingLocation = scan.SwipedOut,
                    ScanType = ScanType.Location,
                    IsKiosk = scan.IsKiosk
                };

            //}
            //else { 
            //    return (false);
            //}

            var uri = string.Format("{0}{1}", ApiUrl, url);

            using (HttpClient client = new HttpClient())
            {
                //client.Timeout = TimeSpan.FromMilliseconds(1000);

                var content = new StringContent(JsonConvert.SerializeObject(@event), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(uri, content);
                var respString = await response.Content.ReadAsStringAsync();
            }
            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return (true);
        }

        public IObservable<bool> SendScan(ScanModel scan)
        {
            var url = string.Empty;

           dynamic @event = null;

            if (scan.SwipeMode == SwipeMode.ClassroomTardy)
            {
                url = "/Station/Publish/TardyScan";

                @event = new
                {
                    //__type = "Swipe.Common.Events.TardyScan, Common",
                    StudentNumber = scan.Barcode,
                    StationId = string.Format("{0}", Environment.MachineName),
                    RoomName = scan.Location,
                    SchoolId = Settings.Default.SchoolId.ToString(CultureInfo.CurrentCulture),
                    ScanTime = scan.EntryTime,
                    scan.Period,
                    scan.AttendanceCode,
                    ProcessAttendance = true
                };

            }

            if (scan.SwipeMode == SwipeMode.Entry)
            {
                url = "/Station/Publish/EntryScan";

                @event = new
                {
                    //__Type = "Swipe.Common.Events.EntryScan, Common",
                    StudentNumber = scan.Barcode,
                    StationId = string.Format("{0}", Environment.MachineName),
                    //RoomName = string.Empty,
                    SchoolId = Settings.Default.SchoolId.ToString(CultureInfo.CurrentCulture),
                    ScanTime = scan.EntryTime,
                    IsManualScan = scan.IsManual,
                    IsMarkPresentOverride = scan.MarkAllPresent
                };
            }
            if (scan.SwipeMode == SwipeMode.Location)
            {
                return Observable.Return(false);
            }
            using (var client = new JsonServiceClient(ApiUrl))
            {

                var json = client.Post<string>(string.Format(url), @event);

                //var resp = json.FromJson<ExpandoObject>();

            }
            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return Observable.Return(true);
        }

        public async Task<bool> SendScanAsync(ScanModel scan)
        {
            var url = string.Empty;

            dynamic @event = null;

            if (scan.SwipeMode == SwipeMode.ClassroomTardy)
            {
                url = "/Station/Publish/TardyScan";

                @event = new
                {
                    //__type = "Swipe.Common.Events.TardyScan, Common",
                    StudentNumber = scan.Barcode,
                    StationId = string.Format("{0}", Environment.MachineName),
                    RoomName = scan.Location,
                    SchoolId = Settings.Default.SchoolId.ToString(CultureInfo.CurrentCulture),
                    ScanTime = scan.EntryTime,
                    scan.Period,
                    scan.AttendanceCode,
                    ProcessAttendance = Settings.Default.TakeAttendance
                };

            }

            if (scan.SwipeMode == SwipeMode.Entry)
            {
                url = "/Station/Publish/EntryScan";

                @event = new
                {
                    //__Type = "Swipe.Common.Events.EntryScan, Common",
                    StudentNumber = scan.Barcode,
                    StationId = string.Format("{0}", Environment.MachineName),
                    //RoomName = string.Empty,
                    SchoolId = Settings.Default.SchoolId.ToString(CultureInfo.CurrentCulture),
                    ScanTime = scan.EntryTime,
                    IsManualScan = scan.IsManual,
                    IsMarkPresentOverride = scan.MarkAllPresent
                };
            }
            if (scan.SwipeMode == SwipeMode.Location)
            {
                return (false);
            }

            var uri = string.Format("{0}{1}", ApiUrl, url);

            using (HttpClient client = new HttpClient())
            {
                //client.Timeout = TimeSpan.FromMilliseconds(1000);

                var content = new StringContent(JsonConvert.SerializeObject(@event), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(uri, content);
                var respString = await response.Content.ReadAsStringAsync();
            }
            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return (true);
        }

        public async Task<bool> SendVisitEntryAsync(VisitorEntry visit)
        {
            try
            {
                visit.Source = Environment.MachineName;

                var url = "/Station/Publish/VisitorEntry";

                var uri = string.Format("{0}{1}", ApiUrl, url);

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders
                        .Accept
                        .Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var json = JsonConvert.SerializeObject(visit);
                    //client.Timeout = TimeSpan.FromMilliseconds(1000);
                    //visit.SendNotification = true;
                    var content = new StringContent(json, Encoding.UTF8,
                        "application/json");
                    var response = await client.PostAsync(uri, content);
                    var respString = await response.Content.ReadAsStringAsync();

                    Logger.Debug(respString);

                    var responseObj = JsonConvert.DeserializeObject<SwipeDesktop.Models.VisitResponse>(respString);

                    if (responseObj.FlaggedForOffender)
                        return (true);

                }

                return (false);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to send VisitorEntry", ex);
            }
            
            return (false);
        }

        public void DownloadStudentImages(string[] imageNames)
        {
            ImageList.AddRange(imageNames);
           
            string url = string.Format(Settings.Default.ImageUrl, Settings.Default.SchoolId, ImageList[0]);
            var filePath = string.Format("{0}\\{1}", Settings.Default.ImagesFolder, ImageList[0]);

            RemoteClient.DownloadFileCompleted += DownloadFileCompleted;

            Timer.Start();

            DownloadAndSaveImage(url, filePath, RemoteClient);

        }

        private void DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            var filePath = string.Format("{0}\\{1}", Settings.Default.ImagesFolder, ImageList[0]);

            //check last file downloaded
            try
            {
                if (File.Exists(filePath) && File.ReadAllBytes(filePath).Length == 0)
                {
                    File.Delete(filePath);
                }
            }
            catch { /*no-op*/ }

            //continue to next file
            ImageList.RemoveAt(0);
            if (ImageList.Count == 0)
            {
                Timer.Stop();
                Logger.DebugFormat("Image sync completed in {0} minutes.", Timer.Elapsed.TotalMinutes);
                Timer.Reset();
            }

            if (ImageList.Count > 0)
            {
                var nextFile = string.Format("{0}\\{1}", Settings.Default.ImagesFolder, ImageList[0]);
                string url = string.Format(Settings.Default.ImageUrl, Settings.Default.SchoolId, ImageList[0]);

                DownloadAndSaveImage(url, nextFile, RemoteClient);
            }
        }

        void DownloadAndSaveImage(string url, string filePath, WebClient client)
        {
            try
            {
                if(File.Exists(filePath))
                    File.Delete(filePath);

                client.DownloadFileAsync(new Uri(url), filePath);
            }
            catch (Exception ex)
            {
                Logger.Warn(string.Format("Could not download image: {0}", url), ex);
            }
        }


        public IEnumerable<Fine> Fines(int school, string filter = null)
        {
            throw new NotImplementedException();
        }
    }
}
