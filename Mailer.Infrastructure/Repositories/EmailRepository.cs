using Mailer.Mapping;
using Mailer.Domain.Models;
using Mailer.Domain.Repositories;
using Mailer.Infrastructure.Models;
using System;
using System.Collections.Generic;

namespace Mailer.Infrastructure.Repositories
{
    public class EmailRepository : GenericRepository<Email, EmailDbModel>, IMongoRepository<EmailDbModel>, IEmailRepository
    {
        public EmailRepository(IMongoContext context, IMapper mapper) : base(context, mapper) { }

        public virtual IAsyncEnumerable<Email> GetListByMailingAsync(IMailerContext context, Guid mailingId, int? limit = null)
        {
            return GetDbListAsync(context, e => e.MailingId == mailingId, limit);
        }
    }
}
