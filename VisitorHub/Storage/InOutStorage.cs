using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;
using Oak;
using ServiceStack;
using ServiceStack.Redis;
using ServiceStack.Redis.Generic;
using Simple.Data;
using SwipeDesktop.Models;
using SwipeDesktop.ViewModels;
using Xceed.Wpf.DataGrid.Converters;

namespace SwipeDesktop.Storage
{
    public class InOutStorage : RedisStorage<long>, IStorage<LocationScan, long>
    {
      
        private static readonly ILog Logger = LogManager.GetLogger(typeof(InOutStorage));

        private static string LocationUrn = "urn:Set:{0}:{1}";

        private static string LocationByStudentUrn = "urn:Set:{0}:{1}:{2}";

        //private static string StudentUrn = "urn:Set:{0}:{1}";

        private static string DateUrn = "urn:Set:{0}:{1}";

        public static readonly string SyncUri = string.Format("urn:Set:Sync:{0}", typeof(LocationScan));

        private readonly IRedisTypedClient<LocationScan> _typedClient;

        public IRedisTypedClient<LocationScan> RedisClient { get { return _typedClient; } }

        public InOutStorage()
        {
            _typedClient = Client.As<LocationScan>();
        }

        public LocationScan GetById(long key)
        {         
            return _typedClient.GetById(key);
        }

        public Task<LocationScan> GetObjectAsync(long key)
        {
            throw new NotImplementedException();
        }

        public void InsertObject(long key, LocationScan scan)
        {
            //does scan exist already

            _typedClient.Store(scan);

            //add to student location index
            //StudentUrn = string.Format(StudentUrn, typeof(LocationScan), scan.StudentNumber);
            //Client.AddItemToSet(StudentUrn, key.ToString(CultureInfo.CurrentCulture));

            //add to date index
            DateUrn = string.Format(DateUrn, typeof(LocationScan), scan.SwipeTime.Date.ToString("MM-dd-yyyy"));
            Client.AddItemToSet(DateUrn, key.ToString(CultureInfo.CurrentCulture));

            //add to location index
            LocationUrn = string.Format(LocationUrn, scan.RoomName, scan.SwipeTime.Date.ToString("MM-dd-yyyy"));
            Client.AddItemToSet(LocationUrn, key.ToString(CultureInfo.CurrentCulture));

            LocationByStudentUrn = string.Format(LocationByStudentUrn, scan.RoomName, scan.SwipeTime.Date.ToString("MM-dd-yyyy"), scan.StudentNumber);
            Client.AddItemToSet(LocationByStudentUrn, key.ToString(CultureInfo.CurrentCulture));

            //add to set to find records needing sync to server
            Client.AddItemToSet(SyncUri, key.ToString(CultureInfo.CurrentCulture));
          
        }

        public Task InsertObjectAsync(long key, LocationScan scan)
        {
            throw new NotImplementedException();
        }

        public LocationScan InsertObject(LocationScan o, bool isNew = true)
        {
            try
            {
                if (isNew)
                {
                    o.Id = _typedClient.GetNextSequence(); //GetNextIdentityValue();

                    BackupRecord(new
                    {
                        Settings.Default.SchoolId,
                        o.StudentNumber,
                        o.SwipeTime,
                        RedisId = o.Id,
                        ObjectType = typeof(LocationScan).ToString(),
                        ObjectJson = JsonConvert.SerializeObject(o)
                    });
                }
                //Client.Save();

                InsertObject(o.Id, o);
                //Logger.InfoFormat("Snapshot recorded {0}.", Client.LastSave);
            }
            catch (RedisResponseException rre)
            {
                Logger.Error(rre); 
                try
                {
                    if (isNew)
                    {
                        BackupRecord(new
                        {
                            Settings.Default.SchoolId,
                            o.StudentNumber,
                            o.SwipeTime,
                            RedisId = 0,
                            ObjectType = typeof(LocationScan).ToString(),
                            ObjectJson = JsonConvert.SerializeObject(o)
                        });
                    }
                }
                catch (Exception iex) { Logger.Error(iex); }
            }
            catch (Exception ex)
            {
                try
                {
                    if (isNew)
                    {
                        BackupRecord(new
                        {
                            Settings.Default.SchoolId,
                            StudentNumber = o.StudentNumber,
                            SwipeTime = o.SwipeTime,
                            RedisId = 0,
                            ObjectType = typeof(LocationScan).ToString(),
                            ObjectJson = JsonConvert.SerializeObject(o)
                        });
                    }
                }
                catch (Exception iex) { Logger.Error(iex); }

                Logger.Error(ex);

            }

            return o;
        }

        public Task InsertObjectAsync(LocationScan o)
        {
            throw new NotImplementedException();
        }


        public Task RemoveObject(long key)
        {
            throw new NotImplementedException();
        }

        public long[] FindIdsByRoom(DateTime date, string room, out string urn)
        {
            urn = string.Format(LocationUrn, room, date.Date.ToString("MM-dd-yyyy"));
            var items = Client.GetAllItemsFromSet(urn);

            var list = items.Select(long.Parse).ToArray();

            return list;
        }
        
        public IEnumerable<LocationScan> GetItemsByDate(DateTime date)
        {
            var urn = string.Format(DateUrn, typeof(LocationScan), date.Date.ToString("MM-dd-yyyy"));
            var items = Client.GetAllItemsFromSet(urn);

            var list = items.Select(long.Parse).ToArray();

            return Client.GetByIds<LocationScan>(list);
        }

        public IEnumerable<LocationScan> GetItemsByDate(string room, DateTime date)
        {
            var urn = string.Format(LocationUrn, room, date.Date.ToString("MM-dd-yyyy"));
            var items = Client.GetAllItemsFromSet(urn);

            var list = items.Select(long.Parse).ToArray();

            return Client.GetByIds<LocationScan>(list);
        }

        public IEnumerable<LocationScan> GetItemsByStudent(string room, DateTime date, string student)
        {
            var urn = string.Format(LocationByStudentUrn, room, date.Date.ToString("MM-dd-yyyy"), student);
            var items = Client.GetAllItemsFromSet(urn);

            var list = items.Select(long.Parse).ToArray();

            return Client.GetByIds<LocationScan>(list).Where(x=>x.StudentNumber == student);
        }

        public long[] FindNotSynced(out string urn)
        {
            urn = SyncUri;
            var items = Client.GetAllItemsFromSet(urn);

            var list = items.Select(long.Parse).ToArray();

            return list;
        }

        public int CountNotSynced()
        {
            var items = Client.GetAllItemsFromSet(SyncUri);

            return items.Count();
        }


        public void RemoveFromSet(long id, string urn)
        {
            Client.RemoveItemFromSet(urn, id.ToString());
        }
      
        public long[] FindByDate(DateTime date, out string urn)
        {
            urn = string.Format(DateUrn, typeof(LocationScan), date.Date.ToString("MM-dd-yyyy"));
            var items = Client.GetAllItemsFromSet(urn);

            var list = items.Select(long.Parse).ToArray();

            return list;
        }
    }
}
