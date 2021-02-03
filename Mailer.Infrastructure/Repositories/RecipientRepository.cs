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
    public class RecipientRepository : GenericRepository<Recipient, RecipientDbModel>, IMongoRepository<RecipientDbModel>, IRecipientRepository
    {
        public RecipientRepository(IMongoContext context, IMapper mapper) : base(context, mapper) { }

        public virtual Task<Recipient?> GetByEmailAddressAsync(IMailerContext context, string emailAddress)
        {
            return GetDbOneAsync(context, r => r.EmailAddress.Hash == Mapper.Map<string, EncryptedString>(emailAddress).Hash);
        }

        protected override IEnumerable<CreateIndexModel<RecipientDbModel>> IndexesDefinitions()
        {
            return new[]
            {
                new CreateIndexModel<RecipientDbModel>(
                    new IndexKeysDefinitionBuilder<RecipientDbModel>().Ascending(s => s.EmailAddress.Hash),
                    new CreateIndexOptions<RecipientDbModel> { Unique = true,
                        PartialFilterExpression = Builders<RecipientDbModel>.Filter.Where(s => s.Deleted == false) }),
            };
        }
    }
}
