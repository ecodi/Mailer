using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Mailer.Api.Controllers;
using Mailer.Domain.Repositories;
using Mailer.Domain.Models;
using System.Linq;
using Mailer.Api;
using Mailer.Domain.Application;
using Mailer.Infrastructure.Connectors;
using Microsoft.Net.Http.Headers;
using Grpc.Net.Client;
using Grpc.Core;

namespace Mailer.IntegrationTests.Fixtures
{
    public static class RandomExtensions
    {
        public static string MachineStamp(this Random random)
        {
            var creationTimeStamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            var randomBit = random.Next(0, 999999).ToString("D6");
            return $"{creationTimeStamp}{randomBit}";
        }
    }

    public class ApiFixture : IDisposable
    {
        private class DatabaseSeeder : IHostedService
        {
            private readonly IServiceScopeFactory _serviceScopeFactory;

            public DatabaseSeeder(IServiceScopeFactory serviceScopeFactory)
            {
                _serviceScopeFactory = serviceScopeFactory;
            }

            public async Task StartAsync(CancellationToken stoppingToken)
            {
                using var serviceScope = _serviceScopeFactory.CreateScope();
                var context = new TestContext();

                var recipientsRepository = serviceScope.ServiceProvider.GetService<IRecipientRepository>();
                var recipients = new[]
                {
                    new Recipient { EmailAddress = "email@sink.sendgrid.net", FirstName = "Mark", LastName = "Twain" },
                    new Recipient { EmailAddress = "email2@sink.sendgrid.net", FirstName = "Stephen", LastName = "Dark" },
                    new Recipient { EmailAddress = "email3@sink.sendgrid.net", FirstName = "Will be", LastName = "Updated", RowVersion = 3 },
                    new Recipient { EmailAddress = "todelete@sink.sendgrid.net", FirstName = "Will be", LastName = "Deleted" }
                };
                await Task.WhenAll(recipients.Select(r => recipientsRepository.InsertAsync(context, r)));

                var sendersRepository = serviceScope.ServiceProvider.GetService<ISenderRepository>();
                var senders = new[]
                {
                    new Sender { EmailAddress = "sender@email.com", Name = "Mailer" },
                    new Sender { EmailAddress = "sender2@email.com", Name = "To update", RowVersion = 4 },
                    new Sender { EmailAddress = "todelete@email.com", Name = "To delete" }
                };
                await Task.WhenAll(senders.Select(r => sendersRepository.InsertAsync(context, r)));

                var mailingsRepository = serviceScope.ServiceProvider.GetService<IMailingRepository>();
                var mailings = new[]
                {
                    new Mailing { Recipients =  recipients.Take(2).ToList(), Sender = senders.First(), SubjectTemplate = "read only", PlainBodyTemplate = "And body" },
                    new Mailing { Recipients =  recipients.Take(1).ToList(), Sender = senders.First(), SubjectTemplate = "to update" },
                    new Mailing { Recipients =  recipients.Take(1).ToList(), Sender = senders.First(), SubjectTemplate = "to delete" }
                };
                await Task.WhenAll(mailings.Select(r => mailingsRepository.InsertAsync(context, r)));
            }

            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private class TestContext : IMailerContext
        {
            public CancellationToken CancellationToken => CancellationToken.None;
            public Guid UserId => new Guid("3a5a5b3f-f495-46a0-b428-c1993cfce4dc");
        }


        public TestServer Server { get; }
        public GrpcChannel Channel { get; }
        public GrpcChannel AuthChannel { get; }
        public IMailerContext Context { get; } = new TestContext();
        public ApiFixture()
        {
            var testEnvId = $"{new Random().MachineStamp()}_";
            var configJson = $@"
{{
    ""DataProtection"": {{
        ""ApplicationName"": ""mailer.{testEnvId}.tests""
    }},
    ""Mongo"": {{
        ""CollectionNameSuffix"": ""t{testEnvId}t""
    }},
    ""RabbitMQ"": {{
        ""Mailing"": {{
            ""QueueName"": ""testmailer.{testEnvId}.mailing""
        }}
    }}
}}
";
            Server = new TestServer(WebHost.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config
                        .AddJsonFile("users.json")
                        .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(configJson)))
                        .AddEnvironmentVariables();
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseEnvironment(Environments.Staging)
                .UseStartup<Startup>()
                .ConfigureTestServices(services => {
                    services.AddMvcCore().AddApplicationPart(typeof(RecipientsController).Assembly);
                    services
                        .AddSingleton<DummyMailClient>()
                        .AddScoped<SendGridMailClient>()
                        .AddScoped<IMailClient>(s => s.GetRequiredService<DummyMailClient>())
                        .AddHostedService<DatabaseSeeder>();
                }));
            Server.BaseAddress = new UriBuilder(Server.BaseAddress) { Scheme = Uri.UriSchemeHttps }.Uri;
            Channel = CreateChannel();
            AuthChannel = CreateChannel(true);
        }

        private class ResponseVersionHandler : DelegatingHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                var response = await base.SendAsync(request, cancellationToken);
                response.Version = request.Version;

                return response;
            }
        }

        public GrpcChannel CreateChannel(bool auth = false)
        {
            var responseVersionHandler = new ResponseVersionHandler
            {
                InnerHandler = Server.CreateHandler()
            };
            var options = new GrpcChannelOptions
            {
                HttpClient = new HttpClient(responseVersionHandler)
                {
                    BaseAddress = Server.BaseAddress
                },
            };
            if (auth)
            {
                var credentials = CallCredentials.FromInterceptor((context, metadata) =>
                {
                    foreach (var header in AuthHeaders)
                        metadata.Add(header.Key, header.Value);
                    return Task.CompletedTask;
                });
                options.Credentials = ChannelCredentials.Create(new SslCredentials(), credentials);
            }
            return GrpcChannel.ForAddress(Server.BaseAddress, options);
        }

        public TService GetService<TService>() => Server.Host.Services.GetRequiredService<TService>();

        public IDictionary<string, string> AuthHeaders => new Dictionary<string, string>
        {
            [HeaderNames.Authorization] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("test:admin1!"))
        };


        public string SerializeBody(object? value)
        {
            return JsonSerializer.Serialize(value);
        }

        public T DeserializeBody<T>(string? value)
        {
            return JsonSerializer.Deserialize<T>(value, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<TResult> MakeJsonSuccessRequest<TResult>(string path, HttpMethod method, object? bodyObject = null, IDictionary<string, string>? headers = null, bool addAuth = true)
        {
            var body = SerializeBody(bodyObject);
            var content = await MakeSuccessRequest(path, method, body, headers, addAuth);
            var result = DeserializeBody<TResult>(content);
            return result;
        }

        public async Task<string> MakeSuccessRequest(string path, HttpMethod method,
            string? body = null, IDictionary<string, string>? headers = null, bool addAuth = true)
        {
            var response = await MakeRequest(path, method, body, headers, addAuth);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return content;
        }

        public Task<HttpResponseMessage> MakeRequest(string path, HttpMethod method,
            string? body = null, IDictionary<string, string>? headers = null, bool addAuth = true)
        {
            return MakeRequest(path, method, headers, addAuth, rb =>
            {
                rb.AddHeader("Accept", new MediaTypeWithQualityHeaderValue("application/vnd.coreapi+json").MediaType);
                if (!string.IsNullOrEmpty(body))
                    rb.And(requestMessage => requestMessage.Content = new StringContent(body, Encoding.UTF8, new MediaTypeWithQualityHeaderValue(System.Net.Mime.MediaTypeNames.Application.Json).MediaType));
            });
        }

        private async Task<HttpResponseMessage> MakeRequest(string path, HttpMethod method,
            IDictionary<string, string>? headers = null, bool addAuth = true, Action<RequestBuilder>? requestPrepare = null)
        {
            var requestBuilder = Server.CreateRequest(path);

            AddHeaders(requestBuilder, headers, addAuth);
            requestPrepare?.Invoke(requestBuilder);
            return await requestBuilder.SendAsync(method.Method);
        }

        private void AddHeaders(RequestBuilder builder, IDictionary<string, string>? headers, bool addAuth = true)
        {
            builder.AddHeader("X-Real-IP", "127.0.0.1");
            if (addAuth)
                foreach (var header in AuthHeaders)
                    builder.AddHeader(header.Key, header.Value);
            if (headers == null)
                return;
            foreach (var header in headers)
                builder.AddHeader(header.Key, header.Value);
        }

        public virtual void Dispose()
        {
            Channel?.Dispose();
            AuthChannel?.Dispose();
            Server?.Dispose();
        }
    }
}
