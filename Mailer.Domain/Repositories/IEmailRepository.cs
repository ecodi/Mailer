using Mailer.Domain.Models;
using System;
using System.Collections.Generic;

namespace Mailer.Domain.Repositories
{
    public interface IEmailRepository : IDbRepository<Email>
    {
        IAsyncEnumerable<Email> GetListByMailingAsync(IMailerContext context, Guid mailingId, int? limit = null);
    }
}
