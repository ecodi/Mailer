using Mailer.Mapping;
using Mailer.Domain.Models;
using Mailer.Domain.Repositories;
using Mailer.Infrastructure.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Mailer.Infrastructure.Repositories
{
    public class MailingRepository : GenericRepository<Mailing, MailingDbModel>, IMongoRepository<MailingDbModel>, IMailingRepository
    {
        private readonly IMongoRepository<RecipientDbModel> _recipientRepository;
        private readonly IMongoRepository<SenderDbModel> _senderRepository;

        public MailingRepository(IMongoContext context, IMapper mapper, IMongoRepository<RecipientDbModel> recipientRepository, IMongoRepository<SenderDbModel> senderRepository)
            : base(context, mapper)
        {
            _recipientRepository = recipientRepository;
            _senderRepository = senderRepository;
        }

        public override async Task<Mailing?> GetByIdAsync(IMailerContext context, Guid id)
        {
            var result = await PrepareQuery(GetCollection().Aggregate().Match(CreateKeyFilter(id)).Limit(1)).FirstOrDefaultAsync(context.CancellationToken);
            return Mapper.Map<FullMailingDbModel, Mailing>(result);
        }
        protected override async IAsyncEnumerable<Mailing> GetDbListAsync(IMailerContext context, Expression<Func<MailingDbModel, bool>>? filter, int? limit = null)
        {
            var collection = GetCollection().Aggregate();
            if (filter != null) collection = collection.Match(AdjustFilter(filter));
            if (limit.HasValue) collection = collection.Limit(limit.Value);
            using var asyncCursor = await PrepareQuery(collection).ToCursorAsync(context.CancellationToken);
            while (await asyncCursor.MoveNextAsync())
            {
                foreach (var result in asyncCursor.Current)
                    yield return Mapper.Map<FullMailingDbModel, Mailing>(result);
            }
        }

        public IAsyncEnumerable<Mailing> GetListByRecipientAsync(IMailerContext context, Recipient recipient, int? limit)
            => GetDbListAsync(context, m => m.RecipientsIds!.Contains(recipient.Id), limit);
        public IAsyncEnumerable<Mailing> GetListBySenderAsync(IMailerContext context, Sender sender, int? limit)
            => GetDbListAsync(context, m => m.SenderId == sender.Id, limit);


        public async Task<bool> ReplaceOnStatusCodeAsync(IMailerContext context, Mailing model, MailingStatusCode mailingStatusCode)
        {
            var result = await GetCollection().FindOneAndReplaceAsync(m => m.Id == model.Id && m.Status!.StatusCode == mailingStatusCode, GetDbModel(context, model), cancellationToken: context.CancellationToken);
            return result != null;
        }

        private IAggregateFluent<FullMailingDbModel> PrepareQuery(IAggregateFluent<MailingDbModel> collection)
        {
            return collection
               .Lookup<MailingDbModel, SenderDbModel, FullMailingDbModel>(_senderRepository.GetCollection(),
                   x => x.SenderId,
                   y => y.Id,
                   y => y.Senders
                ).Lookup(_recipientRepository.GetCollection(), x => x.RecipientsIds,
                    x => x.Id, (FullMailingDbModel pr) => pr.Recipients);
        }


        private class FullMailingDbModel : MailingDbModel
        {
            public IEnumerable<RecipientDbModel> Recipients { get; set; } = default!;
            public IEnumerable<SenderDbModel> Senders { get; set; } = default!;
        }

        public class Mapping : BaseMapping
        {
            public Mapping()
            {
                CreateMap<FullMailingDbModel, Mailing>()
                    .IncludeBase<MailingDbModel, Mailing>()
                    .ForMember(m => m.Recipients, e => e.MapFrom(m => m.Recipients))
                    .ForMember(m => m.Sender, e => e.MapFrom(m => m.Senders == null ? null : m.Senders.FirstOrDefault()));
            }
        }
    }
}
