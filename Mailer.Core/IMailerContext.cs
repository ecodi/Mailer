using System;
using System.Threading;

namespace Mailer
{
    public interface IMailerContext
    {
        CancellationToken CancellationToken { get; }
        Guid UserId { get; }
    }
}
