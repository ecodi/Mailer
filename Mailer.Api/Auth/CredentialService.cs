using Mailer.Infrastructure.Security;
using Mailer.Security;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mailer.Api.Auth
{
    public class CredentialService : ICredentialService
    {
        private readonly IReadOnlyCollection<User> _users;
        private readonly ICipherService _protector;

        public CredentialService(IDbProtector protector, IOptions<UsersList> optionsAccessor)
        {
            _users = optionsAccessor.Value;
            _protector = protector;
        }

        public Task<User?> GetUserAsync(string login, string password, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var user = _users?.FirstOrDefault(u => u.Verify(login, _protector.Hash(password)));
            return Task.FromResult(user);
        }
    }
}
