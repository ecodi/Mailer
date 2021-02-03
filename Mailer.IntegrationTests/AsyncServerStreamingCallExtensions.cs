using Grpc.Core;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mailer.IntegrationTests
{
    public static class AsyncServerStreamingCallExtensions
    {
        public static async Task<List<TResponse>> LoadAsync<TResponse>(this AsyncServerStreamingCall<TResponse> call)
            => await call.ResponseStream.ReadAllAsync().ToListAsync();
    }
}
