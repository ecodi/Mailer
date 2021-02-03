using Mailer.Domain.Models;
using System.Threading.Tasks;

namespace Mailer.Domain.Repositories
{
    public interface IRecipientRepository : IDbRepository<Recipient>
    {
        Task<Recipient?> GetByEmailAddressAsync(IMailerContext context, string emailAddress);
    }
}
