using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;
using ServiceStack.Redis;
using Simple.Data;

namespace SwipeDesktop.Storage
{
    public class RedisStorage<TId> : IDisposable
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(RedisStorage<>));

        private readonly string RedisConfig = System.Configuration.ConfigurationManager.AppSettings["RedisConfig"];
        protected readonly RedisClient Client;

        private readonly dynamic Db;

        public RedisStorage(string host = "localhost")
        {
            try
            {
                Client = new RedisClient(host);

                Client.ConfigSet("save", ASCIIEncoding.ASCII.GetBytes("60 1"));
                Client.ConfigSet("dbfilename", ASCIIEncoding.ASCII.GetBytes("swipe.rdb"));
                Client.ConfigSet("stop-writes-on-bgsave-error", ASCIIEncoding.ASCII.GetBytes("no")); // no
            }
            catch (RedisException rex)
            {
                Logger.Error(rex);
            }
            Db = Database.OpenNamedConnection("ScanStation");
        }

        public virtual object BackupRecord(dynamic record)
        {
            try
            {
                
                var data = Db.RedisScans.Insert(record);

                //return data.Id;
            }
            catch (Exception ex)
            {
                Logger.Error("Could not store backup sync record.",ex);
            }

            return 0;
        }

        static readonly string ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["ScanStation"].ConnectionString;

        public int[] FindNotSynced(string model)
        {
            try
            {
                var recs = new List<int>();
                var count = 0;
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 30;
                    cmd.CommandText = string.Format("Select Id from RedisScans where SyncTime IS NULL AND ObjectType = '{0}'", model);

                    using (var dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        while (dr.Read())
                        {
                            recs.Add(dr.GetInt32(0));
                        }
                    }

                    return recs.ToArray();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return new int[] {};
            }
        }

        public T GetFromDatabase<T>(int id)
        {
            var record = Db.RedisScans.Find(Db.RedisScans.Id == id && Db.RedisScans.ObjectType == typeof(T).ToString());

            return JsonConvert.DeserializeObject<T>(record.ObjectJson);
        }

        public long GetNextIdentityValue()
        {
            var sql = String.Format("exec GetNextIndexOnRedisScans");

            try
            {
                var count = 0;
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandTimeout = 30;
                    cmd.CommandText = sql;

                    return long.Parse(cmd.ExecuteScalar().ToString());


                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return long.Parse(DateTime.Now.ToString("ddHHmmssff"));
            }

        }

        public virtual void MarkAsSynced<T>(long id, string studentNumber, DateTime date)
        {
            var record = Db.RedisScans.Find(Db.RedisScans.RedisId == id && Db.RedisScans.StudentNumber == studentNumber && (Db.RedisScans.SwipeTime >= date && Db.RedisScans.SwipeTime < date.AddDays(1)) && Db.RedisScans.ObjectType == typeof(T).ToString());
            if (record != null)
            {
                record.SyncTime = DateTime.Now;
                Db.RedisScans.UpdateById(record);
            }

            record = Db.RedisScans.Find(Db.RedisScans.Id == id && Db.RedisScans.StudentNumber == studentNumber && (Db.RedisScans.SwipeTime >= date && Db.RedisScans.SwipeTime < date.AddDays(1)) && Db.RedisScans.ObjectType == typeof(T).ToString());
            if (record != null)
            {
                record.SyncTime = DateTime.Now;
                Db.RedisScans.UpdateById(record);
            }
        }

        public virtual void RecordDatabaseSynced<T>(long id, string studentNumber, DateTime date)
        {
         
            var record = Db.RedisScans.Find(Db.RedisScans.Id == id && Db.RedisScans.StudentNumber == studentNumber && (Db.RedisScans.SwipeTime >= date && Db.RedisScans.SwipeTime < date.AddDays(1)) && Db.RedisScans.ObjectType == typeof(T).ToString());
            if (record != null)
            {
                record.SyncTime = DateTime.Now;
                Db.RedisScans.UpdateById(record);
            }
        }

        public void Dispose()
        {
            Client.Dispose();
        }

        public void AddToSet(long id, string urn)
        {
            Client.AddItemToSet(urn, id.ToString());
        }

    }
}
