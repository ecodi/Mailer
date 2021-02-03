using Mailer.Mapping;
using Mailer.Domain.Models;
using Mailer.Domain.Repositories;
using Mailer.Infrastructure.Models;
using Mailer.Infrastructure.Types;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mailer.Infrastructure.Repositories
{
    public class SenderRepository : GenericRepository<Sender, SenderDbModel>, IMongoRepository<SenderDbModel>, ISenderRepository
    {
        public SenderRepository(IMongoContext context, IMapper mapper) : base(context, mapper) { }

        public virtual Task<Sender?> GetByEmailAddressAsync(IMailerContext context, string emailAddress)
        {
            return GetDbOneAsync(context, s => s.EmailAddress.Hash == Mapper.Map<string, EncryptedString>(emailAddress).Hash);
        }

        protected override IEnumerable<CreateIndexModel<SenderDbModel>> IndexesDefinitions()
        {
            return new[]
            {
                new CreateIndexModel<SenderDbModel>(
                    new IndexKeysDefinitionBuilder<SenderDbModel>().Ascending(s => s.EmailAddress.Hash),
                    new CreateIndexOptions<SenderDbModel> { Unique = true,
                        PartialFilterExpression = Builders<SenderDbModel>.Filter.Where(s => s.Deleted == false) }),
            };
        }
    }
}
