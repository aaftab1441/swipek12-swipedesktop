using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.Redis.Generic;

namespace SwipeDesktop.Storage
{
    public interface IStorage<TModel, TId>
    {
        IRedisTypedClient<TModel> RedisClient { get; }

        Task<TModel> GetObjectAsync(TId key);

        TModel GetById(TId key);

        Task InsertObjectAsync(TId key, TModel value);

        void InsertObject(TId key, TModel value);

        Task InsertObjectAsync(TModel value);

        TModel InsertObject(TModel value, bool backup = true);

        Task RemoveObject(TId key);

        TId[] FindByDate(DateTime date, out string urn);

        void AddToSet(long id, string urn);
    }
}
