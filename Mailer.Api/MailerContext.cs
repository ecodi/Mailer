using System;
using System.Threading;

namespace Mailer.Api
{
    public class MailerContext : IMailerContext
    {
        public CancellationToken CancellationToken { get; }
        public Guid UserId { get; }

        public MailerContext(Guid userId, CancellationToken cancellationToken)
        {
            UserId = userId;
            CancellationToken = cancellationToken;
        }
    }
}
