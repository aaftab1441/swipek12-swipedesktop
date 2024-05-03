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
    public class FineStorage : RedisStorage<long>, IStorage<AssessedFine, long>
    {

        private static readonly ILog Logger = LogManager.GetLogger(typeof(FineStorage));

        private static string StandardUri = "urn:Set:{0}:{1}";

        public static readonly string SyncUri = string.Format("urn:Set:Sync:{0}", typeof(AssessedFine));

        private readonly IRedisTypedClient<AssessedFine> _typedClient;

        public IRedisTypedClient<AssessedFine> RedisClient { get { return _typedClient; } }

        public FineStorage()
        {
            _typedClient = Client.As<AssessedFine>();
        }

        public AssessedFine GetById(long key)
        {         
            return _typedClient.GetById(key);
        }

        public Task<AssessedFine> GetObjectAsync(long key)
        {
            throw new NotImplementedException();
        }

        public void InsertObject(long key, AssessedFine fine)
        {
           
            _typedClient.Store(fine);

            Client.AddItemToSet(string.Format(StandardUri, typeof(AssessedFine), fine.FineDate.Date.ToString("MM-dd-yyyy")), key.ToString(CultureInfo.CurrentCulture));

            //add to location index
           
            Client.AddItemToSet(string.Format(StandardUri, typeof(AssessedFine), fine.StudentNumber), key.ToString(CultureInfo.CurrentCulture));

            //add to set to find records needing sync to server
            Client.AddItemToSet(SyncUri, key.ToString(CultureInfo.CurrentCulture));
          
        }

        public Task InsertObjectAsync(long key, AssessedFine scan)
        {
            throw new NotImplementedException();
        }

        public AssessedFine InsertObject(AssessedFine o, bool isNew = true)
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
                        SwipeTime = o.FineDate,
                        RedisId = o.Id,
                        ObjectType = typeof(AssessedFine).ToString(),
                        ObjectJson = JsonConvert.SerializeObject(o)
                    });
                }
                //Client.Save();

                InsertObject(o.Id, o);
                //Logger.InfoFormat("Snapshot recorded {0}.", Client.LastSave);
            }
            catch (Exception ex) { Logger.Error(ex); }

            return o;
        }

        public Task InsertObjectAsync(AssessedFine o)
        {
            throw new NotImplementedException();
        }


        public Task RemoveObject(long key)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<AssessedFine> GetItemsByStudent(DateTime date, string student)
        {
            var urn = string.Format(StandardUri, student, date.Date.ToString("MM-dd-yyyy"));
            var items = Client.GetAllItemsFromSet(urn);

            var list = items.Select(long.Parse).ToArray();

            return Client.GetByIds<AssessedFine>(list).Where(x => x.StudentNumber == student);
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
            urn = string.Format(StandardUri, typeof(AssessedFine), date.Date.ToString("MM-dd-yyyy"));
            var items = Client.GetAllItemsFromSet(urn);

            var list = items.Select(long.Parse).ToArray();

            return list;
        }
    }
}
