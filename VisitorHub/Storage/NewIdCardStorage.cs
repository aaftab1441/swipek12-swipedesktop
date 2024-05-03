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
    public class IdCardStorage : RedisStorage<long>, IStorage<NewIdCard, long>
    {

        private static readonly ILog Logger = LogManager.GetLogger(typeof(IdCardStorage));

        private static string StandardUri = "urn:Set:{0}:{1}";

        public static readonly string SyncUri = string.Format("urn:Set:Sync:{0}", typeof(NewIdCard));

        private readonly IRedisTypedClient<NewIdCard> _typedClient;

        public IRedisTypedClient<NewIdCard> RedisClient { get { return _typedClient; } }

        public IdCardStorage()
        {
            _typedClient = Client.As<NewIdCard>();
        }

        public NewIdCard GetById(long key)
        {         
            return _typedClient.GetById(key);
        }

        public Task<NewIdCard> GetObjectAsync(long key)
        {
            throw new NotImplementedException();
        }

        public void InsertObject(long key, NewIdCard fine)
        {
           
            _typedClient.Store(fine);

            Client.AddItemToSet(string.Format(StandardUri, typeof(NewIdCard), fine.PrintDate.Date.ToString("MM-dd-yyyy")), key.ToString(CultureInfo.CurrentCulture));

            //add to location index
           
            Client.AddItemToSet(string.Format(StandardUri, typeof(NewIdCard), fine.StudentNumber), key.ToString(CultureInfo.CurrentCulture));

            //add to set to find records needing sync to server
            Client.AddItemToSet(SyncUri, key.ToString(CultureInfo.CurrentCulture));
          
        }

        public Task InsertObjectAsync(long key, NewIdCard scan)
        {
            throw new NotImplementedException();
        }

        public NewIdCard InsertObject(NewIdCard o, bool isNew = true)
        {
            try
            {
                if (isNew)
                {
                    o.Id = _typedClient.GetNextSequence(); //GetNextIdentityValue(); //

                    BackupRecord(new
                    {
                        Settings.Default.SchoolId,
                        o.StudentNumber,
                        SwipeTime = o.PrintDate,
                        RedisId = o.Id,
                        ObjectType = typeof(NewIdCard).ToString(),
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
                            SwipeTime = o.PrintDate,
                            RedisId = 0,
                            ObjectType = typeof(NewIdCard).ToString(),
                            ObjectJson = JsonConvert.SerializeObject(o)
                        });
                    }
                }
                catch (Exception iex) { Logger.Error(iex); }
            }
            catch (Exception ex) { Logger.Error(ex); }

            return o;
        }

        public Task InsertObjectAsync(NewIdCard o)
        {
            throw new NotImplementedException();
        }


        public Task RemoveObject(long key)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<NewIdCard> GetItemsByStudent(DateTime date, string student)
        {
            var urn = string.Format(StandardUri, typeof (NewIdCard), date.Date.ToString("MM-dd-yyyy"));
            var items = Client.GetAllItemsFromSet(urn);

            var list = items.Select(long.Parse).ToArray();

            return Client.GetByIds<NewIdCard>(list).Where(x => x.StudentNumber == student);
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
            urn = string.Format(StandardUri, typeof(NewIdCard), date.Date.ToString("MM-dd-yyyy"));
            var items = Client.GetAllItemsFromSet(urn);

            var list = items.Select(long.Parse).ToArray();

            return list;
        }
    }
}
