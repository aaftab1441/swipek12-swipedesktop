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
    public class ScanStorage : RedisStorage<long>, IStorage<ScanModel, long>
    {

        private static readonly ILog Logger = LogManager.GetLogger(typeof(ScanStorage));

        private const string PeriodUrn = "urn:Set:Scans:{0}:{1}";
        private const string StudentPeriodUrn = "urn:Set:Scans:{0}:{1}:{2}";
        private const string DateUrn = "urn:Set:Scans:{0}";
        private static readonly string SyncUri = string.Format("urn:Set:Sync:{0}", typeof(ScanModel));
        private const string StudentScanSetUri = "urn:Set:StudentScanSet:{0}";

        private readonly IRedisTypedClient<ScanModel> _typedClient;

        public IRedisTypedClient<ScanModel> RedisClient { get { return _typedClient; } }

        public ScanStorage()
        {
            //ModelConfig<ScanModel>.Id(x => x.Id);

            _typedClient = Client.As<ScanModel>();
        }

        public ScanModel GetById(long key)
        {
            /*
            var urn = string.Format(DateUrn, DateTime.Today.ToString("MM-dd-yyyy"));
            var items = Client.GetAllItemsFromSet(urn);
            
            var item = items.SingleOrDefault(x => x == key.ToString());
            
            if(item != null)
                return _typedClient.GetById(item);*/
            
            return _typedClient.GetById(key);
        }

        public ScanModel GetByStudent(Guid key, DateTime date)
        {
            try
            {
                //get student index
                var urn = string.Format(StudentScanSetUri, key);
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
                    return scans[0];

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return null;
        }
        /*
        public ScanModel GetByStudent(Guid key, DateTime date, string p)
        {
            //get student index
            var urn = string.Format(StudentScanSetUri, key);
            var items = Client.GetAllItemsFromSet(urn);

            //get scan index for date
            var urn2 = string.Format(PeriodUrn, date.Date.ToString("MM-dd-yyyy"), p);
            var items2 = Client.GetAllItemsFromSet(urn2);

            //intersect indexes
            items.IntersectWith(items2);
            var item = items.SingleOrDefault();

            //return scan
            if (item != null)
                return _typedClient.GetById(item);

            return null;
        }
        */
        public ScanModel GetByStudent(Guid key, DateTime date, string p)
        {
           
            //get scan index for date period student
            var urn3 = string.Format(StudentPeriodUrn, key, date.Date.ToString("MM-dd-yyyy HH:mm:ss"), p);
            var items = Client.GetAllItemsFromSet(urn3);

           
            //return scan
            if (items.Any())
                return _typedClient.GetById(items.First());

            return null;
        }

        public Task<ScanModel> GetObjectAsync(long key)
        {
            throw new NotImplementedException();
        }

        public void InsertObject(long key, ScanModel scan)
        {
            //does scan exist already
            if (GetByStudent(scan.StudentGuid, scan.EntryTime.Date, scan.Period) == null)
            {
                _typedClient.Store(scan);

                var uri = string.Format(PeriodUrn, scan.EntryTime.Date.ToString("MM-dd-yyyy"), scan.Period);
                var uri2 = string.Format(DateUrn, scan.EntryTime.Date.ToString("MM-dd-yyyy"));

                var uri3 = string.Format(StudentPeriodUrn, scan.StudentGuid, scan.EntryTime.Date.ToString("MM-dd-yyyy HH:mm:ss"), scan.Period);

                //add to set by date to index by Date and Period for Queries
                Client.AddItemToSet(uri, key.ToString(CultureInfo.CurrentCulture));

                //add to set by date to index by Date Queries
                Client.AddItemToSet(uri2, key.ToString(CultureInfo.CurrentCulture));

                //add to student period date index
                Client.AddItemToSet(uri3, key.ToString(CultureInfo.CurrentCulture));

                //add to set to find records needing sync to server
                Client.AddItemToSet(SyncUri, key.ToString(CultureInfo.CurrentCulture));

                /* 
                string syncUri;
                var list = FindNotSynced(out syncUri);

                var items = Client.GetByIds<ScanModel>(list).Select(x=>new Tuple<Guid, string>(x.StudentGuid, x.Period));

                if (!items.Any(x=>x.Item1 == scan.StudentGuid && x.Item2 == scan.Period))
                {
                    Client.AddItemToSet(SyncUri, key.ToString(CultureInfo.CurrentCulture));
                }
                */

                //add to set to student set
                var setUri = string.Format(StudentScanSetUri, scan.StudentGuid);

                Client.AddItemToSet(setUri, key.ToString(CultureInfo.CurrentCulture));

            }

        }

        public Task InsertObjectAsync(long key, ScanModel scan)
        {
            throw new NotImplementedException();
        }

        public ScanModel InsertObject(ScanModel scan, bool isNew = true)
        {

            try
            {

                if (isNew)
                {
                    Logger.DebugFormat("backing up record for {0}", scan.Barcode);
                    scan.Id = _typedClient.GetNextSequence(); //GetNextIdentityValue(); // 

                    BackupRecord(new
                    {
                        Settings.Default.SchoolId,
                        StudentNumber = scan.Barcode,
                        SwipeTime = scan.EntryTime,
                        RedisId = scan.Id,
                        ObjectType = typeof(ScanModel).ToString(),
                        ObjectJson = JsonConvert.SerializeObject(scan)
                    });

                }

                Logger.WarnFormat("Inserting {0}, {1}, {2}, {3}, {4} with idx {5}", scan.Barcode, scan.Period,
                    scan.AttendanceCode, scan.EntryTime, scan.Location, scan.Id);

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
                            ObjectType = typeof(ScanModel).ToString(),
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
                            ObjectType = typeof(ScanModel).ToString(),
                            ObjectJson = JsonConvert.SerializeObject(scan)
                        });
                    }
                }
                catch (Exception iex) { Logger.Error(iex); }

                Logger.Error(ex); 
                
            }

            return scan;
        }

        public Task InsertObjectAsync(ScanModel scan)
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

        public IEnumerable<ScanModel> GetItemsByDate(DateTime date)
        {
            var urn = string.Format(DateUrn, date.Date.ToString("MM-dd-yyyy"));
            var items = Client.GetAllItemsFromSet(urn);

            var list = items.Select(Guid.Parse).ToArray();

            return Client.GetByIds<ScanModel>(list);
        }

        public IEnumerable<ScanModel> GetItemsByDateAndPeriod(DateTime date, string p)
        {
            var urn = string.Format(PeriodUrn, date.Date.ToString("MM-dd-yyyy"), p);
            var items = Client.GetAllItemsFromSet(urn);

            var list = items.Select(Guid.Parse).ToArray();

            return Client.GetByIds<ScanModel>(list);
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
