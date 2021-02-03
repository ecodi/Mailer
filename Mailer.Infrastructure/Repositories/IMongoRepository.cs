using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;

namespace Mailer.Infrastructure.Repositories
{
    public interface IMongoRepository
    {
        Task CreateIndexesAsync(CancellationToken cancellationToken);
    }

    public interface IMongoRepository<TDbModel> : IMongoRepository
    {
        IMongoCollection<TDbModel> GetCollection();
    }
}
