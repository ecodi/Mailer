using Mailer.Domain.Models;
using System.Threading.Tasks;

namespace Mailer.Domain.Services
{
    public interface IMailingService : IService<Mailing>
    {
        Task ExecuteAsync(IMailerContext context, Mailing model);
        Task ProcessAsync(IMailerContext context, Mailing model);
    }
}
