using Mailer.Domain.Repositories;
using Mailer.IntegrationTests.Fixtures;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Mailer.API.Protos.Recipients;
using Grpc.Core;

namespace Mailer.IntegrationTests.ApiTests
{
    [Collection(CollectionNames.ApiGrpc)]
    public class RecipientsGrpcTests
    {
        private readonly ApiFixture _api;
        private readonly Recipients.RecipientsClient _client;
        private readonly IRecipientRepository _recipientRepository;

        public RecipientsGrpcTests(ApiFixture apiFixture)
        {
            _api = apiFixture;
            _client = new Recipients.RecipientsClient(_api.AuthChannel);
            _recipientRepository = _api.GetService<IRecipientRepository>();
        }

        [Fact]
        public async Task Request_RequiresAuthentication()
        {
            var clientUnAuth = new Recipients.RecipientsClient(_api.Channel);
            using var call = clientUnAuth.GetRecipients(new GetRecipientsRequest());
            var ex = await Assert.ThrowsAsync<RpcException>(() => call.LoadAsync());
            Assert.Equal(StatusCode.Unauthenticated, ex.StatusCode);
        }

        [Fact]
        public async Task GetAsyncRequest_ReturnsRequestedRecipient()
        {
            var model = await _recipientRepository.GetByEmailAddressAsync(_api.Context, "email@sink.sendgrid.net");
            var result = await _client.GetRecipientAsync(new GetRecipientRequest { Id = model!.Id.ToString() });
            Assert.Equal(model.Id, result.Id.ToGuid());
            Assert.Equal(model.EmailAddress, result.EmailAddress);
        }

        [Fact]
        public async Task GetListAsyncRequest_ReturnsRecipientsList()
        {
            using var call = _client.GetRecipients(new GetRecipientsRequest());
            var results = await call.LoadAsync();
            Assert.NotEmpty(results);
        }

        [Fact]
        public async Task InsertAsyncRequest_AddsRecipientToStorage()
        {
            var result = await _client.AddRecipientAsync(new AddRecipientRequest
            {
                Data = new AddRecipientRequest.Types.Data
                {
                    EmailAddress = "my.address@example.com",
                    FirstName = "John",
                    LastName = "Snow"
                }
            });
            Assert.False(result.Id.ToGuid().IsEmpty());
            Assert.Equal("my.address@example.com", result.EmailAddress);

            var model = await _recipientRepository.GetByIdAsync(_api.Context, result.Id.ToGuid());
            Assert.NotNull(model);
            Assert.Equal("my.address@example.com", model!.EmailAddress);
        }

        [Fact]
        public async Task UpdateAsyncRequest_UpdatesRecipientInStorage()
        {
            var model = await _recipientRepository.GetByEmailAddressAsync(_api.Context, "email3@sink.sendgrid.net");
            var result = await _client.UpdateRecipientAsync(new UpdateRecipientRequest
            {
                Id = model!.Id.ToString(),
                RowVersion = model.RowVersion,
                Data = new UpdateRecipientRequest.Types.Data
                {
                    FirstName = "I am",
                    LastName = "Updated"
                }
            });
            result.Should().BeEquivalentTo(
                new Recipient
                {
                    Id = model.Id.ToString(),
                    RowVersion = model.RowVersion + 1,
                    EmailAddress = model.EmailAddress,
                    FirstName = "I am",
                    LastName = "Updated"
                }
            );

            model = await _recipientRepository.GetByIdAsync(_api.Context, result.Id.ToGuid());
            Assert.Equal(model!.RowVersion, result.RowVersion);
        }

        [Fact]
        public async Task RemoveAsyncRequest_RemovesRecipientFromStorage()
        {
            var model = await _recipientRepository.GetByEmailAddressAsync(_api.Context, "todelete@sink.sendgrid.net");
            await _client.DeleteRecipientAsync(new DeleteRecipientRequest
            {
                Id = model!.Id.ToString(),
                RowVersion = model.RowVersion
            }); 
            model = await _recipientRepository.GetByIdAsync(_api.Context, model.Id);
            Assert.Null(model);
        }
    }
}
