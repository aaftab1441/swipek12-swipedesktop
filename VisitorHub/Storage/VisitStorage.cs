using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Repository.Hierarchy;
using Newtonsoft.Json;
using ServiceStack.Redis;
using ServiceStack.Redis.Generic;
using SwipeDesktop.Models;
using Xceed.Wpf.DataGrid.Converters;

namespace SwipeDesktop.Storage
{
    public class VisitStorage : RedisStorage<long>, IStorage<VisitModel, long>
    {

        private static readonly ILog Logger = LogManager.GetLogger(typeof(VisitStorage));

        private const string GroupUri = "urn:Set:Date:{0}";
        private static readonly string SyncUri = string.Format("urn:Set:Sync:{0}", typeof(VisitModel));
        private IRedisTypedClient<VisitModel> _typedClient;

        public IRedisTypedClient<VisitModel> RedisClient { get { return _typedClient; } } 

        public VisitStorage()
        {
            _typedClient = Client.As<VisitModel>();
        }
        
        public VisitModel GetById(long key)
        {
            return _typedClient.GetById(key);
        }

        public Task<VisitModel> GetObjectAsync(long key)
        {
            throw new NotImplementedException();
        }

        public void InsertObject(long key, VisitModel visit)
        {
            visit.Id = key;

            _typedClient.Store(visit);

            var uri = string.Format(GroupUri, visit.VisitEntryDate.Date.ToString("MM-dd-yyyy"));
            
            //add to set by date to index by Date for Queries
            Client.AddItemToSet(uri, visit.Id.ToString(Thread.CurrentThread.CurrentCulture));

            //add to set to find records needing sync to server
            Client.AddItemToSet(SyncUri, visit.Id.ToString(Thread.CurrentThread.CurrentCulture));

            try
            {
                Client.Save();
            }catch(Exception ex){ Logger.Error(ex);}

        }

        public Task InsertObjectAsync(long key, VisitModel visit)
        {
            throw new NotImplementedException();
        }
        public VisitModel InsertObject(VisitModel visit, bool isNew = true)
        {

            try
            {

                if (isNew)
                {
                    visit.Id = _typedClient.GetNextSequence(); //GetNextIdentityValue(); // 

                    BackupRecord(new
                    {
                        Settings.Default.SchoolId,
                        StudentNumber = visit.VisitEntryNumber,
                        SwipeTime = visit.VisitEntryDate,
                        RedisId = visit.Id,
                        ObjectType = typeof(VisitModel).ToString(),
                        ObjectJson = JsonConvert.SerializeObject(visit)
                    });

                }

                /*
                Logger.WarnFormat("Inserting {0}, {1}, {2}, {3}, {4} with idx {5}", scan.Barcode, scan.Period,
                    scan.AttendanceCode, scan.EntryTime, scan.Location, scan.Id);
                    */

                InsertObject(visit.Id, visit);

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
                            StudentNumber = visit.VisitEntryNumber,
                            SwipeTime = visit.VisitEntryDate,
                            RedisId = 0,
                            ObjectType = typeof(VisitModel).ToString(),
                            ObjectJson = JsonConvert.SerializeObject(visit)
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
                            StudentNumber = visit.VisitEntryNumber,
                            SwipeTime = visit.VisitEntryDate,
                            RedisId = 0,
                            ObjectType = typeof(VisitModel).ToString(),
                            ObjectJson = JsonConvert.SerializeObject(visit)
                        });
                    }
                }
                catch (Exception iex) { Logger.Error(iex); }

                Logger.Error(ex);

            }

            return visit;
        }

        public Task InsertObjectAsync(VisitModel value)
        {
            throw new NotImplementedException();
        }


        public Task RemoveObject(long key)
        {
            throw new NotImplementedException();
        }

        public long[] FindByDate(DateTime date, out string urn)
        {
            urn = string.Format(GroupUri, date.Date.ToString("MM-dd-yyyy"));
            var items = Client.GetAllItemsFromSet(urn);

            var list = items.Select(long.Parse).ToArray();

            return list;
        }

        public long[] FindNotSynced(out string urn)
        {
            urn = SyncUri;
            var items = Client.GetAllItemsFromSet(urn);

            var list = items.Select(long.Parse).ToArray();

            return list;
        }

        public void RemoveFromSet(long id, string urn)
        {
            base.Client.RemoveItemFromSet(urn, id.ToString());
        }

    }
}
