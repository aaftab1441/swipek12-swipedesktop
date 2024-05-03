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
    public class VisitExitStorage : RedisStorage<long>, IStorage<VisitExit, long>
    {
      
        private static readonly ILog Logger = LogManager.GetLogger(typeof(VisitExitStorage));

        private static string SetUrn = "urn:Set:{0}";

        //private static string StudentUrn = "urn:Set:{0}:{1}";

        private static string DateUrn = "urn:Set:{0}:{1}";

        public static readonly string SyncUri = string.Format("urn:Set:Sync:{0}", typeof(LocationScan));

        private readonly IRedisTypedClient<VisitExit> _typedClient;

        public IRedisTypedClient<VisitExit> RedisClient { get { return _typedClient; } }

        public VisitExitStorage()
        {
            _typedClient = Client.As<VisitExit>();
        }

        public VisitExit GetById(long key)
        {         
            return _typedClient.GetById(key);
        }

        public Task<VisitExit> GetObjectAsync(long key)
        {
            throw new NotImplementedException();
        }

        public void InsertObject(long key, VisitExit item)
        {
            //does scan exist already

            _typedClient.Store(item);

            //add to student location index
            //StudentUrn = string.Format(StudentUrn, typeof(LocationScan), scan.StudentNumber);
            //Client.AddItemToSet(StudentUrn, key.ToString(CultureInfo.CurrentCulture));

            //add to date index
            DateUrn = string.Format(DateUrn, typeof(VisitExit), item.DateExited.Date.ToString("MM-dd-yyyy"));
            Client.AddItemToSet(DateUrn, key.ToString(CultureInfo.CurrentCulture));

            //add to exit index
            SetUrn = string.Format(SetUrn, item.VisitId);
            Client.AddItemToSet(SetUrn, key.ToString(CultureInfo.CurrentCulture));

            //add to set to find records needing sync to server
            Client.AddItemToSet(SyncUri, key.ToString(CultureInfo.CurrentCulture));
          
        }

        public Task InsertObjectAsync(long key, VisitExit scan)
        {
            throw new NotImplementedException();
        }

        public VisitExit InsertObject(VisitExit o, bool isNew = true)
        {
            try
            {
                if (isNew)
                {
                    o.Id = _typedClient.GetNextSequence(); //GetNextIdentityValue();

                    BackupRecord(new
                    {
                        Settings.Default.SchoolId,
                        StudentNumber = o.VisitNumber,
                        SwipeTime = o.DateExited,
                        RedisId = o.Id,
                        ObjectType = typeof(VisitExit).ToString(),
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
                            StudentNumber = o.VisitNumber,
                            SwipeTime = o.DateExited,
                            RedisId = 0,
                            ObjectType = typeof(VisitExit).ToString(),
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
                            StudentNumber = o.VisitNumber,
                            SwipeTime = o.DateExited,
                            RedisId = 0,
                            ObjectType = typeof(VisitExit).ToString(),
                            ObjectJson = JsonConvert.SerializeObject(o)
                        });
                    }
                }
                catch (Exception iex) { Logger.Error(iex); }

                Logger.Error(ex);

            }

            return o;
        }

        public Task InsertObjectAsync(VisitExit o)
        {
            throw new NotImplementedException();
        }


        public Task RemoveObject(long key)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<VisitExit> GetItemsByDate(DateTime date)
        {
            var urn = string.Format(DateUrn, typeof(VisitExit), date.Date.ToString("MM-dd-yyyy"));
            var items = Client.GetAllItemsFromSet(urn);

            var list = items.Select(long.Parse).ToArray();

            return Client.GetByIds<VisitExit>(list);
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
            urn = string.Format(DateUrn, typeof(VisitExit), date.Date.ToString("MM-dd-yyyy"));
            var items = Client.GetAllItemsFromSet(urn);

            var list = items.Select(long.Parse).ToArray();

            return list;
        }
    }
}
