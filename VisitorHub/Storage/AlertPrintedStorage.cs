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
    public class AlertPrintedStorage : RedisStorage<long>, IStorage<AlertPrinted, long>
    {

        private static readonly ILog Logger = LogManager.GetLogger(typeof(AlertPrintedStorage));

        private static string DateUrn = "urn:Set:{0}:{1}";
        
        private static readonly string SyncUri = string.Format("urn:Set:Sync:{0}", typeof(AlertPrinted));

        private readonly IRedisTypedClient<AlertPrinted> _typedClient;

        public IRedisTypedClient<AlertPrinted> RedisClient { get { return _typedClient; } }

        public AlertPrintedStorage()
        {
            //ModelConfig<ScanModel>.Id(x => x.Id);

            _typedClient = Client.As<AlertPrinted>();
        }

        public AlertPrinted GetById(long key)
        {         
            return _typedClient.GetById(key);
        }

        public Task<AlertPrinted> GetObjectAsync(long key)
        {
            throw new NotImplementedException();
        }

        public void InsertObject(long key, AlertPrinted obj)
        {
            //does scan exist already

            _typedClient.Store(obj);


            DateUrn = string.Format(DateUrn, typeof(AlertPrinted), obj.DatePrinted.Date.ToString("MM-dd-yyyy"));

            Client.AddItemToSet(DateUrn, key.ToString(CultureInfo.CurrentCulture));

            //add to set to find records needing sync to server
            Client.AddItemToSet(SyncUri, key.ToString(CultureInfo.CurrentCulture));

            try
            {
                Client.Save();
            }
            catch (Exception ex) { Logger.Error(ex); }
          
        }

        public Task InsertObjectAsync(long key, AlertPrinted scan)
        {
            throw new NotImplementedException();
        }

        public AlertPrinted InsertObject(AlertPrinted o, bool backup = true)
        {
      
            try
            {
                if (backup)
                {
                    o.Id = _typedClient.GetNextSequence(); //GetNextIdentityValue(); //
                    /* clear image to object can be serialized*/
                    o.StudentImage = null;
                    BackupRecord(new
                    {
                        Settings.Default.SchoolId,
                        o.StudentNumber,
                        SwipeTime = o.DatePrinted,
                        RedisId = o.Id,
                        ObjectType = typeof(AlertPrinted).ToString(),
                        ObjectJson = JsonConvert.SerializeObject(o)
                    });
                }

                InsertObject(o.Id, o);
                //Client.Save();
            }
            catch (RedisResponseException rre)
            {
                Logger.Error(rre);
                try
                {
                    if (backup)
                    {
                        
                        /* clear image to object can be serialized*/
                        o.StudentImage = null;
                        BackupRecord(new
                        {
                            Settings.Default.SchoolId,
                            o.StudentNumber,
                            SwipeTime = o.DatePrinted,
                            RedisId = 0,
                            ObjectType = typeof(AlertPrinted).ToString(),
                            ObjectJson = JsonConvert.SerializeObject(o)
                        });
                    }
                }
                catch (Exception iex) { Logger.Error(iex); }
            }
            catch (Exception ex) { Logger.Error(ex); }

            return o;
        }

        public Task InsertObjectAsync(AlertPrinted o)
        {
            throw new NotImplementedException();
        }


        public Task RemoveObject(long key)
        {
            throw new NotImplementedException();
        }

        public void UpdateObject(AlertPrinted record)
        {
            var key = record.Id;
            _typedClient.SetEntry(key.ToString(CultureInfo.CurrentCulture), record);

            Client.AddItemToSet(SyncUri, key.ToString(CultureInfo.CurrentCulture));
        }

        public long[] FindByDate(DateTime date, out string urn)
        {
            urn = string.Format(DateUrn, typeof(AlertPrinted), date.Date.ToString("MM-dd-yyyy"));
            var items = Client.GetAllItemsFromSet(urn);

            var list = items.Select(long.Parse).ToArray();

            return list;
        }

        public IEnumerable<AlertPrinted> GetItemsByDate(DateTime date)
        {
            var urn = string.Format(DateUrn, typeof(AlertPrinted), date.Date.ToString("MM-dd-yyyy"));
            var items = Client.GetAllItemsFromSet(urn);

            var list = items.Select(long.Parse).ToArray();

            return Client.GetByIds<AlertPrinted>(list);
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
