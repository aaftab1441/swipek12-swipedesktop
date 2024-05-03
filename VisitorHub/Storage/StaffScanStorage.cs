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
using ServiceStack;
using ServiceStack.Redis;
using ServiceStack.Redis.Generic;
using SwipeDesktop.Models;
using SwipeDesktop.ViewModels;
using Xceed.Wpf.DataGrid.Converters;

namespace SwipeDesktop.Storage
{
    public class StaffScanStorage : RedisStorage<long>, IStorage<StaffRecord, long>
    {

        private static readonly ILog Logger = LogManager.GetLogger(typeof(StaffScanStorage));

        private const string DateUrn = "urn:Set:StaffScans:{0}";
        private static readonly string SyncUri = string.Format("urn:Set:StaffSync:{0}", typeof(StaffRecord));
        private const string StaffScanSetUri = "urn:Set:StaffScanSet:{0}";

        private readonly IRedisTypedClient<StaffRecord> _typedClient;

        public IRedisTypedClient<StaffRecord> RedisClient { get { return _typedClient; } }

        public StaffScanStorage()
        {
            _typedClient = Client.As<StaffRecord>();
        }

        public StaffRecord GetById(long key)
        {   
            return _typedClient.GetById(key);
        }

        public StaffRecord GetByStaff(int key, DateTime date)
        {
            //get student index
            var urn = string.Format(StaffScanSetUri, key);
            var items = Client.GetAllItemsFromSet(urn);

            //get scan index for date
            var urn2 = string.Format(DateUrn, date.Date.ToString("MM-dd-yyyy"));
            var items2 = Client.GetAllItemsFromSet(urn2);

            //intersect indexes
            items2.IntersectWith(items);
            //var item = items2.SingleOrDefault();
            var scans = _typedClient.GetByIds(items2);

            //return scan
            if (scans.Any())
                return scans[0] ;
           
            return null;
        }
       
        public Task<StaffRecord> GetObjectAsync(long key)
        {
            throw new NotImplementedException();
        }

        public void InsertObject(long key, StaffRecord scan)
        {
            //does scan exist already
            if (GetByStaff(scan.PersonId, scan.EntryTime.Date) == null)
            {
                _typedClient.Store(scan);

                var uri2 = string.Format(DateUrn, scan.EntryTime.Date.ToString("MM-dd-yyyy"));
              
                //add to set by date to index by Date Queries
                Client.AddItemToSet(uri2, key.ToString(CultureInfo.CurrentCulture));

                //add to set to find records needing sync to server
                string syncUri;

                var list = FindNotSynced(out syncUri);

                var items = Client.GetByIds<StaffRecord>(list).Select(x=>new Tuple<int, DateTime>(x.PersonId, x.EntryTime.Date));

                if (!items.Any(x=>x.Item1 == scan.PersonId))
                {
                    Client.AddItemToSet(SyncUri, key.ToString(CultureInfo.CurrentCulture));
                }

                //add to set to student set
                var setUri = string.Format(StaffScanSetUri, scan.PersonId);

                Client.AddItemToSet(setUri, key.ToString(CultureInfo.CurrentCulture));

            }

        }

        public Task InsertObjectAsync(long key, StaffRecord scan)
        {
            throw new NotImplementedException();
        }

        public StaffRecord InsertObject(StaffRecord scan, bool isNew = true)
        {

            Logger.WarnFormat("Inserting {0}, {1}, {2} with idx {3}", scan.Barcode, scan.EntryTime, scan.Location, scan.Id);

            try
            {

                if (isNew)
                {
                    scan.Id = _typedClient.GetNextSequence(); //GetNextIdentityValue(); // 

                    BackupRecord(new
                    {
                        Settings.Default.SchoolId,
                        StudentNumber = scan.Barcode,
                        SwipeTime = scan.EntryTime,
                        RedisId = scan.Id,
                        ObjectType = typeof(StaffRecord).ToString(),
                        ObjectJson = JsonConvert.SerializeObject(scan)
                    });

                }

                InsertObject(scan.Id, scan);

                //Client.Save();
            }
            catch (RedisResponseException rre)
            {
                try
                {
                    if (isNew)
                    {
                        BackupRecord(new
                        {
                            Settings.Default.SchoolId,
                            StudentNumber = scan.Barcode,
                            SwipeTime = scan.EntryTime,
                            RedisId = 0,
                            ObjectType = typeof(StaffRecord).ToString(),
                            ObjectJson = JsonConvert.SerializeObject(scan)
                        });
                    }
                }
                catch (Exception iex) { Logger.Error(iex); }

                Logger.Error(rre); 
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
                            StudentNumber = scan.Barcode,
                            SwipeTime = scan.EntryTime,
                            RedisId = 0,
                            ObjectType = typeof(StaffRecord).ToString(),
                            ObjectJson = JsonConvert.SerializeObject(scan)
                        });
                    }
                }
                catch (Exception iex) { Logger.Error(iex); }

                Logger.Error(ex); 
                
            }

            return scan;
        }

        public Task InsertObjectAsync(StaffRecord scan)
        {
            
            throw new NotImplementedException();
        }


        public Task RemoveObject(long key)
        {
            throw new NotImplementedException();
        }

        public long[] FindByDate(DateTime date, out string urn)
        {
            urn = string.Format(DateUrn, date.Date.ToString("MM-dd-yyyy"));
            var items = Client.GetAllItemsFromSet(urn);

            var list = items.Select(long.Parse).ToArray();

            return list;
        }

        public IEnumerable<StaffRecord> GetItemsByDate(DateTime date)
        {
            var urn = string.Format(DateUrn, date.Date.ToString("MM-dd-yyyy"));
            var items = Client.GetAllItemsFromSet(urn);

            var list = items.Select(Guid.Parse).ToArray();

            return Client.GetByIds<StaffRecord>(list);
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
    }
}
