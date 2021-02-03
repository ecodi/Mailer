using Mailer.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mailer.Domain.Repositories
{
    public interface IMailingRepository : IDbRepository<Mailing>
    {
        Task<bool> ReplaceOnStatusCodeAsync(IMailerContext context, Mailing record, MailingStatusCode mailingStatusCode);
        IAsyncEnumerable<Mailing> GetListByRecipientAsync(IMailerContext context, Recipient recipient, int? limit);
        IAsyncEnumerable<Mailing> GetListBySenderAsync(IMailerContext context, Sender sender, int? limit);
    }
}
