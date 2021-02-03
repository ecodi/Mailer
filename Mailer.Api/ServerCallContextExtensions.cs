using Grpc.Core;

namespace Mailer.Api
{
    public static class ServerCallContextExtensions
    {
        public static IMailerContext GetMailerContext(this ServerCallContext callContext)
        {
            return callContext.GetHttpContext().GetMailerContext(callContext.CancellationToken);
        }
    }
}
