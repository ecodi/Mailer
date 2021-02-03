using Mailer.Domain.Repositories;
using Mailer.IntegrationTests.Fixtures;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Mailer.API.Protos.Senders;
using Grpc.Core;

namespace Mailer.IntegrationTests.ApiTests
{
    [Collection(CollectionNames.ApiGrpc)]
    public class SendersGrpcTests
    {
        private readonly ApiFixture _api;
        private readonly Senders.SendersClient _client;
        private readonly ISenderRepository _senderRepository;

        public SendersGrpcTests(ApiFixture apiFixture)
        {
            _api = apiFixture;
            _client = new Senders.SendersClient(_api.AuthChannel);
            _senderRepository = _api.GetService<ISenderRepository>();
        }

        [Fact]
        public async Task Request_RequiresAuthentication()
        {
            var clientUnAuth = new Senders.SendersClient(_api.Channel);
            using var call = clientUnAuth.GetSenders(new GetSendersRequest());
            var ex = await Assert.ThrowsAsync<RpcException>(() => call.LoadAsync());
            Assert.Equal(StatusCode.Unauthenticated, ex.StatusCode);
        }

        [Fact]
        public async Task GetAsyncRequest_ReturnsRequestedSender()
        {
            var model = await _senderRepository.GetByEmailAddressAsync(_api.Context, "sender@email.com");
            var result = await _client.GetSenderAsync(new GetSenderRequest { Id = model!.Id.ToString() });
            Assert.Equal(model.Id, result.Id.ToGuid());
            Assert.Equal(model.EmailAddress, result.EmailAddress);
        }

        [Fact]
        public async Task GetListAsyncRequest_ReturnsSendersList()
        {
            using var call = _client.GetSenders(new GetSendersRequest());
            var results = await call.LoadAsync();
            Assert.NotEmpty(results);
        }

        [Fact]
        public async Task InsertAsyncRequest_AddsSenderToStorage()
        {
            var result = await _client.AddSenderAsync(new AddSenderRequest
            {
                Data = new AddSenderRequest.Types.Data
                {
                    EmailAddress = "my.address@example.com",
                    Name = "Mailer System"
                }
            });
            Assert.False(result.Id.ToGuid().IsEmpty());
            Assert.Equal("my.address@example.com", result.EmailAddress);

            var model = await _senderRepository.GetByIdAsync(_api.Context, result.Id.ToGuid());
            Assert.NotNull(model);
            Assert.Equal("my.address@example.com", model!.EmailAddress);
        }

        [Fact]
        public async Task UpdateAsyncRequest_UpdatesSenderInStorage()
        {
            var model = await _senderRepository.GetByEmailAddressAsync(_api.Context, "sender2@email.com");
            var result = await _client.UpdateSenderAsync(new UpdateSenderRequest
            {
                Id = model!.Id.ToString(),
                RowVersion = model.RowVersion,
                Data = new UpdateSenderRequest.Types.Data
                {
                    Name = "I am updated"
                }
            });
            result.Should().BeEquivalentTo(
                new Sender
                {
                    Id = model.Id.ToString(),
                    RowVersion = model.RowVersion + 1,
                    EmailAddress = model.EmailAddress,
                    Name = "I am updated"
                }
            );

            model = await _senderRepository.GetByIdAsync(_api.Context, result.Id.ToGuid());
            Assert.Equal(model!.RowVersion, result.RowVersion);
        }

        [Fact]
        public async Task RemoveAsyncRequest_RemovesSenderFromStorage()
        {
            var model = await _senderRepository.GetByEmailAddressAsync(_api.Context, "todelete@email.com");
            await _client.DeleteSenderAsync(new DeleteSenderRequest {
                Id = model!.Id.ToString(),
                RowVersion = model.RowVersion
            });
            model = await _senderRepository.GetByIdAsync(_api.Context, model.Id);
            Assert.Null(model);
        }
    }
}
