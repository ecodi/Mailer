using Mailer.Domain.Repositories;
using Mailer.IntegrationTests.Fixtures;
using Mailer.Api.ViewModels.SenderVm;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using System.Net;
using FluentAssertions;

namespace Mailer.IntegrationTests.ApiTests
{
    [Collection(CollectionNames.ApiRest)]
    public class SendersRestTests
    {
        private readonly ApiFixture _api;
        private readonly ISenderRepository _senderRepository;

        public SendersRestTests(ApiFixture apiFixture)
        {
            _api = apiFixture;
            _senderRepository = _api.GetService<ISenderRepository>();
        }

        [Fact]
        public async Task GetAsyncRequest_WithoutAuthHeader_Returns401Error()
        {
            var model = await _senderRepository.GetByEmailAddressAsync(_api.Context, "sender@email.com");
            var response = await _api.MakeRequest($"senders/{model!.Id}", HttpMethod.Get, addAuth: false);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetAsyncRequest_ReturnsRequestedSender()
        {
            var model = await _senderRepository.GetByEmailAddressAsync(_api.Context, "sender@email.com");
            var result = await _api.MakeJsonSuccessRequest<SenderViewModel>($"senders/{model!.Id}", HttpMethod.Get);
            Assert.Equal(model.Id, result.Id);
            Assert.Equal(model.EmailAddress, result.EmailAddress);
        }

        [Fact]
        public async Task GetListAsyncRequest_ReturnsSendersList()
        {
            var results = await _api.MakeJsonSuccessRequest<ICollection<SenderViewModel>>("senders", HttpMethod.Get);
            Assert.NotEmpty(results);
        }

        [Fact]
        public async Task InsertAsyncRequest_AddsSenderToStorage()
        {
            var result = await _api.MakeJsonSuccessRequest<SenderViewModel>("senders", HttpMethod.Post, new SenderAddModel
            {
                EmailAddress = "my.address@example.com",
                Name = "Mailer System"
            });
            Assert.False(result.Id.IsEmpty());
            Assert.Equal("my.address@example.com", result.EmailAddress);

            var model = await _senderRepository.GetByIdAsync(_api.Context, result.Id);
            Assert.NotNull(model);
            Assert.Equal("my.address@example.com", model!.EmailAddress);
        }

        [Fact]
        public async Task UpdateAsyncRequest_UpdatesSenderInStorage()
        {
            var model = await _senderRepository.GetByEmailAddressAsync(_api.Context, "sender2@email.com");
            var result = await _api.MakeJsonSuccessRequest<SenderViewModel>($"senders/{model!.Id}/{model.RowVersion}", HttpMethod.Put, new SenderUpdateModel
            {
                Name = "I am updated"
            });
            result.Should().BeEquivalentTo(
                new SenderViewModel
                {
                    Id = model.Id,
                    RowVersion = model.RowVersion + 1,
                    EmailAddress = model.EmailAddress,
                    Name = "I am updated"
                }
            );

            model = await _senderRepository.GetByIdAsync(_api.Context, result.Id);
            Assert.Equal(model!.RowVersion, result.RowVersion);
        }

        [Fact]
        public async Task RemoveAsyncRequest_RemovesSenderFromStorage()
        {
            var model = await _senderRepository.GetByEmailAddressAsync(_api.Context, "todelete@email.com");
            await _api.MakeSuccessRequest($"senders/{model!.Id}/{model.RowVersion}", HttpMethod.Delete);
            model = await _senderRepository.GetByIdAsync(_api.Context, model.Id);
            Assert.Null(model);
        }
    }
}
