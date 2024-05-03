using log4net;
using Newtonsoft.Json;
using ServiceStack.Redis;
using ServiceStack.Redis.Generic;
using SwipeDesktop.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwipeDesktop.Storage
{
    public class EventStorage : RedisStorage<long>
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(EventStorage));
        
        public static readonly string SyncUri = string.Format("urn:Set:Sync:{0}", typeof(GenericEvent<>));

        private readonly IRedisTypedClient<ISwipeEvent> _typedClient;

        public IRedisTypedClient<ISwipeEvent> RedisClient { get { return _typedClient; } }

        public EventStorage()
        {
            _typedClient = Client.As<ISwipeEvent>();
        }

        public ISwipeEvent GetById(long key)
        {
            return _typedClient.GetById(key);
        }

        public void Insert(long key, ISwipeEvent o)
        {
            o.Id = key;

            _typedClient.Store(o);

           
            try
            {
                Client.Save();
            }
            catch (Exception ex) { Logger.Error(ex); }

        }


        public ISwipeEvent InsertObject(ISwipeEvent o, bool isNew = true)
        {
            try
            {
                if (isNew)
                {
                    o.Id = _typedClient.GetNextSequence(); //GetNextIdentityValue();

                    BackupRecord(new
                    {
                        Settings.Default.SchoolId,
                        StudentNumber = string.Empty,
                        SwipeTime = DateTime.Now,
                        RedisId = o.Id,
                        ObjectType = o.GetType().ToString(),
                        ObjectJson = JsonConvert.SerializeObject(o)
                    });
                }
                //Client.Save();

                Insert(o.Id, o);
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
                            StudentNumber = string.Empty,
                            SwipeTime = DateTime.Now,
                            RedisId = 0,
                            ObjectType = o.GetType().ToString(),
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
                            StudentNumber = string.Empty,
                            SwipeTime = DateTime.Now,
                            RedisId = 0,
                            ObjectType = o.GetType().ToString(),
                            ObjectJson = JsonConvert.SerializeObject(o)
                        });
                    }
                }
                catch (Exception iex) { Logger.Error(iex); }

                Logger.Error(ex);

            }

            return o;
        }
    }
}
