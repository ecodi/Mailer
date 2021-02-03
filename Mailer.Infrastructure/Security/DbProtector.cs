using Mailer.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Mailer.Infrastructure.Security
{
    public class DbProtector : CipherService, IDbProtector
    {
        public DbProtector(IDataProtectionProvider provider, IOptions<HashOptions> optionsAccessor)
            : base(provider, optionsAccessor.Value, "database") { }
    }
}
