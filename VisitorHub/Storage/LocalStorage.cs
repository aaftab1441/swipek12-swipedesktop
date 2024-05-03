using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Common;
using log4net;
using Messages;
using Microsoft.SqlServer.Server;
using Oak;
using ReactiveUI;
using ServiceStack;
using ServiceStack.Text;
using SwipeDesktop.Common;
using SwipeDesktop.Models;
using SwipeDesktop.ViewModels;
using SwipeK12;

namespace SwipeDesktop.Api
{
    public class LocalStorage : IReturnData
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LocalStorage));

        static readonly string ConnectionString = ConfigurationManager.ConnectionStrings["ScanStation"].ConnectionString;

        private int MaxRows = 10;

        public IObservable<Models.PersonModel[]> SearchStudents(string term)
        {
            var startTime = DateTime.Now;

            string sql = string.Empty;

            IDataReader reader;

            if (string.IsNullOrEmpty(term))
                return Observable.Return(new StudentModel[0]);

      
            if (!term.Contains(","))
            {
                term = term.Replace("'", "''") + "%";

                sql = string.Format(
                    "Select top {1} * from Students s INNER JOIN Person p on s.personid = p.personid where p.schoolid = {0} AND Active = 1 AND (LastName Like '{2}' OR StudentNumber Like '{2}') order by lastname, firstname",
                    Settings.Default.SchoolId, MaxRows, term);
            }
            else
            {

                var array = term.Replace("'", "''").Split(",".ToCharArray());
                var lastPos = array[0] + "%";
                var firstPos = array[1] + "%";


                sql = string.Format(
                    "Select top {1} * from Students s INNER JOIN Person p on s.personid = p.personid where p.schoolid = {0} AND Active = 1 AND LastName Like '{2}' and FirstName Like '{3}' order by lastname, firstname",
                    Settings.Default.SchoolId, MaxRows, lastPos, firstPos.Trim());
            }

            var students = new List<StudentModel>();
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandTimeout = 30;
                cmd.CommandText = sql;
                  

                reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    students.Add(new StudentModel()
                    {
                        UniqueId = Guid.Parse(reader["Guid"].ToString()),
                        IdNumber = reader["StudentNumber"].ToString(),
                        LastName = reader["LastName"].ToString(),
                        FirstName = reader["FirstName"].ToString(),
                        Grade = DBNull.Value == reader["Grade"] ? "N/A" : reader["Grade"].ToString(),
                        Homeroom = DBNull.Value == reader["Homeroom"] ? "N/A" : reader["Homeroom"].ToString(),
                        PhotoPath = reader["PhotoPath"].ToString(),
                        PersonId = int.Parse(reader["PersonId"].ToString()),
                        StudentId = reader.GetInt32(0)
                    });
                }
            }

            var queryTime = (DateTime.Now - startTime).TotalSeconds;

            Logger.WarnFormat("SearchStudents elapsed time: {0}", queryTime);

            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return Observable.Return(students.ToArray());
        }

        public async Task<Models.PersonModel[]> SearchStudentsAsync(string term)
        {
            var startTime = DateTime.Now;

            string sql = string.Empty;

            //IDataReader reader;

            if (string.IsNullOrEmpty(term))
                return new StudentModel[0];


            if (!term.Contains(","))
            {
                term = term.Replace("'", "''") + "%";

                if (Settings.Default.IncludeStaff)
                {
                    sql = string.Format(
                        "Select top {1} s.StudentId, s.GUID, s.Grade, s.Homeroom, p.* from Person p LEFT OUTER JOIN Students s on p.personid = s.personid where (p.schoolid = {0} AND Active = 1 AND ((LastName Like '{2}' OR SSN Like '{2}') OR s.personid in (select personid from personidno where barcodeid LIKE '{2}' AND schoolid = {0}))) order by lastname, firstname",
                        Settings.Default.SchoolId, MaxRows, term);
                }
                else
                {
                    sql = string.Format(
                       "Select top {1} s.StudentId, s.GUID, s.Grade, s.Homeroom, p.* from Person p LEFT OUTER JOIN Students s on p.personid = s.personid where p.persontypeid = 1 and p.schoolid = {0} AND Active = 1 AND ((LastName Like '{2}' OR SSN Like '{2}') OR s.personid in (select personid from personidno where barcodeid LIKE '{2}' AND schoolid = {0})) order by lastname, firstname",
                       Settings.Default.SchoolId, MaxRows, term);
                }
            }
            else
            {

                var array = term.Replace("'", "''").Split(",".ToCharArray());
                var lastPos = array[0] + "%";
                var firstPos = array[1] + "%";


                sql = string.Format(
                    "Select top {1}  s.StudentId, s.GUID, s.Grade, s.Homeroom, p.* from Person p LEFT OUTER JOIN Students s on p.personid = s.personid where p.schoolid = {0} AND Active = 1 AND LastName Like '{2}' and FirstName Like '{3}' order by lastname, firstname",
                    Settings.Default.SchoolId, MaxRows, lastPos, firstPos.Trim());
            }

            var asyncConnection = new SqlConnectionStringBuilder(ConnectionString) { AsynchronousProcessing = true }.ToString();

            var students = new List<PersonModel>();

            using (var connection = new SqlConnection(asyncConnection))
            {
                using (var cmd = new SqlCommand())
                {
                    cmd.Connection = connection;
                    cmd.CommandText = sql;
                    cmd.CommandType = CommandType.Text;
                    connection.Open();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        students = reader.Select(HelperExtensions.ViewModelBuilder).ToList();
                    }
                }
            }

            var queryTime = (DateTime.Now - startTime).TotalSeconds;

            Logger.WarnFormat("SearchStudentsAsync elapsed time: {0}", queryTime);

            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return students.ToArray();
        }

        public async Task<Tuple<PersonModel, Lane, bool, string>> ObservableOnBarcode(string barcode)
        {
            var task = SearchByBarcodeAsync(barcode);

            var student = await task;
            if (student != null)
                student.IsManualEntry = true;

            return new Tuple<PersonModel, Lane, bool, string>(student, Lane.Right, true, barcode);
        }

        public string UnexcusedTardyCodes()
        {
            string sql = string.Format("Select excusecode from excuse where schoolid = {0} and excused = 0 and TardyOnly = 1", Settings.Default.SchoolId);

            var values = new List<string>();

            IDataReader reader;

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandTimeout = 30;
                cmd.CommandText = sql;

                reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    values.Add(reader[0].ToString());
                }
            }

            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return string.Join(",", values);
        }

        public void ImportDefaultIdCardTemplates()
        {
            try
            {
                string sql = string.Format(@"Insert into idcards
                select {0} as schoolid, cardname, cardwidth, cardheight,studentcard,teachercard,othercard,tempcard,frontbackground,
                frontopacity,backbackground,BackOpacity,DualSided,FrontPortrait,BackPortrait,Fields,active,getdate() as updatedon, getdate() as createdon 
                from idcards where schoolid = -1 and cardname not in (select cardname from idcards where schoolid = {0})",
                    Settings.Default.SchoolId);

                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 30;
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not migrate default Id Card tempates to {0}", Settings.Default.School);
                Logger.Error(ex);
            }

        }

        public void DeactivateAlert(int id, bool deactivateNow = false)
        {
            try
            {
                
                string sql =
                    $"UPDATE StationAlerts set Active = 0 WHERE AlertId = {id} and Expires <= '{DateTime.Today}'";

                if(deactivateNow)
                    sql = $"UPDATE StationAlerts set Active = 0 WHERE AlertId = {id}";

                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 5;
                    cmd.CommandText = sql;

                    cmd.ExecuteNonQuery();
                }

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

        }

        public bool StudentWasDismissed(string studentNumber, string location = null)
        {

            string sql =
                string.Format("select count(*) as cnt from redisscans where ObjectType='SwipeDesktop.Models.Dismissal' AND dbo.shortdate(SwipeTime) = '{1}' AND StudentNumber = '{0}'", studentNumber, DateTime.Today);
           
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandTimeout = 30;
                cmd.CommandText = sql;


                IDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var cnt = reader.GetInt32(0);

                    if (cnt >0)
                        return reader.GetInt32(0)%2 != 0;

                }
            }

            return false;
        }
        public List<StationAlert> GetAlerts(int studentid)
        {
            var current = DateTime.Now;

            var values = new List<StationAlert>();

            try
            {
               
                string sql = string.Format("SELECT * from StationAlerts where studentid = {0} AND Active = 1", studentid);
                
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 30;
                    cmd.CommandText = sql;


                    IDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        var text = reader.GetString(1);
                        var expires = reader[2].ToString();

                        //if (values.FirstOrDefault(x => x.AlertText == text) == null)
                        //{
                            values.Add(new StationAlert()
                            {

                                AlertId = reader.GetInt32(0),
                                AlertText = text,
                                Expires = reader.IsDBNull(2) ? DateTime.MaxValue : DateTime.Parse(expires),
                                Active = reader.GetBoolean(3),
                                CorrelationId = reader.GetInt32(4),
                                AlertColor = reader.GetInt32(5),
                                //AlertSound = reader.GetString(6),
                                AlertSound = DBNull.Value == reader["AlertSound"] ? "general.wav" : reader.GetString(6),
                                SchoolId = reader.GetInt32(7),
                                StudentId = reader.GetInt32(8),
                                AlertType = reader.GetString(9)
                            });
                        //}
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            var elapsed = (current - DateTime.Now).TotalSeconds;

            Logger.WarnFormat("{0} seconds elapsed on get alerts", elapsed);

            return values.Where(x=>x.Expires >= DateTime.Today).ToList();
        }

        public async Task<PersonModel> SearchByBarcodeAsync(string barcode)
        {
            try
            {
                Logger.DebugFormat("Lookup student {0}", barcode);

                var asyncConnection =
                    new SqlConnectionStringBuilder(ConnectionString) {AsynchronousProcessing = true}.ToString();

                var sql = string.Format(
                    "Select top {1} s.StudentId, s.GUID, s.Grade, s.Homeroom, p.* from Person p LEFT OUTER JOIN Students s on p.personid = s.personid where p.schoolid = {0} AND Active = 1 AND SSN = '{2}' and PersonTypeId = 1 or s.personid in (select personid from personidno where barcodeid = '{2}' AND schoolid = {0}) ORDER BY Difference(SSN, '{2}') DESC",
                    Settings.Default.SchoolId, 1, barcode);

                if (Settings.Default.IncludeStaff)
                {
                    sql = string.Format(
                    "Select top {1} s.StudentId, s.GUID, s.Grade, s.Homeroom, p.* from Person p LEFT OUTER JOIN Students s on p.personid = s.personid where p.schoolid = {0} AND Active = 1 AND SSN = '{2}' or s.personid in (select personid from personidno where barcodeid = '{2}' AND schoolid = {0})",
                    Settings.Default.SchoolId, 1, barcode);
                }
                var start = DateTime.Now;
                using (var connection = new SqlConnection(asyncConnection))
                {
                    using (var cmd = new SqlCommand())
                    {
                        cmd.Connection = connection;
                        cmd.CommandText = sql;
                        cmd.CommandType = CommandType.Text;
                        connection.Open();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows)
                            {
                                LogInvalidScan(barcode);
                                return null;
                            }
                            reader.Read();
                            if (!reader.GetBoolean(20))
                            {
                                return null;
                            }
                            
                            var diff = DateTime.Now - start;

                            Logger.DebugFormat("Lookup student async in {0} ms", diff.TotalMilliseconds);

                            return HelperExtensions.ViewModelBuilder(reader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("Could not search for student {0}", barcode), ex);
                return null;
            }

        }

        public async Task<VisitLog> SearchForVisitAsync(string visitNumber)
        {
            try
            {
                Logger.DebugFormat("Lookup visit by number {0}", visitNumber);

                var asyncConnection =
                    new SqlConnectionStringBuilder(ConnectionString) { AsynchronousProcessing = true }.ToString();

                var sql = "Select top 1 * from VisitorLog where VisitNumber = @0 and School = @1 and dbo.shortdate(DateOfVisit) = dbo.shortdate(getdate())";
             
                var start = DateTime.Now;
                using (var connection = new SqlConnection(asyncConnection))
                {
                    using (var cmd = new SqlCommand())
                    {
                        cmd.Connection = connection;
                        cmd.CommandText = sql;
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.AddWithValue("@1", Settings.Default.SchoolId);
                        cmd.Parameters.AddWithValue("@0", visitNumber);
                        connection.Open();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows)
                            {
                                //LogInvalidScan(barcode);
                                return null;
                            }
                            reader.Read();
                          
                            var diff = DateTime.Now - start;

                            Logger.DebugFormat("Lookup visit async in {0} ms", diff.TotalMilliseconds);

                            return HelperExtensions.VisitBuilder(reader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("Could not search for visit {0}", visitNumber), ex);
                return null;
            }

        }

        public async Task RecordVisitExit(VisitExit exit)
        {
            try
            {
                Logger.DebugFormat("exit visit by id {0}", exit.VisitId);

                var asyncConnection =
                    new SqlConnectionStringBuilder(ConnectionString) { AsynchronousProcessing = true }.ToString();

                var sql = "Update VisitorLog set DateExited = @0 where pk_VisitRecord = @1";

                var start = DateTime.Now;
                using (var connection = new SqlConnection(asyncConnection))
                {
                    using (var cmd = new SqlCommand())
                    {
                        cmd.Connection = connection;
                        cmd.CommandText = sql;
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.AddWithValue("@1", exit.VisitId);
                        cmd.Parameters.AddWithValue("@0", exit.DateExited);
                        connection.Open();
                        
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("Could not update visit {0}", exit.VisitId), ex);
            }

        }


        public async Task<dynamic> SearchForSpeedpassAsync(string pass)
        {
            try
            {
                Logger.DebugFormat("Lookup speedpass {0}", pass);

                var asyncConnection =
                    new SqlConnectionStringBuilder(ConnectionString) { AsynchronousProcessing = true }.ToString();

                var sql = string.Format(
                    "Select top {1} * from SpeedPass WHERE SchoolId = '{0}' AND PassId = '{2}'",
                    Settings.Default.SchoolId, 1, pass);

                var start = DateTime.Now;
                using (var connection = new SqlConnection(asyncConnection))
                {
                    using (var cmd = new SqlCommand())
                    {
                        cmd.Connection = connection;
                        cmd.CommandText = sql;
                        cmd.CommandType = CommandType.Text;
                        connection.Open();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                          
                            reader.Read();
                          
                            var diff = DateTime.Now - start;

                            Logger.DebugFormat("Lookup visitor pass async in {0} ms", diff.TotalMilliseconds);

                            return reader.VisitorPassBuilder();// .VisitorPassBuilder(reader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("Could not search for visitor pass {0}, {1}", pass, ex.StackTrace);
                return null;
            }

        }

        public PersonModel SearchByBarcode(string barcode)
        {
            string sql = string.Empty;

            IDataReader reader;

            if (string.IsNullOrEmpty(barcode))
                return null;

            sql = string.Format(
                    "Select top {1} s.StudentId, s.GUID, s.Grade, s.Homeroom, p.* from Person p LEFT OUTER JOIN Students s on p.personid = s.personid where p.schoolid = {0} AND Active = 1 AND SSN = '{2}' or s.personid in (select personid from personidno where barcodeid = '{2}')", Settings.Default.SchoolId, 1, barcode);

            var start = DateTime.Now;

            //var students = new List<StudentViewModel>();
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandTimeout = 30;
                cmd.CommandText = sql;

                reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var diff = DateTime.Now - start;

                    Logger.DebugFormat("Lookup student in {0} ms", diff.TotalMilliseconds);

                    return HelperExtensions.ViewModelBuilder(reader);
                }
            }

          

            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return null;
        }

        public string CheckRoomLocation(int student, string period)
        {

            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 30;
                    cmd.CommandText =
                        string.Format("select RoomName from StudentTimeTable where studentid={0} and Period = '{1}'", student, period);

                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        if(!string.IsNullOrEmpty(reader.GetString(0)))
                            return reader.GetString(0);
                    }

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);

                }
            }

            return "N/A";
        }

        public string CheckRoomLocation(int student, string period, string day)
        {

            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 30;
                    cmd.CommandText =
                        string.Format("select RoomName from StudentTimeTable where studentid={0} and Period = '{1}' and Day = '{2}'", student, period, day);

                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        if (!string.IsNullOrEmpty(reader.GetString(0)))
                            return reader.GetString(0);
                    }

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);

                }
            }

            return "N/A";
        }

        public bool CheckLunchLocation(int student, string period)
        {

            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 30;
                    cmd.CommandText =
                        string.Format("select id from StudentLunchTable where studentid={0} and LunchKey = '{1}'", student, period);

                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                       
                        return true;                          
                    }

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);

                }
            }

            return false;
        }

        public Tuple<string, string> GetLunchLocation(int student, string lunchKey)
        {

            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 30;
                    cmd.CommandText =
                        string.Format("select period, day from StudentLunchTable where LunchKey = '{0}'", lunchKey);

                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {

                        return new Tuple<string,string>(reader[0].ToString(), reader[1].ToString());
                    }

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);

                }
            }

            return null;
        }

        public bool CheckGroupLocation(int student, string group)
        {

            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 30;
                    cmd.CommandText =
                        string.Format(@"select Groups.*, PersonGroups.PersonId, students.studentid from Groups
                            inner join PersonGroups on Groups.GroupId = PersonGroups.GroupId
                            inner join Students on PersonGroups.Personid = students.personid
                            where studentid ={0} and GroupName = '{1}'", student, group);

                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {

                        return true;
                    }

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);

                }
            }

            return false;
        }

        public bool CheckStudentPerson(string id)
        {

            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 30;
                    cmd.CommandText =
                        string.Format("select personid from Person where SSN='{0}' and persontypeid = 1", id);

                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {

                        return true;
                    }

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);

                }
            }

            return false;
        }

        public bool CheckStaffPerson(string id)
        {

            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 30;
                    cmd.CommandText =
                        string.Format("select personid from Person where SSN='{0}' and persontypeid = 8", id);

                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        return true;
                    }

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);

                }
            }

            return false;
        }

        public void LogInvalidScan(string barcode)
        {
            var asyncConnection = new SqlConnectionStringBuilder(ConnectionString) { AsynchronousProcessing = true }.ToString();

            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 10;
                    cmd.CommandText = "INSERT INTO InvalidScans(SchoolId,ReadScan,SwipeTime) VALUES(@school, @barcode, @datescanned)";
                    cmd.Parameters.AddWithValue("@school", Settings.Default.SchoolId);
                    cmd.Parameters.AddWithValue("@barcode", barcode);
                    cmd.Parameters.AddWithValue("@datescanned", DateTime.Now);

                    var rslt = cmd.ExecuteNonQuery();
               

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);

                }
            }

        }


        public void RecordSyncAudit()
        {
            var asyncConnection = new SqlConnectionStringBuilder(ConnectionString) { AsynchronousProcessing = true }.ToString();

            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 10;
                    cmd.CommandText = "INSERT INTO AuditTable(AuditTime,Type) VALUES(@audittime,@type)";
                    //cmd.Parameters.AddWithValue("@school", Settings.Default.SchoolId);
                    cmd.Parameters.AddWithValue("@type", "ManualSync");
                    cmd.Parameters.AddWithValue("@audittime", DateTime.Now);

                    var rslt = cmd.ExecuteNonQuery();


                }
                catch (Exception ex)
                {
                    Logger.Error(ex);

                }
            }

        }

        public void RecordIdCardSyncAudit()
        {
            var asyncConnection = new SqlConnectionStringBuilder(ConnectionString) { AsynchronousProcessing = true }.ToString();

            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 10;
                    cmd.CommandText = "INSERT INTO AuditTable(AuditTime,Type) VALUES(@audittime,@type)";
                    //cmd.Parameters.AddWithValue("@school", Settings.Default.SchoolId);
                    cmd.Parameters.AddWithValue("@type", "IDCardSync");
                    cmd.Parameters.AddWithValue("@audittime", DateTime.Now);

                    var rslt = cmd.ExecuteNonQuery();


                }
                catch (Exception ex)
                {
                    Logger.Error(ex);

                }
            }

        }

        public Tuple<DateTime, string> LastManualSync()
        {

            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 30;
                    cmd.CommandText =
                        string.Format("select top 1 AuditTime, Data from AuditTable where Type = 'ManualSync' order by AuditTime DESC");

                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {

                        return new Tuple<DateTime, String>(reader.GetDateTime(0), reader[1].ToString());
                    }

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);

                }
            }

            return null;
        }

        public dynamic InsertStudent(PersonModel person, StudentDetails student)
        {
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {

                    connection.Open();
                    var personPK = connection.CreateCommand();
                    personPK.CommandText = "GetNextIndexOnPerson";
                    personPK.CommandType = CommandType.StoredProcedure;
                    var personId = personPK.ExecuteScalar();
                    
                    var studentPK = connection.CreateCommand();
                    studentPK.CommandText = "GetNextIndexOnStudent";
                    studentPK.CommandType = CommandType.StoredProcedure;
                    var studentId = studentPK.ExecuteScalar();

                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 10;
                    cmd.CommandText = "InsertStudent";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@SchoolId", Settings.Default.SchoolId);
                    cmd.Parameters.AddWithValue("@PersonId", personId);
                    cmd.Parameters.AddWithValue("@StudentId", studentId);
                    cmd.Parameters.AddWithValue("@IdNumber", person.IdNumber);
                    cmd.Parameters.AddWithValue("@FirstName", person.FirstName);
                    cmd.Parameters.AddWithValue("@LastName", person.LastName);
                    cmd.Parameters.AddWithValue("@DateOfBirth", person.DateOfBirth);
                    cmd.Parameters.AddWithValue("@Bus", student.Bus);
                    cmd.Parameters.AddWithValue("@Grade", student.Grade);
                    cmd.Parameters.AddWithValue("@Homeroom", student.Homeroom);

                    var rslt = cmd.ExecuteReader();

                    while (rslt.Read())
                    {
                        return new {personId = rslt.GetInt32(0), studentId = rslt.GetInt32(1)};
                    }

                    //return new Tuple<bool, string>(true, null);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw ex;
                //return new Tuple<bool, string>(false, ex.Message);
            }

            return null;
        }
        public void InsertStudent(PersonModel person, StudentDetails student, int personId, int studentId)
        {
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {

                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 10;
                    cmd.CommandText = "InsertStudent";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@SchoolId", Settings.Default.SchoolId);
                    cmd.Parameters.AddWithValue("@PersonId", personId);
                    cmd.Parameters.AddWithValue("@StudentId", studentId);
                    cmd.Parameters.AddWithValue("@IdNumber", person.IdNumber);
                    cmd.Parameters.AddWithValue("@FirstName", person.FirstName);
                    cmd.Parameters.AddWithValue("@LastName", person.LastName);
                    cmd.Parameters.AddWithValue("@DateOfBirth", person.DateOfBirth);
                    cmd.Parameters.AddWithValue("@Bus", student.Bus);
                    cmd.Parameters.AddWithValue("@Grade", student.Grade);
                    cmd.Parameters.AddWithValue("@Homeroom", student.Homeroom);

                    var rslt = cmd.ExecuteNonQuery();

                    //return new Tuple<bool, string>(true, null);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw ex;
                //return new Tuple<bool, string>(false, ex.Message);
            }
        }

        public void InsertStaff(PersonModel person, StaffDetails staff, int personId)
        {
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {

                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 10;
                    cmd.CommandText = "InsertStaff";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@SchoolId", Settings.Default.SchoolId);
                    cmd.Parameters.AddWithValue("@PersonId", personId);
                    cmd.Parameters.AddWithValue("@IdNumber", person.IdNumber);
                    cmd.Parameters.AddWithValue("@FirstName", person.FirstName);
                    cmd.Parameters.AddWithValue("@LastName", person.LastName);
                    cmd.Parameters.AddWithValue("@DateOfBirth", person.DateOfBirth);
                    cmd.Parameters.AddWithValue("@JobTitle", staff.JobTitle);
                    cmd.Parameters.AddWithValue("@OfficeLocation", staff.OfficeLocation);

                    var rslt = cmd.ExecuteNonQuery();

                    //return new Tuple<bool, string>(true, null);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw ex;
            }
        }

        public dynamic InsertStaff(PersonModel person, StaffDetails staff)
        {
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {

                    connection.Open();
                    var personPK = connection.CreateCommand();
                    personPK.CommandText = "GetNextIndexOnPerson";
                    personPK.CommandType = CommandType.StoredProcedure;
                    var personId = personPK.ExecuteScalar();

                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 10;
                    cmd.CommandText = "InsertStaff";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@SchoolId", Settings.Default.SchoolId);
                    cmd.Parameters.AddWithValue("@PersonId", personId);
                    cmd.Parameters.AddWithValue("@IdNumber", person.IdNumber);
                    cmd.Parameters.AddWithValue("@FirstName", person.FirstName);
                    cmd.Parameters.AddWithValue("@LastName", person.LastName);
                    cmd.Parameters.AddWithValue("@DateOfBirth", person.DateOfBirth);
                    cmd.Parameters.AddWithValue("@JobTitle", staff.JobTitle);
                    cmd.Parameters.AddWithValue("@OfficeLocation", staff.OfficeLocation);


                    var rslt = cmd.ExecuteReader();

                    while (rslt.Read())
                    {
                        return new { personId = rslt.GetInt32(0) };
                    }

                    //return new Tuple<bool, string>(true, null);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw ex;
            }

            return null;
        }

        public IEnumerable<Tuple<string, int>> DatabaseStats()
        {
            var stats = new List<Tuple<string, int>>();
            int rtnVal = 0;
            SqlDataReader reader;
            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 10;
                    cmd.CommandText =
                        string.Format(
                            @"select count(*) from Students s inner join person p on s.personid = p.personid where p.Schoolid = {0} and p.Active=1",
                            Settings.Default.SchoolId);

                    reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {

                        rtnVal = reader.GetInt32(0);

                    }

                    reader.Close();
                    stats.Add(new Tuple<string, int>("Total Active Students", rtnVal));

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);

                }
                
                try
                {
                  
                    var cmd2 = connection.CreateCommand();
                    cmd2.CommandTimeout = 10;
                    cmd2.CommandText = string.Format(@"select count(*) from CorrectiveActions where Schoolid = {0} and Active=1", Settings.Default.SchoolId);

                    reader = cmd2.ExecuteReader();

                    rtnVal = 0;
                    while (reader.Read())
                    {

                        rtnVal = reader.GetInt32(0);

                    }

                    stats.Add(new Tuple<string, int>("Total Active Consequences", rtnVal));
                    reader.Close();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);

                }


                try
                {

                    var cmd2 = connection.CreateCommand();
                    cmd2.CommandTimeout = 10;
                    cmd2.CommandText = string.Format(@"select count(*) from studenttimetable stt inner join students s on stt.studentid = s.studentid where s.Schoolid = {0}", Settings.Default.SchoolId);

                    reader = cmd2.ExecuteReader();

                    rtnVal = 0;
                    while (reader.Read())
                    {

                        rtnVal = reader.GetInt32(0);

                    }

                    stats.Add(new Tuple<string, int>("Total Time Table Records", rtnVal));
                    reader.Close();
                }
                catch (Exception ex)
                {
                    stats.Add(new Tuple<string, int>("Total Time Table Records", -1));
                    Logger.Error(ex);

                }

                try
                {

                    var cmd2 = connection.CreateCommand();
                    cmd2.CommandTimeout = 10;
                    cmd2.CommandText = string.Format(@"select count(*) from stationalerts where Schoolid = {0} and Active=1", Settings.Default.SchoolId);

                    reader = cmd2.ExecuteReader();

                    rtnVal = 0;
                    while (reader.Read())
                    {

                        rtnVal = reader.GetInt32(0);

                    }

                    stats.Add(new Tuple<string, int>("Total Active Alerts", rtnVal));
                    reader.Close();
                }
                catch (Exception ex)
                {
                    stats.Add(new Tuple<string, int>("Total Active Alerts", -1));
                    Logger.Error(ex);

                }

                try
                {

                    var cmd2 = connection.CreateCommand();
                    cmd2.CommandTimeout = 10;
                    cmd2.CommandText = string.Format(@"select count(*) from studentlunchtable stt
                            inner join students s  on stt.studentid = s.studentid
                            where stt.SchoolID = {0}
                            ", Settings.Default.SchoolId);

                    reader = cmd2.ExecuteReader();

                    rtnVal = 0;
                    while (reader.Read())
                    {

                        rtnVal = reader.GetInt32(0);

                    }

                    stats.Add(new Tuple<string, int>("Total Lunch Records", rtnVal));
                    reader.Close();
                }
                catch (Exception ex)
                {
                    stats.Add(new Tuple<string, int>("Total Lunch Records", -1));
                    Logger.Error(ex);

                }
            }

            return stats;
        }

        /// <summary>
        //  Get MTD and YTD Tardy stats
        /// </summary>
        /// <param name="student"></param>
        /// <param name="period"></param>
        /// <returns>array of stats - pos 0 = total count, pos 1 = period count, pos 2 = since count</returns>
        public IEnumerable<TardyStat> CheckTardyStats(int student, string period, DateTime since)
        {
            DateTime startYr = since.Date;
            DateTime endYr = startYr.AddDays(365);

            if (DateTime.Today.Month > 7)
            {
                startYr = DateTime.Parse(string.Format("8/1/{0}", DateTime.Today.Year));
                endYr = DateTime.Parse(string.Format("7/31/{0}", DateTime.Today.Year + 1));
            }
            else
            {
                startYr = DateTime.Parse(string.Format("8/1/{0}", DateTime.Today.Year-1));
                endYr = DateTime.Parse(string.Format("7/31/{0}", DateTime.Today.Year));
            }

            var stats = new List<TardyStat>(new[] { new TardyStat() { Description = "Unexcused Tardy" }, new TardyStat() { Description = string.Format("Unexcused Period {0}", period) }});//, new TardyStat() { Description = string.Format("Since {0}", since.ToString("d")) } });

            DateTime startMonth = DateTime.Today.EndOfLastMonth().AddDays(1);

            SqlCommand cmd = null;
            SqlDataReader reader = null;

            using (var connection = new SqlConnection(ConnectionString))
            {


                try
                {
                    connection.Open();
                    cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 10;
                    cmd.CommandText = string.Format(
                        "Select top 1 StartDate from CorrectiveActions WHERE Schoolid = {0} and active=1 order by startdate", Settings.Default.SchoolId);

                    reader = cmd.ExecuteReader();
                    reader.Read();
                    startMonth = reader.GetDateTime(0);

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
                finally
                {
                    if(reader != null)
                        reader.Close();
                }
                try
                {
                    // dateadd(month,datediff(month,0,getdate()),0) and dateadd(mi,-1,dateadd(month,datediff(month,-1,getdate()),0))
                   
                    cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 10;
                    cmd.CommandText = string.Format(@"select count(*) from (select period, dbo.shortdate(swipetime) as swipedate from tardyswipe 
                        inner join Excuse on Excuse.ExcuseCode = tardyswipe.AttendanceCode AND Excuse.SchoolID = tardyswipe.SchoolID
                        where swipetime between '{3}' and '{4}'                     
                        and studentid = {2} and Excuse.excused = 0) as qry1
                        union all
                        select count(*) from (select period, dbo.shortdate(swipetime) as swipedate from tardyswipe 
                        inner join Excuse on Excuse.ExcuseCode = tardyswipe.AttendanceCode AND Excuse.SchoolID = tardyswipe.SchoolID
                        where swipetime between '{0}' and '{1}'
                        and studentid = {2} and Excuse.excused = 0) as qry2", startYr, endYr, student, startMonth, DateTime.Today.AddDays(1));

                    reader = cmd.ExecuteReader();
                    
                    var values = new List<int>();
                    while (reader.Read())
                    {
                        
                        values.Add(reader.GetInt32(0));

                    }

                    stats[0].MonthToDate = values[0];
                    stats[0].YearToDate = values[1];

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);

                }
                finally
                {
                    if (reader != null)
                        reader.Close();
                }
                try
                {
                   
                    cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 10;
                    cmd.CommandText = string.Format(@"select count(*) from (select period, dbo.shortdate(swipetime) as swipedate from tardyswipe 
                        inner join Excuse on Excuse.ExcuseCode = tardyswipe.AttendanceCode AND Excuse.SchoolID = tardyswipe.SchoolID
                        where swipetime between '{4}' and '{5}'
                        and studentid = {2} and period='{3}' and Excuse.excused = 0) as qry1
                        union all
                        select count(*) from (select period, dbo.shortdate(swipetime) as swipedate from tardyswipe 
                        inner join Excuse on Excuse.ExcuseCode = tardyswipe.AttendanceCode AND Excuse.SchoolID = tardyswipe.SchoolID
                        where swipetime between
                        '{0}' and '{1}'
                        and studentid = {2} and period='{3}' and Excuse.excused = 0) as qry2", startYr, endYr, student, period, startMonth, DateTime.Today.AddDays(1));

                    reader = cmd.ExecuteReader();
                    var values = new List<int>();
                    while (reader.Read())
                    {

                        values.Add(reader.GetInt32(0));

                    }

                    if(values.Any())
                        stats[1].MonthToDate = values[0];

                    if (values.Count() > 1)
                        stats[1].YearToDate = values[1];

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);

                }
                finally
                {
                    if (reader != null)
                        reader.Close();
                }
            }

            /*
            if (since != DateTime.MinValue)
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    try
                    {
                        connection.Open();
                        var cmd = connection.CreateCommand();
                        cmd.CommandTimeout = 10;
                        cmd.CommandText = string.Format(@"select count(*) from (select period, dbo.shortdate(swipetime) as swipedate from tardyswipe where swipetime between
                        '{0}' and dateadd(mi,-1,dateadd(month,datediff(month,-1,getdate()),0))
                        and studentid = {1}) as qry1
                        union all
                        select count(*) from (select period, dbo.shortdate(swipetime) as swipedate from tardyswipe where swipetime between
                        '{0}' and '{2}'
                        and studentid = {1}) as qry2", since, student, endYr);

                        var reader = cmd.ExecuteReader();

                        List<int> values = new List<int>();
                        while (reader.Read())
                        {

                            values.Add(reader.GetInt32(0));

                        }

                        stats[2].MonthToDate = values[0];
                        stats[2].YearToDate = values[1];

                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);

                    }
                }
            }
            */
            return stats;
        }

        public Tuple<int, DateTime, string> CheckTardySwipe(int student, string period)
        {

            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 30;
                    cmd.CommandText =
                        string.Format("select TardySwipeId, SwipeTime, Location from TardySwipe inner join inoutrooms on TardySwipe.Location = inoutrooms.RoomName where studentid={0} and dbo.shortdate(SwipeTime) = '{1}' and Period = '{2}' and allow_multiple_scans = 0", student, DateTime.Today, period);

                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        return new Tuple<int, DateTime, string>(reader.GetInt32(0), reader.GetDateTime(1), reader.GetString(2));
                    }

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);

                }
            }

            return null;
        }

        public Tuple<int> IncidentCount(int student, string codes, DateTime startDate, string period = null)
        {
            var formattedCodes = String.Join("','", codes.Split(",".ToCharArray()));

            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 30;
                   
                    if (period == null)
                    {
                        cmd.CommandText =
                            string.Format(@"select count(*) from (select period, dbo.shortdate(swipetime) as swipedate from TardySwipe where 
                                studentid={0} and SwipeTime >= '{1}' and AttendanceCode in ('{2}')) as cntQuery",
                                student, startDate, formattedCodes);
                    }
                    else
                    {
                        cmd.CommandText =
                            string.Format(@"select count(*) from (select period, dbo.shortdate(swipetime) as swipedate from TardySwipe where 
                                studentid={0} and SwipeTime >= '{1}' and AttendanceCode in ('{2}') and Period = '{3}') as cntQuery",
                                student, startDate, formattedCodes, period);
                    }

                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        return new Tuple<int>(reader.GetInt32(0));
                    }

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);

                }
            }

            return null;
        }


        /// <summary>
        /// returns incident count, message to display, outcometypeid, StartDate, repeats
        /// </summary>
        /// <param name="code"></param>
        /// <param name="schoolId"></param>
        /// <returns></returns>
        public List<dynamic> GetConsequences(string code, int schoolId)
        {
            var list = new List<dynamic>();
            int count = 0;
            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 30;
                    cmd.CommandText =
                        string.Format("select IncidentCount, Message, OutcomeType, StartDate, Name, IncidentCode, Repeats, Period, Operator, ServeByDays from CorrectiveActions where StartDate <= '{0}' and SchoolId = {1} and IncidentCode like '%{2}%' And Active=1", DateTime.Today, schoolId, code);

                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        string message = string.Format("{0} ({1}): {2}", reader.GetString(4), reader.GetString(5), reader.GetString(1));

                        if (!DBNull.Value.Equals(reader[9]) && reader.GetInt32(9) > 0)
                        {
                            message += string.Format(".  Serve By {0}", DateTime.Today.AddBusinessDays(reader.GetInt32(9)).ToString("d"));
                        }
                        count++;
                        var o = new
                        {
                            IncidentCount = reader.GetInt32(0),
                            OutcomeType = reader.GetInt32(2),
                            StartDate = reader.GetDateTime(3),
                            Repeats = reader.GetBoolean(6),
                            IncidentCode = reader.GetString(5),
                            Period = DBNull.Value.Equals(reader[7]) ? null : reader.GetString(7),
                            Operator = DBNull.Value.Equals(reader[8]) ? "Equal" : reader.GetString(8),
                            ServeByDate = !DBNull.Value.Equals(reader[9]) ? DateTime.Today.AddBusinessDays(reader.GetInt32(9)) : DateTime.Today.AddDays(0),
                            Message = message
                        };

                        list.Add(o);
                    }

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);

                }
            }

            Logger.InfoFormat("{0} consequences downloads.", count);
            return list;
        }


        public dynamic GetSchoolSettings(int schoolid)
        {

            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                { 
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 60;
                    
                    cmd.CommandText =
                        string.Format("select * from School where SchoolId={0}", schoolid);

                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        var start = DateTime.Parse("8:00 AM");
                        var school = string.Empty;

                        if (reader.GetValue(1) != DBNull.Value)
                        {
                            school = reader.GetString(1);
                        }

                        if (reader.GetValue(10) != DBNull.Value)
                        {
                            start = reader.GetDateTime(10);
                        }

                        return new {SchoolId = reader.GetInt32(0), DayStartTime = start, SchoolName = school};
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    MessageBox.Show(
                        "There was a problem connecting to the database.  School Settings were not retrieved.  If this error persists, please contact an Administrator.");

                    //Application.Current.Shutdown();
                }
            }

            return null;
        }

        public IEnumerable<SchoolStartTime> GetSchoolStartTimes(int schoolid)
        {
            List<SchoolStartTime> startTimes = new List<SchoolStartTime>();

            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 60;

                    cmd.CommandText =
                        string.Format("select * from schoolstarttimes where SchoolId={0}", schoolid);

                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        //var start = DateTime.Parse(reader[2].ToString());


                        startTimes.Add(new SchoolStartTime(){ StartTime = reader[2].ToString(), Grade = reader[1].ToString() });
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    MessageBox.Show(
                        "There was a problem connecting to the database.  School Start Times were not retrieved.  If this error persists, please contact an Administrator.");

                    //Application.Current.Shutdown();
                }
            }

            return startTimes;
        }

        public IEnumerable<StudentStartTime> GetStudentStartTimes(int schoolId)
        {
            var startTimes = new List<StudentStartTime>();

            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 60;

                    cmd.CommandText =
                        string.Format("select * from studentstarttime where SchoolId={0}", schoolId);

                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        //var start = DateTime.Parse(reader[2].ToString());


                        startTimes.Add(new StudentStartTime() { StartTime = reader[4].ToString(), StudentNumber = reader[2].ToString() });
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    MessageBox.Show(
                        "There was a problem connecting to the database.  Student Start Times were not retrieved.  If this error persists, please contact an Administrator.");

                    //Application.Current.Shutdown();
                }
            }

            return startTimes;
        }

        public IObservable<IEnumerable<ScanLocation>> GetLocations(LocationType type)
        {
            var list = new List<ScanLocation>();

            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 20;

                    cmd.CommandText =
                        string.Format("select * from InOutRooms where SchoolId={0} and Active=1 /*and location_type = */ Order By RoomName", Settings.Default.SchoolId);

                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        
                        var typeInt = 0;
                        var iorId  = (long)reader.GetValue(0);
                        var roomName = reader.GetValue(2).ToString();
                        var allowMultiple = false;

                        if (!reader.IsDBNull(6))
                        {
                            typeInt = reader.GetInt32(6);
                        }

                        if (!reader.IsDBNull(7))
                        {
                            allowMultiple = reader.GetBoolean(7);
                        }

                        var location = new ScanLocation() { Id = iorId, Type = (LocationType)typeInt, RoomName = roomName, AllowMultipleScans = allowMultiple};

                        list.Add(location);

                    }

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    MessageBox.Show("There was a problem connecting to the local database.  If this error persists, contact an administrator.");

                    //Application.Current.Shutdown();
                }

                var rslts = list.Where(x => x.Type == type).ToList();

                return Observable.Return(rslts);
            }

            return null;
        }

        public IObservable<IEnumerable<string>> GetLunchPeriods()
        {
            var list = new List<string>();

            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 20;

                    cmd.CommandText =
                        string.Format("select distinct LunchKey from studentlunchtable where SchoolId={0} order by LunchKey", Settings.Default.SchoolId);

                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {

                      
                        var period = reader.GetValue(0).ToString();

                        list.Add(period);

                    }

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    MessageBox.Show("There was a problem getting lunch period data.  If this error persists, contact an administrator.");

                    //Application.Current.Shutdown();
                }

                return Observable.Return(list);
            }

            return null;
        }

        public IObservable<IEnumerable<string>> GetGroups()
        {
            var list = new List<string>();

            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 20;

                    cmd.CommandText =
                        string.Format("select distinct GroupName from Groups where SchoolId={0} order by GroupName", Settings.Default.SchoolId);

                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {


                        var item = reader.GetValue(0).ToString();

                        list.Add(item);

                    }

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    MessageBox.Show("There was a problem connecting to the local database to get groups.  If this error persists, contact an administrator.");

                    //Application.Current.Shutdown();
                }

                return Observable.Return(list);
            }

        }

        public Tuple<int, DateTime, string> CheckEntrySwipe(int student)
        {

            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 30;
                    cmd.CommandText =
                        string.Format("select StudentAttendId, EntryDate, StatusCd from PersonDayAttend inner join attendstatus on persondayattend.attstatusid = attendstatus.attendstatid where not attendstatus.statuscd = 'ABS' and studentid={0} and dbo.shortdate(entrydate) = '{1}'", student, DateTime.Today);

                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        return new Tuple<int, DateTime, string>(reader.GetInt32(0), reader.GetDateTime(1), reader.GetString(2));
                    }

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);

                }
            }

            return null;
        }

        public SpeedPassModel CheckEntrySwipe(string criteria)
        {

            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 30;
                    cmd.CommandText = "SELECT * FROM SPEEDPASS WHERE PASSID = @passId AND SchoolId = @school";
                  
                    cmd.Parameters.AddWithValue("@school", Settings.Default.SchoolId);
                    cmd.Parameters.AddWithValue("@passId", criteria);
                    //cmd.Parameters.AddWithValue("@datescanned", DateTime.Now);

                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        return new SpeedPassModel();
                    }

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);

                }
            }

            return null;
        }

        public void InsertVisit(VisitModel scan)
        {
            var current = DateTime.Now;

            var sql = string.Format("INSERT INTO VisitorLog(pk_VisitRecord,DateOfVisit, " +
                                    "PurposeForVisit,School,Source,VisitNumber, FirstName, LastName) " +
                                    "Values('{0}','{1}','{2}',{3},'{4}','{5}','{6}','{7}')",
                Guid.NewGuid(), scan.VisitEntryDate, scan.ReasonForVisit, Settings.Default.SchoolId, Environment.MachineName, scan.VisitEntryNumber, scan.FirstName, scan.LastName);

            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 30;
                    cmd.CommandText = sql;

                    cmd.ExecuteNonQuery();

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);

                }

                var elapsed = (current - DateTime.Now).TotalSeconds;

                Logger.WarnFormat("{0} seconds elapsed on insert visit", elapsed);
            }

        }

        public void InsertTardySwipe(Scan scan)
        {
            var current = DateTime.Now;

            var sql = string.Format("INSERT INTO TardySwipe(SchoolId,StudentId,SwipeTime,Location,Period, AttendanceCode, Source,LastUpdated) Values({0},{1},'{2}','{3}','{4}','{5}','{6}','{7}')",
                Settings.Default.SchoolId, scan.StudentId, scan.EntryTime, scan.ScanLocation.RoomName, scan.Period, scan.AttendanceCode, Environment.MachineName, DateTime.Now);
            
            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 30;
                    cmd.CommandText = sql;

                    cmd.ExecuteNonQuery();

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);

                }

                var elapsed = (current - DateTime.Now).TotalSeconds;

                Logger.WarnFormat("{0} seconds elapsed on insert tardy swipe", elapsed);
            }

        }

        public static void EnsureSchoolRecordExists(int school)
        {
            var qry = String.Format("select count(*) from School where Schoolid = {0}", school);

            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 30;
                    cmd.CommandText = qry;

                    var rslt = (int)cmd.ExecuteScalar();

                    if (rslt == 0)
                    {
                        var insert = connection.CreateCommand();
                        insert.CommandText = string.Format("Set identity_insert School on; Insert into School(SchoolId) values ({0}); Set identity_insert School off;", school);
                        insert.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);

                }
            }

        }


        public Tuple<int,DateTime> CheckDayAttendance(int student)
        {
           
            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 30;
                    cmd.CommandText =
                        string.Format("select studentattendid, entrydate from persondayattend inner join attendstatus on persondayattend.attstatusid = attendstatus.attendstatid where not attendstatus.statuscd = 'ABS' and where studentid={0} and entrydate > '{1}'", student,
                            DateTime.Today);

                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        return new Tuple<int, DateTime>(reader.GetInt32(0), reader.GetDateTime(1));
                    }
                    
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                   
                }
            }

            return null;
        }

        public IEnumerable<string> GetStudentImageNames()
        {
            var images = new List<string>();

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandTimeout = 30;
                cmd.CommandText = string.Format("Select PhotoPath from Person where PersonTypeID = 1 and SchoolId = {0} and Active=1", Settings.Default.SchoolId);


                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    images.Add(reader["PhotoPath"].ToString());
                }
            }

            return images;
        }

        public IObservable<bool> SendScan(Models.ScanModel scan)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Fine> Fines(int school, string filter = null)
        {
            var sql = String.Format("select OutcomeTypeName, FineAmt from Outcome where Schoolid = {0} and FineAmt > 0", school);

            var fines = new List<Fine>();
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandTimeout = 30;
                cmd.CommandText = sql;


                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    fines.Add(new Fine()
                    {
                        Name = reader["OutcomeTypeName"].ToString(),
                        Amount = decimal.Parse(reader["FineAmt"].ToString())
                    });
                }
            }

            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return fines.ToArray();
        }

        public IEnumerable<CardTemplate> Cards(int school)
        {
            var baseStmt = "select * from IdCards where Schoolid = {0} or SchoolId =-1 and Active = 1";

            var sql = String.Format(baseStmt, school);

            var cards = new List<CardTemplate>();
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandTimeout = 30;
                cmd.CommandText = sql;


                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    cards.Add(new CardTemplate()
                    {
                        TemplateName = reader["CardName"].ToString(),
                        Id = reader.GetInt32(0)
                    });
                }
            }

            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return cards.ToArray();
        }

        public CardTemplate FindCardByName(int school, string cardName)
        {
            var baseStmt = "select * from IdCards where Schoolid = @0 AND CardName = @1";

            var sql = String.Format(baseStmt, school);

            var cards = new List<CardTemplate>();
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandTimeout = 30;
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@0", school);
                cmd.Parameters.AddWithValue("@1", cardName);

                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    return new CardTemplate()
                    {
                        TemplateName = reader["CardName"].ToString(),
                        Id = reader.GetInt32(0)
                    };
                }
            }

            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return null;
        }

        

        public IEnumerable<CardTemplate> StudentCards(int school)
        {
            var baseStmt = "select * from IdCards where (Schoolid = {0} or SchoolId =-1) and Active = 1 AND StudentCard = 1";

            var sql = String.Format(baseStmt, school);

            var cards = new List<CardTemplate>();
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandTimeout = 30;
                cmd.CommandText = sql;


                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    cards.Add(new CardTemplate()
                    {
                        TemplateName = reader["CardName"].ToString(),
                        Id = reader.GetInt32(0)
                    });
                }
            }

            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return cards.ToArray();
        }

        public IEnumerable<CardTemplate> TeacherCards(int school)
        {
            var baseStmt = "select * from IdCards where (Schoolid = {0} or SchoolId =-1) and Active = 1 AND TeacherCard = 1";

            var sql = String.Format(baseStmt, school);

            var cards = new List<CardTemplate>();
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandTimeout = 30;
                cmd.CommandText = sql;


                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    cards.Add(new CardTemplate()
                    {
                        TemplateName = reader["CardName"].ToString(),
                        Id = reader.GetInt32(0)
                    });
                }
            }

            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return cards.ToArray();
        }

        public static bool CheckConnection()
        {
            var count = -1;
            try
            {

                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 30;
                    cmd.CommandText = "select count(*) from school";

                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {

                        count = reader.GetInt32(0);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("There was a problem starting the application. ", ex);
            }

            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return count != -1;
        }


        public int GetStudentScanCount(int school)
        {
            var sql = String.Format("select count(*) from redisscans where schoolid = {0} and swipetime >= '{1}' and ObjectType in ('SwipeDesktop.Models.ScanModel','SwipeDesktop.Models.LocationScan','SwipeDesktop.Models.Dismissal')", school, DateTime.Today.ToString("yyyy-MM-dd HH:mm:ss"));

            var count = 0;
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandTimeout = 30;
                cmd.CommandText = sql;


                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {

                    count = reader.GetInt32(0);
                }
            }

            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return count;
        }

        public IObservable<Tuple<int, IEnumerable<StudentModel>> > GetBatchPrintCards(int school, string batchBy, string sortBy, bool checkForImage, IEnumerable<string> selectedCriteria, int templateId)
        {
            List<StudentModel> students = new List<StudentModel>();

            string sort = "ORDER BY LastName ASC";
            string criteria = "";

            if (sortBy.ToLower() == "homeroom")
            {
                sort = "ORDER BY HomeRoom ASC";
            }

            if (batchBy.ToLower() == "homeroom")
            {
                criteria = string.Format("AND HOMEROOM IN ('{0}')", String.Join("','", selectedCriteria));
            }

            if (batchBy.ToLower() == "grade")
            {
                criteria = string.Format("AND Grade IN ('{0}')", String.Join("','", selectedCriteria));
            }

            var sql = string.Format(
                "Select s.StudentId, s.GUID, s.Grade, s.Homeroom, p.* from Person p " +
                "LEFT OUTER JOIN Students s on p.personid = s.personid where p.schoolid = {0} AND Active = 1 {1} {2}", Settings.Default.SchoolId, criteria, sort);


            var count = 0;
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandTimeout = 30;
                cmd.CommandText = sql;

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var hasImage = true;

                        if (checkForImage)
                            hasImage = checkImageExists(reader["SSN"].ToString());

                        if (hasImage)
                        {
                            students.Add(new StudentModel()
                            {
                                UniqueId = Guid.Parse(reader["Guid"].ToString()),
                                IdNumber = reader["SSN"].ToString(),
                                LastName = reader["LastName"].ToString(),
                                DateOfBirth = reader.IsDBNull(11) ? "1/1/1900" : reader.GetDateTime(11).ToShortDateString(),
                                FirstName = reader["FirstName"].ToString(),
                                Grade = DBNull.Value == reader["Grade"] ? "N/A" : reader["Grade"].ToString(),
                                Homeroom = DBNull.Value == reader["Homeroom"] ? "N/A" : reader["Homeroom"].ToString(),
                                PhotoPath = reader["PhotoPath"].ToString(),
                                PersonId = int.Parse(reader["PersonId"].ToString()),
                                StudentId = reader.GetInt32(0)
                            });
                        }
                    }
                }
            }

            return Observable.Return(new Tuple<int, IEnumerable<StudentModel>>(templateId,students.ToArray()));
        }

        bool checkImageExists(string studentNumber)
        {
            /*
            System.Net.HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create($"https://app.swipek12.com/SWIPEDATA/{Settings.Default.SchoolId}/Pictures/{studentNumber}.jpg");
            request.Method = "HEAD";

            bool exists;
            try
            {
                request.GetResponse();
                exists = true;
            }
            catch
            {
                exists = false;
            }*/

            return File.Exists(System.IO.Path.Combine(SwipeUtils.getPhotoImageFolder(), $"{studentNumber}.jpg"));
        }

        public IObservable<List<string>> GetBatchPrintItems(int school, string batchBy)
        {
            List<string> items = new List<string>();

            var asyncConnection = new SqlConnectionStringBuilder(ConnectionString) { AsynchronousProcessing = true }.ToString();

            var sql = string.Format("SELECT firstname + ' ' + lastname FROM PERSON WHERE PersonTypeID = 1 AND SchoolId = {0}", school);

            if (batchBy.ToLower() == "grade")
            {
                sql = string.Format("SELECT DISTINCT Grade FROM Students WHERE SchoolId = {0} ORDER BY Grade", school);
            }
            
            if (batchBy.ToLower() == "homeroom")
            {
                sql = string.Format("SELECT DISTINCT Homeroom FROM Students WHERE SchoolId = {0} ORDER BY Homeroom", school);
            }

            var count = 0;
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandTimeout = 30;
                cmd.CommandText = sql;

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if(!reader.IsDBNull(0) && !string.IsNullOrEmpty(reader.GetString(0)))
                        {
                            items.Add(reader.GetString(0));
                        }
                           
                    }
                }
            }

            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return Observable.Return(items); ;
        }

        public IObservable<List<Tuple<string, int>>> GetIdCardTemplates(int school)
        {
            List<Tuple<string, int>> items = new List<Tuple<string, int>>();

            var asyncConnection = new SqlConnectionStringBuilder(ConnectionString) { AsynchronousProcessing = true }.ToString();

            var sql = string.Format("SELECT CardName, CardId FROM IDCards WHERE SchoolId = {0} and active=1", school);

            var count = 0;
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandTimeout = 30;
                cmd.CommandText = sql;

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new Tuple<string, int>(reader.GetString(0), reader.GetInt32(1)));
                    }
                }
            }

            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return Observable.Return(items); ;
        }

        public async Task<int> GetStudentScanCountAsync(int school)
        {
            var asyncConnection = new SqlConnectionStringBuilder(ConnectionString) { AsynchronousProcessing = true }.ToString();

            var sql = String.Format("select count(*) from redisscans where schoolid = {0} and swipetime >= '{1}' and ObjectType in ('SwipeDesktop.Models.ScanModel','SwipeDesktop.Models.LocationScan','SwipeDesktop.Models.Dismissal','SwipeDesktop.Models.Dismissal')", school, DateTime.Today);

            var count = 0;
            using (var connection = new SqlConnection(asyncConnection))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandTimeout = 30;
                cmd.CommandText = sql;


                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (!reader.HasRows)
                    {

                        return 0;
                    }

                    reader.Read();
                    count = reader.GetInt32(0);
                }
            }

            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return count;
        }

        public int GetErrorScanCount(int school)
        {
            var sql = String.Format("select count(*) from redisscans where schoolid = {0} and RedisId = 0 and synctime is null", school, DateTime.Today);

            var count = 0;
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandTimeout = 30;
                cmd.CommandText = sql;


                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {

                    count = reader.GetInt32(0);
                }
            }

            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return count;
        }
        public async Task<int> GetErrorScanCountAsync(int school)
        {
            var asyncConnection = new SqlConnectionStringBuilder(ConnectionString) { AsynchronousProcessing = true }.ToString();

            var sql = String.Format("select count(*) from redisscans where schoolid = {0} and RedisId = 0 and synctime is null", school, DateTime.Today);

            var count = 0;
            using (var connection = new SqlConnection(asyncConnection))
            {
                using (var cmd = new SqlCommand())
                {
                    cmd.Connection = connection;
                    cmd.CommandText = sql;
                    cmd.CommandType = CommandType.Text;
                    connection.Open();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (!reader.HasRows)
                        {
                            
                            return 0;
                        }

                        reader.Read();
                        count = reader.GetInt32(0);
                    }
                }
            }
           
            return count;
        }
        public int GetQueueCount(int school)
        {
            var sql = String.Format("select count(*) from redisscans where schoolid = {0} and synctime is null", school, DateTime.Today);

            var count = 0;
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandTimeout = 30;
                cmd.CommandText = sql;


                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {

                    count = reader.GetInt32(0);
                }
            }

            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return count;
        }

        public async Task<int> GetQueueCountAsync(int school)
        {
            var asyncConnection = new SqlConnectionStringBuilder(ConnectionString) { AsynchronousProcessing = true }.ToString();

            var sql = String.Format("select count(*) from redisscans where schoolid = {0} and synctime is null", school, DateTime.Today);

            var count = 0;
            using (var connection = new SqlConnection(asyncConnection))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandTimeout = 30;
                cmd.CommandText = sql;

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (!reader.HasRows)
                    {

                        return 0;
                    }

                    reader.Read();
                    count = reader.GetInt32(0);
                }
            }

            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return count;
        }

        public void ResetSyncErrors(int school)
        {
            var sql = String.Format("update redisscans set RedisId = Id where schoolid = {0} and RedisId = 0 and synctime is null", school, DateTime.Today);

            var count = 0;
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandTimeout = 30;
                cmd.CommandText = sql;


                cmd.ExecuteNonQuery();

       
            }

        
        }


        public static Dictionary<int, string> Students(int school)
        {
            var sql = String.Format("select * from Students inner join person on students.personid = person.personid where students.Schoolid = {0} and person.Active = 1", school);

            var cards = new Dictionary<int,string>();
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandTimeout = 30;
                cmd.CommandText = sql;


                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var key = int.Parse(reader["PersonId"].ToString());
                    if (!cards.ContainsKey(key)){
                        cards.Add(key, reader["studentnumber"].ToString());
                    }
                }
            }

            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return cards;
        }

        public IEnumerable<Tuple<DateTime>> PvcIdsPrinted(int personId)
        {
            var sql = String.Format("select po.* from personoutcome po inner join Outcome o on po.outcometypeid = o.outcometypeid where o.OutcomeTypeName = 'Replacement ID' AND PersonId = {0} and Active = 1", personId);

            var cards = new List<Tuple<DateTime>>();
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandTimeout = 30;
                cmd.CommandText = sql;


                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    cards.Add(new Tuple<DateTime>(reader.GetDateTime(5)));
                }
            }

            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return cards.ToArray();
        }

        public IEnumerable<Tuple<DateTime>> TempIdsPrinted(int personId)
        {
            var sql = String.Format("select po.* from personoutcome po inner join Outcome o on po.outcometypeid = o.outcometypeid where o.OutcomeTypeName = 'Temp ID' AND PersonId = {0} and Active = 1", personId);

            var cards = new List<Tuple<DateTime>>();
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandTimeout = 30;
                cmd.CommandText = sql;


                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    cards.Add(new Tuple<DateTime>(reader.GetDateTime(5)));
                }
            }

            //var filteredList = list.Where(x => x.ToLower().Contains(filter.ToLower()));
            return cards.ToArray();
        }
    }
}
