using Mailer.Domain.Models;
using System.Threading.Tasks;

namespace Mailer.Domain.Application
{
    public interface IMailClient
    {
        Task SendEmailAsync(Email email);
    }
}
