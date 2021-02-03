using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;

namespace Mailer.Api
{
    public static class HttpContextExtensions
    {
        public static IMailerContext GetMailerContext(this HttpContext httpContext, CancellationToken cancellationToken)
        {
            var userId = httpContext.User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return new MailerContext(userId is null ? Guid.Empty : new Guid(userId), cancellationToken);
        }
    }
}
