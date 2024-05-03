using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

using log4net;
using Massive;
using Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServiceStack.ServiceClient.Web;
using ServiceStack.Text;
using Swipe.Common.Models;

using SwipeK12;
using SwipeK12.NextGen.ReadServices;
using SwipeK12.NextGen.ReadServices.Messages;

using JsonSerializer = ServiceStack.Text.JsonSerializer;

namespace SwipeDesktop.Common
{
    public class DataReplicationLibrary
    {
        private string _api;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(DataReplicationLibrary));
        static readonly string ConnectionString = ConfigurationManager.ConnectionStrings["ScanStation"].ConnectionString;
        private JsonServiceClient _client;

        public DataReplicationLibrary(string api)
        {
            _api = api;
            _client = new JsonServiceClient(_api);
        }
     

        private static Stopwatch Timer = new Stopwatch();

        bool CheckInternet()
        {
            string status;
            var connected = InternetAvailability.IsInternetAvailable(out status);

            return connected;
        }

        public string Full(int schoolId)
        {
            if (!CheckInternet())
            {
                Logger.ErrorFormat("Network connection not detected, skipping full sync at {0}.", DateTime.Now);
                return string.Empty;
            }

            var timer = new Stopwatch();
            List<string> errors = new List<string>();

            long totalRecords = 0;
            try
            {
                timer.Start();

                string url = string.Format("Students/Download?currentPage=0&schoolId={0}", schoolId);


                var  getStudentsResponse = _client.Get<string>(url);
                var resp = ServiceStack.Text.JsonSerializer.DeserializeFromString<Body<BulkStudentsResponse>>(getStudentsResponse);

                errors.AddRange(ProcessStudentDownload(resp.data.Students));

                totalRecords = resp.data.TotalRecords;
                int totalPages = resp.data.TotalPages;

                if (totalPages > 1)
                {
                    for (int p = 0; p < totalPages; p++)
                    {
                        url = string.Format("Students/Download?currentPage={1}&schoolId={0}", schoolId, p + 1);

                        var json = _client.Get<string>(url);

                        resp = ServiceStack.Text.JsonSerializer.DeserializeFromString<Body<BulkStudentsResponse>>(json);

                        errors.AddRange(ProcessStudentDownload(resp.data.Students));
                    }
                }

                timer.Stop();
                Logger.WarnFormat("{0} students replication complete in {1}.", totalRecords, timer.Elapsed.TotalSeconds);

                /*using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 120;
                    cmd.CommandText = string.Format("EXEC DataReplicator '{0}',0,'Snapshot',{1}", MasterIp, SchoolId);

                    var rtn = cmd.ExecuteNonQuery();
                }

                Logger.WarnFormat("snapshot replication complete in {0}.", timer.Elapsed.TotalSeconds);
                 */

                return string.Format("{0} students downloaded in {1} seconds.", totalRecords, timer.Elapsed.TotalSeconds);
                //return true;
            }
            catch (Exception ex)
            {
                //return false;
                Logger.Error("Full replication failed.", ex);
            }
            finally
            {
                timer.Reset();
            }

            if (errors.Any())
            {
                foreach(var s in errors)
                    Logger.Error(s);
            }
            return string.Empty;
        }

        public string AltIds(int school)
        {
            if (!CheckInternet())
            {
                Logger.ErrorFormat("Network connection not detected, skipping full sync at {0}.", DateTime.Now);
                return string.Empty;
            }

            var timer = new Stopwatch();

            long totalRecords = 0;
            try
            {
                timer.Start();

                string url = string.Format("Students/Download?currentPage=0&schoolId={0}", school);


                var getStudentsResponse = _client.Get<string>(url);
                var resp = ServiceStack.Text.JsonSerializer.DeserializeFromString<Body<BulkStudentsResponse>>(getStudentsResponse);

                ProcessAltIds(resp.data.Students);

                totalRecords = resp.data.TotalRecords;
                int totalPages = resp.data.TotalPages;

                if (totalPages > 1)
                {
                    for (int p = 0; p < totalPages; p++)
                    {
                        url = string.Format("Students/Download?currentPage={1}&schoolId={0}", school, p + 1);

                        var json = _client.Get<string>(url);

                        resp = ServiceStack.Text.JsonSerializer.DeserializeFromString<Body<BulkStudentsResponse>>(json);

                        ProcessAltIds(resp.data.Students);
                    }
                }

                timer.Stop();
                Logger.WarnFormat("{0} students replication complete in {1}.", totalRecords, timer.Elapsed.TotalSeconds);

                return string.Format("{0} students downloaded in {1} seconds.", totalRecords, timer.Elapsed.TotalSeconds);
                //return true;
            }
            catch (Exception ex)
            {
                //return false;
                Logger.Error("Full replication failed.", ex);
            }
            finally
            {
                timer.Reset();
            }

            return string.Empty;
        }
        public string RemoteSnapshot(string masterIP, string schoolId)
        {
            if (!CheckInternet())
            {
                Logger.ErrorFormat("Network connection not detected, skipping Snapshot sync at {0}.", DateTime.Now);
                return string.Empty;
            }

            var timer = new Stopwatch();

            long totalRecords = 0;
            try
            {
                timer.Start();

               
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 120;
                    cmd.CommandText = string.Format("EXEC DataReplicator '{0}',0,'Snapshot',{1}", masterIP, schoolId);

                    var rtn = cmd.ExecuteNonQuery();
                }

                timer.Stop();

                var log = string.Format("Remote Snapshot replication complete in {0}.", timer.Elapsed.TotalSeconds);
                Logger.WarnFormat(log);

                return log;
                //return true;
            }
            catch (Exception ex)
            {
                //return false;
                Logger.Error("Remote Snapshot replication failed.", ex);
            }
            finally
            {
                timer.Reset();
            }

            return string.Empty;
        }

        public string SnapshotSince(int minutes, int school)
        {
            if (!CheckInternet())
            {
                Logger.ErrorFormat("Network connection not detected, skipping BulkStudents sync at {0}.", DateTime.Now);
                return string.Empty;
            }

            var timer = new Stopwatch();

            long totalRecords = 0;
            try
            {
                timer.Start();

                Logger.WarnFormat("replicating students changed since {0}.", DateTime.Now.AddMinutes(-minutes));

                string url = string.Format("Students/Download?schoolId={0}&ChangedSince={1}", school, minutes);

                var getStudentsResponse = _client.Get<string>(url);

                var resp = JsonSerializer.DeserializeFromString<Body<BulkStudentsResponse>>(getStudentsResponse);

                totalRecords = resp.data.Students.Count();

                if (totalRecords > 0)
                {
                    ProcessStudentDownload(resp.data.Students);

                    timer.Stop();
                    Logger.WarnFormat("{0} students replication complete in {1}.", totalRecords,
                        timer.Elapsed.TotalSeconds);

                    return string.Format("{0} students downloaded in {1} seconds.", totalRecords,
                        timer.Elapsed.TotalSeconds);
                }
                //return true;
            }
            catch (Exception ex)
            {
                //return false;
                Logger.Error("SnapshotSince replication failed.", ex);
            }
            finally
            {
                timer.Reset();
            }

            return string.Empty;
        }

        /*
        public string StationAlerts(int schoolId, bool clearall)
        {
            if (!CheckInternet())
            {
                Logger.ErrorFormat("Network connection not detected, skipping station alerts at {0}.", DateTime.Now);
                return string.Empty;
            }

            var timer = new Stopwatch();

            try
            {
                timer.Start();


                IEnumerable<StationAlert> alerts = new List<StationAlert>();
               
                try
                {
                    var resp = _client.Get<string>(string.Format("{0}/ActiveStationAlerts", schoolId));

                    var wrapper = resp.FromJson<Response<List<StationAlert>>>();
                    alerts = wrapper.data;

                    ProcessStationAlertsDownload(alerts, schoolId, clearall);
                }
                catch (Exception ex)
                {
                    Logger.Error("There was a problem retrieving station Alerts.", ex);
                }

                
               
                timer.Stop();
                Logger.WarnFormat("Station Alerts replication complete in {0}.", timer.Elapsed.TotalSeconds);


                return string.Format("{0} station alerts downloaded in {1} seconds.", alerts.Count(), timer.Elapsed.TotalSeconds);
                //return true;
            }
            catch (Exception ex)
            {
                //return false;
                Logger.Error("Rooms replication failed.", ex);
            }
            finally
            {
                timer.Reset();
            }
            return string.Empty;
        }
       
        public static string InOutRooms(int schoolId)
        {
            if (!CheckInternet())
            {
                Logger.ErrorFormat("Network connection not detected, skipping InOutRooms sync at {0}.", DateTime.Now);
                return string.Empty;
            }

            var timer = new Stopwatch();

            try
            {
                timer.Start();

                IEnumerable<ScanLocation> locations = new List<ScanLocation>();
                using (var client = _client)
                {
                    try
                    {
                        var resp = client.Get<string>(string.Format("{0}/ScanLocations", schoolId));

                        var wrapper = resp.FromJson<Response<List<ScanLocation>>>();
                        locations = wrapper.data;//.Where(x => x.Type == types);

                        ProcessInOutRoomsDownload(locations);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("There was a problem retrieving scan locations.", ex);
                    }

                }
                //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));

                timer.Stop();
                Logger.WarnFormat("Rooms replication complete in {0}.", timer.Elapsed.TotalSeconds);


                return string.Format("{0} locations downloaded in {1} seconds.", locations.Count(), timer.Elapsed.TotalSeconds);
                //return true;
            }
            catch (Exception ex)
            {
                //return false;
                Logger.Error("Rooms replication failed.", ex);
            }
            finally
            {
                timer.Reset();
            }
            return string.Empty;
        }
        */

        public string Consequences(int schoolId)
        {
            if (!CheckInternet())
            {
                Logger.ErrorFormat("Network connection not detected, skipping Consequences sync at {0}.", DateTime.Now);
                return string.Empty;
            }

            var timer = new Stopwatch();

            try
            {
                timer.Start();

                //IEnumerable<string> data = new List<string>();
                using (var client = _client)
                {
                    try
                    {
                        var resp = client.Get<string>(string.Format("{0}/CorrectiveActions", schoolId));

                        var wrapper = resp.FromJson<Response<List<JsonObject>>>();
                        var data = wrapper.data;//.Where(x => x.Type == types);

                        ProcessConsequenceDownload(data, schoolId);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("There was a problem retrieving consequences.", ex);
                    }

                }
                //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));

                timer.Stop();
                Logger.WarnFormat("Consequences replication complete in {0}.", timer.Elapsed.TotalSeconds);


                return string.Format("data downloaded in {0} seconds.", timer.Elapsed.TotalSeconds);
                //return true;
            }
            catch (Exception ex)
            {
                //return false;
                Logger.Error("Consequences replication failed.", ex);
            }
            finally
            {
                timer.Reset();
            }
            return string.Empty;
        }

        void ProcessConsequenceDownload(IEnumerable<JsonObject> data, int schoolId)
        {

            var now = DateTime.Now;

            var errors = new List<string>();
            var db = new DynamicRepository("CorrectiveActions", "Id");

            using (var conn = db.OpenConnection())
            {
                var trunc = CreateCommand(conn, "Update CorrectiveActions set Active=0 where SchoolId = " + schoolId);
                trunc.ExecuteNonQuery();

                foreach (var item in data)
                {
                   
                    try
                    {

                        dynamic exists = db.SingleWhere("Name = @0 AND SchoolId = @1", new object[] { item["Name"], schoolId });

                        dynamic d =
                            new
                            {
                                //Id = item["Id"],
                                Name = item["Name"],
                                Message = item["Message"],
                                IncidentCount = item["IncidentCount"],
                                IncidentCode = item["IncidentCode"],
                                Repeats = item["Repeats"],
                                Active = item["Active"],
                                SchoolId = item["SchoolId"],
                                OutcomeType = item["OutcomeType"],
                                StartDate = DateTime.Parse(item["StartDateString"]).Date,
                                Period = item["Period"],
                                Operator = item["Operator"]
                            };

                        if (exists != null)
                        {
                       
                            db.Update(d, exists.Id);

                        }
                        else
                        {
                            Logger.DebugFormat("consequence {0} does not exist.", item["Name"]);

                            db.Insert(d);
                        }

                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex.Message);
                    }
                    finally
                    {
                      
                    }
                }

            }
            //return errors;
        }

        /*
        * 
        * 
        static void ProcessStationAlertsDownload(IEnumerable<StationAlert> data, int schoolId, bool clearAll)
        {

            var now = DateTime.Now;

            var errors = new List<string>();
            var table = new DynamicRepository("StationAlerts", "AlertId");

            //var ids = new List<int>();

            using (var conn = table.OpenConnection())
            {
                if (clearAll)
                {
                    table.Delete(where: string.Format("SchoolId = {0}", schoolId));
                }
                foreach (StationAlert item in data)
                {

                    try
                    {
                       
                        dynamic exists = table.SingleWhere("CorrelationId = @0 AND SchoolId = @1", new object[] { item.CorrelationId, schoolId });

                        if (exists != null)
                        {

                            table.Update(new { item.Expires, item.AlertSound, item.AlertColor, item.AlertText, item.Active, item.CorrelationId, item.SchoolId, item.StudentId, item.AlertType }, exists.AlertId);

                        }
                        else
                        {
                            Logger.DebugFormat("alert {0} does not exist.", item.CorrelationId);

                            table.Insert(new { item.Expires, item.AlertSound, item.AlertColor, item.AlertText, item.Active, item.CorrelationId, item.SchoolId, item.StudentId, item.AlertType });
                        }

                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex.Message);
                    }
                    finally
                    {

                    }
                }


            }
           
        }

     
        static void ProcessInOutRoomsDownload(IEnumerable<ScanLocation> data, int schoolId)
        {

            var now = DateTime.Now;

            var errors = new List<string>();
            var rooms = new DynamicRepository("InOutRooms", "IORId");
         
            using (var conn = rooms.OpenConnection())
            {
              
                foreach (ScanLocation item in data)
                {

                    try
                    {
                       
                        dynamic exists = rooms.SingleWhere("RoomName = @0 AND SchoolId = @1",
                            new object[] { item.RoomName, schoolId });

                        int personId = 0;

                        if (exists != null)
                        {

                            exists.Active = 1;
                            exists.location_type = item.Type;
                            exists.LastUpdated = now;

                            rooms.Update(exists, exists.IORId);

                        }
                        else
                        {
                            Logger.DebugFormat("room {0} does not exist.", item.RoomName);

                            rooms.Insert(new {IORId = item.Id, item.SchoolId, item.RoomName, Active=1, LastUpdated= DateTime.Now, location_type = (int)item.Type});
                        }

                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex.Message);
                    }
                    finally
                    {

                    }
                }

                var sql = "Update InOutRooms set Active=0 where SchoolId = " + schoolId + " AND LastUpdated < '" + now.ToString("MM/dd/yyyy HH:mm:ss") + "'";

                var trunc = CreateCommand(conn, sql);
                trunc.ExecuteNonQuery();

            }
            //return errors;
        }
        */

        IEnumerable<string> ProcessAltIds(IEnumerable<StudentModel> data)
        {

            var now = DateTime.Now;

            var errors = new List<string>();
            var students = new DynamicRepository("Students", "StudentID");
            var people = new DynamicRepository("Person", "PersonID");
            var altIds = new DynamicRepository("PersonIdno", "BarcodeID");

            using (var conn = people.OpenConnection())
            {

                foreach (StudentModel student in data)
                {
                    //if (student.Active != "Y")
                    //    continue;

                    try
                    {
                        dynamic exists = students.SingleWhere("StudentID = @0 AND SchoolId = @1",
                            new object[] { student.Id, student.SchoolId });

                        int personId = 0;

                        if (student.ActiveIds != null)
                        {
                            altIds.Execute("Update PersonIDNo set Active=@0, LastUpdated=@1 where PersonID=@2",
                                new object[] { false, DateTime.Now, personId });
                            //update alt ids
                            foreach (var id in student.ActiveIds)
                            {

                                //altIds.Execute("Update PersonIDNo set Active=@0, LastUpdated=@1 where PersonID=@2", new object[] { false, DateTime.Now, person.PersonID });

                                dynamic altId = altIds.SingleWhere("BarcodeId = @0 AND PersonID=@1",
                                    new object[] { id, personId });

                                if (altId != null)
                                {

                                    altIds.Execute(
                                        "Update PersonIDNo set Active=1, LastUpdated=getdate() where PersonID=@0 AND BarcodeID=@1",
                                        new object[] { personId, id });

                                }
                                else
                                {
                                    altIds.Execute(
                                        "Insert into PersonIDNo(SchoolID, PersonID, BarcodeID, Temp, Active,LastUpdated) VALUES(@0,@1,@2,0,1,getdate())",
                                        new object[] { student.SchoolId, personId, id });
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex.Message);
                        Logger.Error(ex);
                    }
                    finally
                    {

                    }
                }
                Logger.WarnFormat("Processed {0} alt ids.", data.Count());
            }
            return errors;
        }
        IEnumerable<string> ProcessStudentDownload(IEnumerable<StudentModel> data)
        {

            var asyncConnection = new SqlConnectionStringBuilder(ConnectionString) { AsynchronousProcessing = true }.ToString();

            var now = DateTime.Now;

            var errors = new List<string>();
            var students = new DynamicRepository("Students", "StudentID");
            var people = new DynamicRepository("Person", "PersonID");
            var altIds = new DynamicRepository("PersonIdno", "BarcodeID");

            using (var conn = people.OpenConnection())
            {

                foreach (StudentModel student in data)
                {
                    //if (student.Active != "Y")
                    //    continue;

                    try
                    {
                        dynamic exists = students.SingleWhere("StudentID = @0 AND SchoolId = @1",
                            new object[] { student.Id, student.SchoolId });

                        int personId = 0;

                        if (exists != null)
                        {

                            //Logger.DebugFormat("student {0} exists.", student.StudentNumber);
                            dynamic person = people.SingleWhere("PersonId = @0 AND SchoolId = @1 and PersonTypeID = 1",
                                new object[] { student.PersonId, student.SchoolId });

                            if (person != null)
                            {
                                personId = person.PersonId;
                                person.Lastname = student.LastName;
                                person.Firstname = student.FirstName;
                                person.DOB = student.DateOfBirth;
                                person.Active = student.Active == "Y";
                                person.LastUpdated = now;
                                person.PhotoPath = student.ImageUrl;
                                people.Update(person, personId);
                                Logger.DebugFormat("person {0} updated.", personId);

                                altIds.Execute("Update PersonIDNo set Active=@0, LastUpdated=@1 where PersonID=@2",
                                    new object[] { false, DateTime.Now, personId });
                            }
                            else
                            {
                                var inserted = InsertPerson(people,
                                    new
                                    {
                                        student.PersonId,
                                        PersonTypeID = 1,
                                        SSN = student.StudentNumber,
                                        Lastname = student.LastName,
                                        Firstname = student.FirstName,
                                        PhotoPath = student.ImageUrl,
                                        DOB = student.DateOfBirth,
                                        Active = student.Active == "Y",
                                        LastUpdated = now,
                                        SchoolID = student.SchoolId
                                    }, conn);

                                personId = (int)inserted;

                                //Logger.DebugFormat("person {0} inserted.", personId);
                            }

                            exists.Grade = student.Grade;
                            exists.Homeroom = student.Homeroom;
                            exists.LastUpdated = now;

                            students.Update(exists, exists.StudentID);

                            //Logger.DebugFormat("student {0} updated.", student.StudentNumber);
                        }
                        else
                        {
                            Logger.DebugFormat("student {0} does not exist.", student.StudentNumber);

                            InsertPerson(people,
                                new
                                {
                                    student.PersonId,
                                    PersonTypeID = 1,
                                    SSN = student.StudentNumber,
                                    Lastname = student.LastName,
                                    Firstname = student.FirstName,
                                    PhotoPath = student.ImageUrl,
                                    DOB = student.DateOfBirth,
                                    Active = student.Active == "Y",
                                    LastUpdated = now,
                                    SchoolID = student.SchoolId
                                }, conn);

                            personId = student.PersonId;

                            InsertStudent(students,
                                new
                                {
                                    StudentId = student.Id,
                                    PersonID = personId,
                                    student.SchoolId,
                                    student.StudentNumber,
                                    student.Grade,
                                    student.Homeroom,
                                    LastUpdated = now,
                                    GUID = student.UniqueIdentifier
                                }, conn);


                        }

                        if (student.ActiveIds != null)
                        {
                            altIds.Execute("Update PersonIDNo set Active=@0, LastUpdated=@1 where PersonID=@2",
                                new object[] { false, DateTime.Now, personId });
                            //update alt ids
                            foreach (var id in student.ActiveIds)
                            {

                                //altIds.Execute("Update PersonIDNo set Active=@0, LastUpdated=@1 where PersonID=@2", new object[] { false, DateTime.Now, person.PersonID });

                                dynamic altId = altIds.SingleWhere("BarcodeId = @0 AND PersonID=@1",
                                    new object[] { id, personId });

                                if (altId != null)
                                {

                                    altIds.Execute(
                                        "Update PersonIDNo set Active=1, LastUpdated=getdate() where PersonID=@0 AND BarcodeID=@1",
                                        new object[] { personId, id });

                                }
                                else
                                {
                                    altIds.Execute(
                                        "Insert into PersonIDNo(SchoolID, PersonID, BarcodeID, Temp, Active,LastUpdated) VALUES(@0,@1,@2,0,1,getdate())",
                                        new object[] { student.SchoolId, personId, id });
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex.Message);
                        Logger.Error(ex);
                    }
                    finally
                    {

                    }
                }
                Logger.WarnFormat("Processed {0} student records.", data.Count());
            }
            return errors;
        }
        object InsertStudent(DynamicRepository repo, object o, DbConnection conn)
        {
            int result = 0;

            try
            {

                var cmd = repo.CreateInsertCommand(o);
                cmd.Connection = conn;
                result = cmd.ExecuteNonQuery();

            }
            catch (SqlException ex)
            {
                throw;
            }

            int outInt = 0;

            if (int.TryParse(result.ToString(), out outInt)) return outInt;

            return result;
        }

        object InsertPerson(DynamicRepository repo, object o, DbConnection conn)
        {


            dynamic result = 0;

            try
            {
               
                var cmd = repo.CreateInsertCommand(o);
                cmd.Connection = conn;
                result = cmd.ExecuteNonQuery();

            }
            catch (SqlException ex)
            {
                throw;
            }

            int outInt = 0;

            if (int.TryParse(result.ToString(), out outInt)) return outInt;

            return result;
        }

        public void SyncTimeTable(int schoolId)
        {
            if (!CheckInternet())
            {
                Logger.ErrorFormat("Network connection not detected, skipping TimeTable sync at {0}.", DateTime.Now);
                return;
            }

            var start = DateTime.Now;
            Logger.Warn("Syncing student time table.");
            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();

                    var trunc = CreateCommand(connection, "truncate table StudentTimeTable");
                    trunc.ExecuteNonQuery();

                    var get = CreateCommand(connection, "insert into studenttimetable exec DataReplicatorConnector.Swipek12.dbo.sp_GetStudentTimeTable '{0}'", schoolId);

                    get.ExecuteNonQuery();
                    
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);

                }
            }

            Logger.WarnFormat("Syncing student time table complete in {0} seconds.", (DateTime.Now - start).TotalSeconds);
        }

        public static SwipeK12.NextGen.ReadServices.CardItem[] MapFromSource(CardItem[] source)
        {
            var items = new List<SwipeK12.NextGen.ReadServices.CardItem>();
            foreach (var item in source)
            {
                items.Add(new SwipeK12.NextGen.ReadServices.CardItem()
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

        public void SyncStudentLunchTable(int schoolId)
        {
            if (!CheckInternet())
            {
                Logger.ErrorFormat("Network connection not detected, skipping StudentLunchTable sync at {0}.", DateTime.Now);
                return;
            }

            var start = DateTime.Now;
            Logger.Warn("Syncing student lunch table.");
            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();

                    var trunc = CreateCommand(connection, "truncate table [StudentLunchTable]");
                    trunc.ExecuteNonQuery();

                    var get = CreateCommand(connection, "insert into [StudentLunchTable] exec DataReplicatorConnector.Swipek12.dbo.sp_GetLunchTimeTable '{0}'", schoolId);

                    get.ExecuteNonQuery();

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);

                }
            }

            Logger.WarnFormat("Syncing student lunch table complete in {0} seconds.", (DateTime.Now - start).TotalSeconds);
        }

        public void SyncTardySwipe(int schoolId)
        {
            if (!CheckInternet())
            {
                Logger.ErrorFormat("Network connection not detected, skipping TardySwipe sync at {0}.", DateTime.Now);
                return;
            }

            var start = DateTime.Now;
            Logger.Warn("Syncing student tardy swipe.");
            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();

                    var trunc = CreateCommand(connection, "truncate table TardySwipe");
                    //trunc.ExecuteNonQuery();

                    var get = CreateCommand(connection, "truncate table TardySwipe;insert into TardySwipe exec DataReplicatorConnector.Swipek12.dbo.sp_GetTardySwipeBySchool '{0}'", schoolId);

                    get.ExecuteNonQuery();

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);

                }
            }

            Logger.WarnFormat("Syncing student tardy swipe table complete in {0} seconds.", (DateTime.Now - start).TotalSeconds);
        }

        public void SyncAlternateIds(int schoolid)
        {
            if (!CheckInternet())
            {
                Logger.ErrorFormat("Network connection not detected, skipping TardySwipe sync at {0}.", DateTime.Now);
                return;
            }

            var start = DateTime.Now;
            Logger.Warn("Syncing SyncAlternateIds");
            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();

                    var trunc = CreateCommand(connection, "truncate table personidno");
                    trunc.ExecuteNonQuery();

                    var get = CreateCommand(connection, "insert into personidno exec DataReplicatorConnector.Swipek12.dbo.QueryForAltIds {0}", schoolid);

                    get.ExecuteNonQuery();

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);

                }
            }

            Logger.WarnFormat("Syncing SyncAlternateIds complete in {0} seconds.", (DateTime.Now - start).TotalSeconds);
        }


        static DbCommand CreateCommand(DbConnection connection, string sql, params object[] parms)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandTimeout = 120;
            cmd.CommandText = string.Format(sql, parms);

            return cmd;
        }

        public async Task<int> Transactional(string masterIP, int school)
        {
            var asyncConnection = new SqlConnectionStringBuilder(ConnectionString) { AsynchronousProcessing = true }.ToString();

            if (!CheckInternet())
            {
                Logger.ErrorFormat("Network connection not detected, skipping Transaction sync at {0}.", DateTime.Now);
                return -1;
            }
            var timer = new Stopwatch();

            try
            {
                //timer.Start();
                using (var connection = new SqlConnection(asyncConnection))
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 60;
                    cmd.CommandText = string.Format("EXEC DataReplicator '{0}',0,'Transactional',{1}", masterIP, school);
                    Logger.WarnFormat("Running Transactional replication");
                    return await cmd.ExecuteNonQueryAsync();

                }
                //timer.Stop();
                //Logger.WarnFormat("Transactional replication complete in {0}.", timer.Elapsed.TotalSeconds);
                //return Observable.Return((long)1);
            }
            catch (Exception ex)
            {
                //return Observable.Return((long)0);
                Logger.Error("Transactional replication failed.", ex);
            }
            finally
            {
                timer.Reset();
            }

            return 0;
        }
        public int InitRemoteServer(string masterIP)
        {
            if (!CheckInternet())
            {
                Logger.ErrorFormat("Network connection not detected, skipping init at {0}.", DateTime.Now);
                return -1;
            }
           
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 120;
                    cmd.CommandText = string.Format("EXEC DataReplicatorConnector 'CREATE', '{0}'", masterIP);

                    return cmd.ExecuteNonQuery();
                }
               
            }
            catch (Exception ex)
            {
                //return Observable.Return((long)0);
                Logger.Error("remote server create failed.", ex);
            }
           
            return 0;
        }

        public void DestroyRemoteServer(string masterIP)
        {
            if (!CheckInternet())
            {
                Logger.ErrorFormat("Network connection not detected, skipping destroy at {0}.", DateTime.Now);
                return;
            }

            try
            {
               
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 120;
                    cmd.CommandText = string.Format("EXEC DataReplicatorConnector 'DESTROY', {0}", masterIP);

                    var rtn = cmd.ExecuteNonQuery();
                }
            
            }
            catch (Exception ex)
            {
                //return Observable.Return((long)0);
                Logger.Error("remote server destroy failed.", ex);
            }
            finally
            {
                
            }
        }
    }
}
