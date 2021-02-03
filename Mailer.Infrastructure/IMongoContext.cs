using MongoDB.Driver;

namespace Mailer.Infrastructure
{
    public interface IMongoContext
    {
        IMongoDatabase Db { get; }
        IMongoCollection<TDbModel> GetCollection<TDbModel>();
    }
}
