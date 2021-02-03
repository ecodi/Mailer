using Mailer.Domain.Models;
using System.Threading.Tasks;

namespace Mailer.Domain.Repositories
{
    public interface ISenderRepository : IDbRepository<Sender>
    {
        Task<Sender?> GetByEmailAddressAsync(IMailerContext context, string emailAddress);
    }
}
