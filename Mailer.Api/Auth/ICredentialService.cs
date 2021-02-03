using System.Threading;
using System.Threading.Tasks;

namespace Mailer.Api.Auth
{
    public interface ICredentialService
    {
        Task<User?> GetUserAsync(string login, string password, CancellationToken cancellationToken = default);
    }
}
