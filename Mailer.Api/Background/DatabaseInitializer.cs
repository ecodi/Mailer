using Mailer.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mailer.Api.Background
{
    public class DatabaseInitializer : IHostedService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public DatabaseInitializer(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var repositories = scope.ServiceProvider.GetServices<IMongoRepository>();
            await Task.WhenAll(repositories.Select(r => r.CreateIndexesAsync(stoppingToken)));
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
