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
using System.Web.Script.Serialization;
using System.Windows.Controls;
using log4net;
using Massive;
using Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServiceStack.ServiceClient.Web;
using ServiceStack.Text;
using Swipe.Common.Models;
using Swipe.Domain;
using SwipeDesktop.Common;
using models = SwipeDesktop.Models;
using SwipeDesktop.ViewModels;
using SwipeDesktop.Views;
using SwipeK12; 
using SwipeK12.NextGen.Messaging;
using SwipeK12.NextGen.ReadServices;
using SwipeK12.NextGen.ReadServices.Messages;
using CardItem = SwipeK12.CardItem;
using JsonSerializer = ServiceStack.Text.JsonSerializer;

namespace SwipeDesktop.Api
{
    public class DataReplicator
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(DataReplicator));
        static readonly string ConnectionString = ConfigurationManager.ConnectionStrings["ScanStation"].ConnectionString;
        private static readonly string MasterIp = Settings.Default.SqlMasterIp;
        private static readonly int SchoolId = Settings.Default.SchoolId;
        static JsonServiceClient _client = new JsonServiceClient(Settings.Default.JsonUrl);

        static RemoteStorage _remoteStorage = new RemoteStorage();

        private static Stopwatch Timer = new Stopwatch();

        static int SyncTardyFailCount = 1;

        static bool CheckInternet()
        {
            string status;
            var connected = InternetAvailability.IsInternetAvailable(out status);

            return connected;
        }

        public static string Full()
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

                string url = string.Format("Students/Download?currentPage=0&schoolId={0}", SchoolId);


                var  getStudentsResponse = _client.Get<string>(url);
                var resp = ServiceStack.Text.JsonSerializer.DeserializeFromString<Body<BulkStudentsResponse>>(getStudentsResponse);

                errors.AddRange(ProcessStudentDownload(resp.data.Students));

                totalRecords = resp.data.TotalRecords;
                int totalPages = resp.data.TotalPages;

                if (totalPages > 1)
                {
                    for (int p = 0; p < totalPages; p++)
                    {
                        url = string.Format("Students/Download?CurrentPage={1}&schoolId={0}", SchoolId, p + 1);

                        var json = _client.Get<string>(url);

                        resp = ServiceStack.Text.JsonSerializer.DeserializeFromString<Body<BulkStudentsResponse>>(json);

                        errors.AddRange(ProcessStudentDownload(resp.data.Students));
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
                Logger.Error("Students replication failed.", ex);
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

        public static string TardySwipes()
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
              
                using (var conn = new DynamicRepository().OpenConnection())
                {
                    var trunc = CreateCommand(conn, "truncate table TardySwipeSync");
                    trunc.ExecuteNonQuery();
                }
                
                timer.Start();

                string url = string.Format("TardySwipe/Download?currentPage=0&schoolId={0}", SchoolId);

                var getStudentsResponse = _client.Get<string>(url);
                var resp = ServiceStack.Text.JsonSerializer.DeserializeFromString<Body<BulkTardySwipeResponse>>(getStudentsResponse);

                errors.AddRange(ProcessTardySwipeDownload(resp.data.Records));

                totalRecords = resp.data.TotalRecords;
                int totalPages = resp.data.TotalPages;

                if (totalPages > 1)
                {
                    for (int p = 0; p < totalPages; p++)
                    {
                        url = string.Format("TardySwipe/Download?CurrentPage={1}&schoolId={0}", SchoolId, p + 1);

                        var json = _client.Get<string>(url);

                        resp = ServiceStack.Text.JsonSerializer.DeserializeFromString<Body<BulkTardySwipeResponse>>(json);

                        errors.AddRange(ProcessTardySwipeDownload(resp.data.Records));
                    }
                }

                timer.Stop();
                Logger.WarnFormat("{0} TardySwipe replication complete in {1}.", totalRecords, timer.Elapsed.TotalSeconds);

                try
                {
                   
                    using (var conn = new DynamicRepository().OpenConnection())
                    {
                        var mergeCmd = CreateCommand(conn, "truncate table TardySwipe; exec MergeTardySwipeSync");
                        mergeCmd.ExecuteNonQuery();

                        //var mergeCmd = CreateCommand(conn, "exec MergeTardySwipeSync");
                    }
                }
                catch (SqlException ex)
                {
                    
                }

                return string.Format("{0} TardySwipe downloaded in {1} seconds.", totalRecords, timer.Elapsed.TotalSeconds);
                //return true;
            }
            catch (Exception ex)
            {
                //return false;
                Logger.Error("TardySwipe replication failed.", ex);
            }
            finally
            {
                timer.Reset();
            }

            if (errors.Any())
            {
                foreach (var s in errors)
                    Logger.Error(s);
            }
            return string.Empty;
        }


        public static string StudentStartTimes()
        {
            if (!CheckInternet())
            {
                Logger.ErrorFormat("Network connection not detected, skipping student start time sync at {0}.", DateTime.Now);
                return string.Empty;
            }

            var timer = new Stopwatch();
            List<string> errors = new List<string>();

            long totalRecords = 0;

            try
            {

                using (var conn = new DynamicRepository().OpenConnection())
                {
                    var trunc = CreateCommand(conn, "truncate table StudentStartTime_Sync");
                    trunc.ExecuteNonQuery();
                }

                timer.Start();

                string url = string.Format("{0}/LookupData/StudentStartTimes", SchoolId);

                var respJson = _client.Get<string>(url);
                var resp = ServiceStack.Text.JsonSerializer.DeserializeFromString<List<models.StudentStartTime>>(respJson);

                errors.AddRange(ProcessStudentStartTimeDownload(resp));

                timer.Stop();
                Logger.WarnFormat("{0} StudentStartTime replication complete in {1}.", totalRecords, timer.Elapsed.TotalSeconds);

                try
                {

                    using (var conn = new DynamicRepository().OpenConnection())
                    {
                        var mergeCmd = CreateCommand(conn, $"truncate table StudentStartTime; exec [MergeStudentStartTimeSync]");
                        mergeCmd.ExecuteNonQuery();

                        //var mergeCmd = CreateCommand(conn, "exec MergeTardySwipeSync");
                    }
                }
                catch (SqlException ex)
                {
                    Logger.Error("StudentStartTime error merge.", ex);
                }

                return string.Format("{0} StudentStartTime downloaded in {1} seconds.", totalRecords, timer.Elapsed.TotalSeconds);
                //return true;
            }
            catch (Exception ex)
            {
                //return false;
                Logger.Error("StudentStartTime replication failed.", ex);
            }
            finally
            {
                timer.Reset();
            }

            if (errors.Any())
            {
                foreach (var s in errors)
                    Logger.Error(s);
            }
            return string.Empty;
        }


        public static string StudentLunchTable()
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

                using (var conn = new DynamicRepository().OpenConnection())
                {
                    var trunc = CreateCommand(conn, "truncate table StudentLunchTable_Sync");
                    trunc.ExecuteNonQuery();
                }

                timer.Start();

                string url = string.Format("{0}/LunchTimeTable", SchoolId);

                var respJson = _client.Get<string>(url);
                var resp = ServiceStack.Text.JsonSerializer.DeserializeFromString<Body<List<models.LunchTimeTable>>>(respJson);

                errors.AddRange(ProcessLunchTableDownload(resp.data));

                timer.Stop();
                Logger.WarnFormat("{0} StudentLunchTable replication complete in {1}.", totalRecords, timer.Elapsed.TotalSeconds);

                try
                {

                    using (var conn = new DynamicRepository().OpenConnection())
                    {
                        var mergeCmd = CreateCommand(conn, $"truncate table StudentLunchTable; exec MergeStudentLunchTableSync '{Settings.Default.SchoolId}'");
                        mergeCmd.ExecuteNonQuery();

                        //var mergeCmd = CreateCommand(conn, "exec MergeTardySwipeSync");
                    }
                }
                catch (SqlException ex)
                {
                    Logger.Error("StudentLunchTable error merge.", ex);
                }

                return string.Format("{0} StudentLunchTable downloaded in {1} seconds.", totalRecords, timer.Elapsed.TotalSeconds);
                //return true;
            }
            catch (Exception ex)
            {
                //return false;
                Logger.Error("StudentLunchTable replication failed.", ex);
            }
            finally
            {
                timer.Reset();
            }

            if (errors.Any())
            {
                foreach (var s in errors)
                    Logger.Error(s);
            }
            return string.Empty;
        }

        public static string AltIds()
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

                string url = string.Format("Students/Download?currentPage=0&schoolId={0}", SchoolId);


                var getStudentsResponse = _client.Get<string>(url);
                var resp = ServiceStack.Text.JsonSerializer.DeserializeFromString<Body<BulkStudentsResponse>>(getStudentsResponse);

                ProcessAltIds(resp.data.Students);

                totalRecords = resp.data.TotalRecords;
                int totalPages = resp.data.TotalPages;

                if (totalPages > 1)
                {
                    for (int p = 0; p < totalPages; p++)
                    {
                        url = string.Format("Students/Download?currentPage={1}&schoolId={0}", SchoolId, p + 1);

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
                Logger.Error("Students replication failed.", ex);
            }
            finally
            {
                timer.Reset();
            }

            return string.Empty;
        }
        public static string VisitorSpeedPass()
        {
            if (!CheckInternet())
            {
                Logger.ErrorFormat("Network connection not detected, skipping VisitorSpeedPass sync at {0}.", DateTime.Now);
                return string.Empty;
            }

            long totalRecords = 0;
            try
            {
                Timer.Start();

                string url = $"{SchoolId}/Speedpass";
                

                var response = _client.Get<string>(url);

                var resp = JsonSerializer.DeserializeFromString<Response<List<VisitorSpeedPass>>>(response);

                ProcessVisitorSpeedpass(resp.data.ToArray());

                Timer.Stop();
                Logger.WarnFormat("{0} VisitorSpeedPass replication complete in {1}.", totalRecords, Timer.Elapsed.TotalSeconds);

                return string.Format("{0} VisitorSpeedPass downloaded in {1} seconds.", totalRecords, Timer.Elapsed.TotalSeconds);
                //return true;
            }
            catch (Exception ex)
            {
                //return false;
                Logger.Error("VisitorSpeedPass replication failed.", ex);
            }
            finally
            {
                Timer.Reset();
            }

            return string.Empty;
        }

        public static string CustomStartTimes()
        {
            if (!CheckInternet())
            {
                Logger.ErrorFormat("Network connection not detected, skipping CustomStartTimes sync at {0}.", DateTime.Now);
                return string.Empty;
            }

            long totalRecords = 0;
            try
            {
                Timer.Start();

                var data = _remoteStorage.GetSchoolStartTimes();

                ProcessStartTimes(data);

                Timer.Stop();
                Logger.WarnFormat("{0} CustomStartTimes replication complete in {1}.", totalRecords, Timer.Elapsed.TotalSeconds);

                return string.Format("{0} CustomStartTimes downloaded in {1} seconds.", totalRecords, Timer.Elapsed.TotalSeconds);
                //return true;
            }
            catch (Exception ex)
            {
                //return false;
                Logger.Error("CustomStartTimes replication failed.", ex);
            }
            finally
            {
                Timer.Reset();
            }

            return string.Empty;
        }

        public static string VisitorLog()
        {
            if (!CheckInternet())
            {
                Logger.ErrorFormat("Network connection not detected, skipping VisitorLog sync at {0}.", DateTime.Now);
                return string.Empty;
            }

            long totalRecords = 0;
            try
            {
                Timer.Start();

                string url = $"{SchoolId}/VisitorData";


                var response = _client.Get<string>(url);

               var resp = JsonSerializer.DeserializeFromString<VisitorDataResponse>(response);

                ProcessVisitLog(resp.VisitorData.ToArray());


                Timer.Stop();
                Logger.WarnFormat("{0} VisitorLog replication complete in {1}.", totalRecords, Timer.Elapsed.TotalSeconds);

                return string.Format("{0} VisitorLog downloaded in {1} seconds.", totalRecords, Timer.Elapsed.TotalSeconds);
                //return true;
            }
            catch (Exception ex)
            {
                //return false;
                Logger.Error("VisitorLog replication failed.", ex);
            }
            finally
            {
                Timer.Reset();
            }

            return string.Empty;
        }
        public static string RemoteSnapshot()
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
                    cmd.CommandText = string.Format("EXEC DataReplicator '{0}',0,'Snapshot',{1}", MasterIp, Settings.Default.SchoolId);

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

        public static string SnapshotSince(int minutes)
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
                var started = DateTime.Now;

                timer.Start();

                Logger.WarnFormat("replicating students changed since {0}.", DateTime.Now.AddMinutes(-minutes));

                string url = string.Format("Students/Download?schoolId={0}&ChangedSince={1}", SchoolId, minutes);

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

                    var people = new DynamicRepository("Person", "PersonID");

                    people.Execute("Update Person set Active=0 where persontypeid = 1 AND LastUpdated<@0 and Active = 1",
                                 new object[] { started  });

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

        public static string StationAlerts(bool clearall)
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
                    var resp = _client.Get<string>(string.Format("{0}/ActiveStationAlerts", Settings.Default.SchoolId));

                    var wrapper = resp.FromJson<Response<List<StationAlert>>>();
                    alerts = wrapper.data;

                    ProcessStationAlertsDownload(alerts, clearall);
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

        public static string InOutRooms()
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
                        var resp = client.Get<string>(string.Format("{0}/ScanLocations", Settings.Default.SchoolId));

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

        public static string Groups()
        {
            if (!CheckInternet())
            {
                Logger.ErrorFormat("Network connection not detected, skipping Group sync at {0}.", DateTime.Now);
                return string.Empty;
            }

            var timer = new Stopwatch();

            try
            {
                timer.Start();

                IEnumerable<Group> groups = new List<Group>();
                using (var client = _client)
                {
                    try
                    {
                   
                        var resp = client.Get<string>(string.Format("{0}/Groups", Settings.Default.SchoolId));

                        var wrapper = resp.FromJson<Response<List<Group>>>();
                        groups = wrapper.data;//.Where(x => x.Type == types);

                        ProcessGroupsDownload(groups);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("There was a problem retrieving Groups.", ex);
                    }

                }
                //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));

                timer.Stop();
                Logger.WarnFormat("Groups replication complete in {0}.", timer.Elapsed.TotalSeconds);


                return string.Format("{0} groups downloaded in {1} seconds.", groups.Count(), timer.Elapsed.TotalSeconds);
                //return true;
            }
            catch (Exception ex)
            {
                //return false;
                Logger.Error("Groups replication failed.", ex);
            }
            finally
            {
                timer.Reset();
            }
            return string.Empty;
        }

        public static string Consequences()
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
                        var resp = client.Get<string>(string.Format("{0}/CorrectiveActions", Settings.Default.SchoolId));

                        var wrapper = resp.FromJson<Response<List<JsonObject>>>();
                        var data = wrapper.data;//.Where(x => x.Type == types);

                        ProcessConsequenceDownload(data);
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

        static void ProcessConsequenceDownload(IEnumerable<JsonObject> data)
        {

            var now = DateTime.Now;

            var errors = new List<string>();
            var db = new DynamicRepository("CorrectiveActions", "Id");

            using (var conn = db.OpenConnection())
            {
                var trunc = CreateCommand(conn, "Update CorrectiveActions set Active=0 where SchoolId = " + Settings.Default.SchoolId);
                trunc.ExecuteNonQuery();

                foreach (var item in data)
                {
                   
                    try
                    {

                        dynamic exists = db.SingleWhere("Name = @0 AND SchoolId = @1", new object[] { item["Name"], Settings.Default.SchoolId });

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
                                Operator = item["Operator"],
                                ServeByDays = item["ServeByDays"]
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

        static void ProcessStationAlertsDownload(IEnumerable<StationAlert> data, bool clearAll)
        {

            var now = DateTime.Now;

            var errors = new List<string>();
            var table = new DynamicRepository("StationAlerts", "AlertId");

            //var ids = new List<int>();

            using (var conn = table.OpenConnection())
            {
                if (clearAll)
                {
                    table.Delete(where: string.Format("SchoolId = {0}", Settings.Default.SchoolId));
                }
                foreach (StationAlert item in data)
                {

                    try
                    {
                       
                        dynamic exists = table.SingleWhere("CorrelationId = @0 AND SchoolId = @1", new object[] { item.CorrelationId, Settings.Default.SchoolId });

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

                /*
                var sql = "Update InOutRooms set Active=0 where SchoolId = " + Settings.Default.SchoolId + " AND LastUpdated < '" + now.ToString("MM/dd/yyyy HH:mm:ss") + "'";

                var trunc = CreateCommand(conn, sql);
                trunc.ExecuteNonQuery();
                 * */

            }
            //return errors;
        }

        static void ProcessInOutRoomsDownload(IEnumerable<ScanLocation> data)
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
                            new object[] { item.RoomName, Settings.Default.SchoolId });

                        int personId = 0;

                        if (exists != null)
                        {

                            exists.Active = 1;
                            exists.location_type = item.Type;
                            exists.LastUpdated = now;
                            exists.allow_multiple_scans = item.AllowMultipleScans;
                            rooms.Update(exists, exists.IORId);

                        }
                        else
                        {
                            Logger.DebugFormat("room {0} does not exist.", item.RoomName);

                            rooms.Insert(new {IORId = item.Id, item.SchoolId, item.RoomName, Active=1, LastUpdated= DateTime.Now, location_type = (int)item.Type, allow_multiple_scans = item.AllowMultipleScans });
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

                var sql = "Update InOutRooms set Active=0 where SchoolId = " + Settings.Default.SchoolId + " AND LastUpdated < '" + now.ToString("MM/dd/yyyy HH:mm:ss") + "'";

                var trunc = CreateCommand(conn, sql);
                trunc.ExecuteNonQuery();

            }
            //return errors;
        }

        static void ProcessStartTimes(IEnumerable<models.SchoolStartTime> data)
        {

            var now = DateTime.Now;

            var errors = new List<string>();
            var times = new DynamicRepository("SchoolStartTimes");


            using (var conn = times.OpenConnection())
            {
                var trunc = CreateCommand(conn, "truncate table SchoolStartTimes");
                trunc.ExecuteNonQuery();


                foreach (models.SchoolStartTime item in data)
                {

                    try
                    {

                        times.Insert(
                            new {
                                item.Grade,
                                item.SchoolId,
                                item.StartTime
                            });
                        
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

        static void ProcessGroupsDownload(IEnumerable<Group> data)
        {

            var now = DateTime.Now;

            var errors = new List<string>();
            var groups = new DynamicRepository("Groups", "GroupId");

            var members = new DynamicRepository("PersonGroups", "Id");

            using (var conn = groups.OpenConnection())
            {

                foreach (Group item in data)
                {

                    try
                    {

                        dynamic exists = groups.SingleWhere("GroupCode = @0 AND SchoolId = @1",
                            new object[] { item.GroupCode, Settings.Default.SchoolId });

                        var groupId = item.GroupId;
                        if (exists != null)
                        {
                            groupId = exists.GroupId;
                            groups.Update(new { item.GroupCode, item.GroupName, item.GroupType, item.IsPrivate, Lastupdated = now, Settings.Default.SchoolId }, exists.GroupId);
                           
                        }
                        else
                        {
                            Logger.DebugFormat("room {0} does not exist.", item.GroupCode);

                            groups.Insert(new {item.GroupCode, item.GroupId, item.GroupName, item.GroupType, item.IsPrivate, Lastupdated = now, Settings.Default.SchoolId});
                        }


                        foreach (PersonGroup member in item.members)
                        {
                            dynamic memberExists = members.SingleWhere("GroupId = @0 AND Personid = @1",
                                new object[] { groupId, member.PersonId});

                            if (memberExists == null)
                            {
                                members.Insert(new { Id = member.AssocId, groupId, member.PersonId, LastUpdated = now });
                            }
                            else
                            {
                                members.Update(new { Id = member.AssocId, LastUpdated = now }, memberExists.Id);
                            }
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


                var sql = "Delete from PersonGroups where LastUpdated < '" + now.ToString("MM/dd/yyyy HH:mm:ss") + "'";
                var trunc = CreateCommand(conn, sql);
                trunc.ExecuteNonQuery();

                sql = "Delete from Groups where SchoolId = " + Settings.Default.SchoolId + " AND LastUpdated < '" + now.ToString("MM/dd/yyyy HH:mm:ss") + "'";
                trunc = CreateCommand(conn, sql);
                trunc.ExecuteNonQuery();

            }
            //return errors;
        }

        static IEnumerable<string> ProcessAltIds(IEnumerable<StudentModel> data)
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

        static void InsertImage(Byte[] image, string imageType, Guid id, string conn)
        {
            //Guid Id = Guid.Parse(id);
            //byte[] bytes = Encoding.ASCII.GetBytes(image);
            using (var connection = new SqlConnection(conn))
            {
                //if(conn.State != ConnectionState.Open)
                //    conn.Open();

                using (SqlCommand cmd = new SqlCommand("UPDATE Speedpass SET Image = @image where pk_Speedpass = @id", connection))
                {
                    cmd.Connection = connection;
                    cmd.Parameters.Add("@image", SqlDbType.VarBinary, image.Length).Value = image;
                    //cmd.Parameters.Add("@imageType", SqlDbType.VarBinary, image.Length).Value = image;
                    cmd.Parameters.Add("@id", SqlDbType.UniqueIdentifier).Value = id;
                    connection.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }
        static IEnumerable<string> ProcessVisitLog(VisitLog[] data)
        {

            var errors = new List<string>();
            var log = new DynamicRepository("VisitorLog", "pk_VisitRecord");
            
            using (var conn = log.OpenConnection())
            {

                foreach (VisitLog visit in data)
                {
               
                    try
                    {
                        dynamic exists = log.SingleWhere("pk_VisitRecord = @0 AND School = @1", new object[] { visit.Id, visit.SchoolId });

                        if (exists == null)
                        {
                            log.Insert(new
                            {
                                pk_VisitRecord = visit.Id,
                                DateOfVisit = visit.VisitDate,
                                visit.PurposeForVisit,
                                School = visit.SchoolId,
                                visit.VisitorId,
                                visit.Source,
                                DateExited = visit.ExitDate,
                                visit.VisitNumber,
                                FirstName = visit.VisitorFirstName,
                                LastName = visit.VisitorLastName
                            });
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
                Logger.WarnFormat("Processed {0} visit log records.", data.Count());
            }
            return errors;
        }
        static IEnumerable<string> ProcessVisitorSpeedpass(VisitorSpeedPass[] data)
        {

            var errors = new List<string>();
            var table = new DynamicRepository("Speedpass", "pk_Speedpass");

            using (var conn = table.OpenConnection())
            {

                foreach (VisitorSpeedPass visit in data)
                {

                    try
                    {
                        dynamic exists = table.SingleWhere("pk_Speedpass = @0 AND SchoolId = @1", new object[] { visit.Id, visit.SchoolId });

                        if (exists == null)
                        {
                            table.Insert(new
                            {
                                pk_Speedpass = visit.Id,
                                visit.FirstName,
                                visit.LastName,
                                visit.Expiration,
                                visit.PassType,
                                visit.PassId,
                                visit.SchoolId,
                                //visit.Image
                                
                            });
                        }
                        else
                        {
                            table.Update(new
                            {
                                visit.FirstName,
                                visit.LastName,
                                visit.Expiration,
                                visit.PassType,
                                visit.PassId,
                                visit.SchoolId,
                                //visit.Image

                            }, visit.Id);
                        }

                        if(visit.Image != null)
                            InsertImage(visit.Image, visit.ImageType, visit.Id, conn.ConnectionString);
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
                Logger.WarnFormat("Processed {0} visit log records.", data.Count());
            }
            return errors;
        }
        static IEnumerable<string> ProcessStudentDownload(IEnumerable<StudentModel> data)
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

                        if (student.StudentNumber == "263698169")
                        {
                            var temp = student;
                        }
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

                                //UpdatePerson(people, person, conn, personId);

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

        static IEnumerable<string> ProcessTardySwipeDownload(IEnumerable<TardySwipeModel> data)
        {

            var asyncConnection = new SqlConnectionStringBuilder(ConnectionString) { AsynchronousProcessing = true }.ToString();

            var now = DateTime.Now;

            var errors = new List<string>();
            var ts = new DynamicRepository("TardySwipeSync", "TardySwipeId");

            foreach (TardySwipeModel record in data)
            {
                  
                try
                {
                    ts.Insert(record);
                }
                catch (Exception ex)
                {
                    errors.Add(ex.Message);
                    //Logger.Error(ex);
                }
                finally
                {

                }
            }
            Logger.WarnFormat("Processed {0} tardy swipe records.", data.Count());
            
            return errors;
        }

        static IEnumerable<string> ProcessStudentStartTimeDownload(IEnumerable<models.StudentStartTime> data)
        {

            var asyncConnection = new SqlConnectionStringBuilder(ConnectionString) { AsynchronousProcessing = true }.ToString();

            var now = DateTime.Now;

            var errors = new List<string>();
            var ts = new DynamicRepository("StudentStartTime_Sync", "Id");

            foreach (models.StudentStartTime record in data)
            {

                try
                {
                    ts.Insert(record);
                }
                catch (Exception ex)
                {
                    errors.Add(ex.Message);
                    //Logger.Error(ex);
                }
                finally
                {

                }
            }
            Logger.WarnFormat("Processed {0} StudentStartTime records.", data.Count());

            return errors;
        }

        static IEnumerable<string> ProcessLunchTableDownload(IEnumerable<models.LunchTimeTable> data)
        {

            var asyncConnection = new SqlConnectionStringBuilder(ConnectionString) { AsynchronousProcessing = true }.ToString();

            var now = DateTime.Now;

            var errors = new List<string>();
            var ts = new DynamicRepository("StudentLunchTable_Sync", "Id");

            foreach (models.LunchTimeTable record in data)
            {

                try
                {
                    ts.Insert(record);
                }
                catch (Exception ex)
                {
                    errors.Add(ex.Message);
                    //Logger.Error(ex);
                }
                finally
                {

                }
            }
            Logger.WarnFormat("Processed {0} StudentLunchTable records.", data.Count());

            return errors;
        }

        static object InsertStudent(DynamicRepository repo, object o, DbConnection conn)
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

        static object InsertPerson(DynamicRepository repo, object o, DbConnection conn)
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

        static object UpdatePerson(DynamicRepository repo, object o, DbConnection conn, object key)
        {


            dynamic result = 0;

            try
            {

                var cmd = repo.CreateUpdateCommand(o, key);
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

        public static int SyncTimeTable()
        {
            if (!CheckInternet())
            {
                Logger.ErrorFormat("Network connection not detected, skipping TimeTable sync at {0}.", DateTime.Now);
                return 0;
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

                    var get = CreateCommand(connection, "insert into studenttimetable exec DataReplicatorConnector.Swipek12.dbo.sp_GetStudentTimeTable '{0}'", Settings.Default.SchoolId);
                    get.CommandTimeout = 300;
                    get.ExecuteNonQuery();


                    Logger.InfoFormat("Syncing student time table complete in {0} seconds.", (DateTime.Now - start).TotalSeconds);

                    return 0;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    return 1;
                }
            }
        }

        static SwipeK12.NextGen.ReadServices.CardItem[] MapFromSource(CardItem[] source)
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

        public static void SyncIdCardTemplates()
        {
            var idcardsBll = new SwipeCardBLL();
            var cards = idcardsBll.GetIDCards();
            foreach (var o in cards)
            {
                if(o.SchoolID != Settings.Default.SchoolId)
                    continue;

                if (!o.Active)
                    continue;

                try
                {
                    if (o.UpdatedOn >= DateTime.Today || o.CreatedOn >= DateTime.Today)
                    {
                        var cmd = new IdCardEnvelope()
                        {
                            Active = o.Active,
                            BackBackground = o.BackBackground,
                            BackOpacity = o.BackOpacity,
                            BackPortrait = o.BackPortrait,
                            CardHeight = o.CardHeight,
                            CardName = o.CardName,
                            CardWidth = o.CardWidth,
                            DualSided = o.DualSided,
                            Fields = MapFromSource(Serialisation.DeserializeObject<List<CardItem>>(o.Fields).ToArray()),
                            FrontBackground = o.FrontBackground,
                            FrontOpacity = o.FrontOpacity,
                            FrontPortrait = o.FrontPortrait,
                            OtherCard = o.OtherCard,
                            SchoolId = o.SchoolID,
                            StudentCard = o.StudentCard,
                            TeacherCard = o.TeacherCard,
                            TempCard = o.TempCard

                        };

                        var resp = _client.Send(cmd);
                    }
                }
                catch (Exception ex)
                {
                    Logger.ErrorFormat("Could not sync ID Card Template {0}", o.CardName);
                    Logger.Error(ex);
                }
            }
        }
        
        public static void SyncTardySwipeDiff()
        {
            if (!CheckInternet())
            {
                Logger.ErrorFormat("Network connection not detected, skipping TardySwipe diff sync at {0}.", DateTime.Now);
                return;
            }

            var start = DateTime.Now;
            Logger.Warn("Syncing student tardy swipe diff.");
            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();

                    var trunc = CreateCommand(connection, "truncate table TardySwipe");
                    //trunc.ExecuteNonQuery();

                    var get = CreateCommand(connection, "insert into TardySwipe(SchoolId, StudentId, SwipeTime, Location, Period, AttendanceCode, source, LastUpdated) exec DataReplicatorConnector.Swipek12.dbo.sp_GetTardySwipeBySchoolSince '{0}', '{1}'", Settings.Default.SchoolId, Environment.MachineName);

                    get.ExecuteNonQuery();
                    SyncTardyFailCount = 0;

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);

                    /*
                    if (ex.Message.Contains("DataReplicatorConnector"))
                    {
                        SyncTardyFailCount++;
                        if (SyncTardyFailCount < 5)
                        {
                            InitRemoteServer();
                            SyncTardySwipeDiff();
                        }
                    }
                    */
                }
            }

            Logger.WarnFormat("Syncing student tardy swipe diff table complete in {0} seconds.", (DateTime.Now - start).TotalSeconds);
        }

        public static void SyncTardySwipe()
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

                    var get = CreateCommand(connection, "truncate table TardySwipe;insert into TardySwipe exec DataReplicatorConnector.Swipek12.dbo.sp_GetTardySwipeBySchool '{0}', '{1}'", Settings.Default.SchoolId, Environment.MachineName);

                    get.ExecuteNonQuery();

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);

                }
            }

            Logger.WarnFormat("Syncing student tardy swipe table complete in {0} seconds.", (DateTime.Now - start).TotalSeconds);
        }

        public static void SyncAlternateIds()
        {
            if (!CheckInternet())
            {
                Logger.ErrorFormat("Network connection not detected, skipping TardySwipe sync at {0}.", DateTime.Now);
                return;
            }

            var start = DateTime.Now;
            Logger.Warn("Syncing SyncAlternateIds.");
            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();

                    var trunc = CreateCommand(connection, "truncate table personidno");
                    trunc.ExecuteNonQuery();

                    var get = CreateCommand(connection, "insert into personidno exec DataReplicatorConnector.Swipek12.dbo.QueryForAltIds {0}", Settings.Default.SchoolId);

                    get.ExecuteNonQuery();

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);

                }
            }

            Logger.WarnFormat("Syncing student personidno table complete in {0} seconds.", (DateTime.Now - start).TotalSeconds);
        }


        public static dynamic SyncSchoolSettings()
        {
            if (!CheckInternet())
            {
                Logger.ErrorFormat("Network connection not detected, skipping SchoolSettings sync at {0}.", DateTime.Now);
                return null;
            }

            Logger.Warn("Syncing School settings.");
            LocalStorage.EnsureSchoolRecordExists(Settings.Default.SchoolId);
            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    DateTime? dayStart = null;
                    string zipCode = string.Empty;

                    connection.Open();

                    var get = CreateCommand(connection, "select * from DataReplicatorConnector.Swipek12.dbo.School Where SchoolId = {0}", Settings.Default.SchoolId);

                    var reader = get.ExecuteReader();

                    while (reader.Read())
                    {
                        Settings.Default.School = reader[1].ToString();;
                        zipCode = reader[7].ToString();
                        if (reader[10] != DBNull.Value)
                        {
                            dayStart = DateTime.Parse(reader[10].ToString());
                        }
                    }
                    reader.Close();

                    if (dayStart.HasValue)
                    {
                        var upd = CreateCommand(connection, "Update School set SchoolName = '{3}', DayStartTime = '{0}', Zip = '{1}' Where SchoolId = {2}", dayStart, zipCode, Settings.Default.SchoolId, Settings.Default.School);
                        upd.ExecuteNonQuery();
                    }

                    return new {Settings.Default.SchoolId, SchoolName = Settings.Default.School};
                }
                catch (Exception ex)
                {
                   
                    Logger.Error(ex);
                    Logger.Debug(ConnectionString);

                }

                return null;
            }

        }

        static DbCommand CreateCommand(DbConnection connection, string sql, params object[] parms)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandTimeout = 120;
            cmd.CommandText = string.Format(sql, parms);

            return cmd;
        }

        public static async Task<int> Transactional()
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
                timer.Start();
                using (var connection = new SqlConnection(asyncConnection))
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 60;
                    cmd.CommandText = string.Format("EXEC DataReplicator '{0}',0,'Transactional',{1}", MasterIp, SchoolId);
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
                Logger.WarnFormat("Syncing transactional complete in {0} seconds.", timer.Elapsed.TotalSeconds);
                timer.Reset();
            }

            return 0;
        }
        public static int InitRemoteServer()
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
                    cmd.CommandText = string.Format("EXEC DataReplicatorConnector 'CREATE', '{0}'", MasterIp);

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

        public static void DestroyRemoteServer()
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
                    cmd.CommandText = string.Format("EXEC DataReplicatorConnector 'DESTROY', {0}", MasterIp);

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
