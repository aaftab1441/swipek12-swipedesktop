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
    public class DetentionStorage : RedisStorage<long>, IStorage<Consequence, long>
    {

        private static readonly ILog Logger = LogManager.GetLogger(typeof(DetentionStorage));

        private static string DateUrn = "urn:Set:{0}:{1}";
        
        private static readonly string SyncUri = string.Format("urn:Set:Sync:{0}", typeof(Consequence));

        private readonly IRedisTypedClient<Consequence> _typedClient;

        public IRedisTypedClient<Consequence> RedisClient { get { return _typedClient; } }

        public DetentionStorage()
        {
            //ModelConfig<ScanModel>.Id(x => x.Id);

            _typedClient = Client.As<Consequence>();
        }

        public Consequence GetById(long key)
        {         
            return _typedClient.GetById(key);
        }

        public Task<Consequence> GetObjectAsync(long key)
        {
            throw new NotImplementedException();
        }

        public void InsertObject(long key, Consequence detention)
        {
            //does scan exist already

            _typedClient.Store(detention);


            DateUrn = string.Format(DateUrn,  typeof(Consequence), detention.InfractionDate.Date.ToString("MM-dd-yyyy"));

            Client.AddItemToSet(DateUrn, key.ToString(CultureInfo.CurrentCulture));

            //add to set to find records needing sync to server
            Client.AddItemToSet(SyncUri, key.ToString(CultureInfo.CurrentCulture));

            try
            {
                Client.Save();
            }
            catch (Exception ex) { Logger.Error(ex); }
          
        }

        public Task InsertObjectAsync(long key, Consequence scan)
        {
            throw new NotImplementedException();
        }

        public Consequence InsertObject(Consequence o, bool backup = true)
        {
            try
            {
                if (backup)
                {
                    o.Id = _typedClient.GetNextSequence(); //GetNextIdentityValue(); //

                    BackupRecord(new
                    {
                        Settings.Default.SchoolId,
                        o.StudentNumber,
                        SwipeTime = o.InfractionDate,
                        RedisId = o.Id,
                        ObjectType = typeof(Consequence).ToString(),
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
                    BackupRecord(new
                    {
                        Settings.Default.SchoolId,
                        o.StudentNumber,
                        SwipeTime = o.InfractionDate,
                        RedisId = o.Id,
                        ObjectType = typeof(Consequence).ToString(),
                        ObjectJson = JsonConvert.SerializeObject(o)
                    });
                }
                catch (Exception iex) { Logger.Error(iex); }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);

                try
                {
                    BackupRecord(new
                    {
                        Settings.Default.SchoolId,
                        o.StudentNumber,
                        SwipeTime = o.InfractionDate,
                        RedisId = o.Id,
                        ObjectType = typeof(Consequence).ToString(),
                        ObjectJson = JsonConvert.SerializeObject(o)
                    });
                }
                catch (Exception iex) { Logger.Error(iex); }
           
            }

            return o;
        }

        public Task InsertObjectAsync(Consequence o)
        {
            throw new NotImplementedException();
        }


        public Task RemoveObject(long key)
        {
            throw new NotImplementedException();
        }

        public long[] FindByDate(DateTime date, out string urn)
        {
            urn = string.Format(DateUrn,  typeof(Consequence), date.Date.ToString("MM-dd-yyyy"));
            var items = Client.GetAllItemsFromSet(urn);

            var list = items.Select(long.Parse).ToArray();

            return list;
        }

        public IEnumerable<Consequence> GetItemsByDate(DateTime date)
        {
            var urn = string.Format(DateUrn,  typeof(Consequence), date.Date.ToString("MM-dd-yyyy"));
            var items = Client.GetAllItemsFromSet(urn);

            var list = items.Select(Guid.Parse).ToArray();

            return Client.GetByIds<Consequence>(list);
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
