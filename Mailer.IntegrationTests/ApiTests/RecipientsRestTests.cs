using Mailer.Domain.Repositories;
using Mailer.IntegrationTests.Fixtures;
using Mailer.Api.ViewModels.RecipientVm;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using System.Net;
using FluentAssertions;

namespace Mailer.IntegrationTests.ApiTests
{
    [Collection(CollectionNames.ApiRest)]
    public class RecipientsRestTests
    {
        private readonly ApiFixture _api;
        private readonly IRecipientRepository _recipientRepository;

        public RecipientsRestTests(ApiFixture apiFixture)
        {
            _api = apiFixture;
            _recipientRepository = _api.GetService<IRecipientRepository>();
        }

        [Fact]
        public async Task GetAsyncRequest_WithoutAuthHeader_Returns401Error()
        {
            var model = await _recipientRepository.GetByEmailAddressAsync(_api.Context, "email@sink.sendgrid.net");
            var response = await _api.MakeRequest($"recipients/{model!.Id}", HttpMethod.Get, addAuth: false);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetAsyncRequest_ReturnsRequestedRecipient()
        {
            var model = await _recipientRepository.GetByEmailAddressAsync(_api.Context, "email@sink.sendgrid.net");
            var result = await _api.MakeJsonSuccessRequest<RecipientViewModel>($"recipients/{model!.Id}", HttpMethod.Get);
            Assert.Equal(model.Id, result.Id);
            Assert.Equal(model.EmailAddress, result.EmailAddress);
        }

        [Fact]
        public async Task GetListAsyncRequest_ReturnsRecipientsList()
        {
            var results = await _api.MakeJsonSuccessRequest<ICollection<RecipientViewModel>>("recipients", HttpMethod.Get);
            Assert.NotEmpty(results);
        }

        [Fact]
        public async Task InsertAsyncRequest_AddsRecipientToStorage()
        {
            var result = await _api.MakeJsonSuccessRequest<RecipientViewModel>("recipients", HttpMethod.Post, new RecipientAddModel
            {
                EmailAddress = "my.address@example.com",
                FirstName = "John",
                LastName = "Snow"
            });
            Assert.False(result.Id.IsEmpty());
            Assert.Equal("my.address@example.com", result.EmailAddress);

            var model = await _recipientRepository.GetByIdAsync(_api.Context, result.Id);
            Assert.NotNull(model);
            Assert.Equal("my.address@example.com", model!.EmailAddress);
        }

        [Fact]
        public async Task UpdateAsyncRequest_UpdatesRecipientInStorage()
        {
            var model = await _recipientRepository.GetByEmailAddressAsync(_api.Context, "email3@sink.sendgrid.net");
            var result = await _api.MakeJsonSuccessRequest<RecipientViewModel>($"recipients/{model!.Id}/{model.RowVersion}", HttpMethod.Put, new RecipientUpdateModel
            {
                FirstName = "I am",
                LastName = "Updated"
            });
            result.Should().BeEquivalentTo(
                new RecipientViewModel
                {
                    Id = model.Id,
                    RowVersion = model.RowVersion + 1,
                    EmailAddress = model.EmailAddress,
                    FirstName = "I am",
                    LastName = "Updated"
                }
            );

            model = await _recipientRepository.GetByIdAsync(_api.Context, result.Id);
            Assert.Equal(model!.RowVersion, result.RowVersion);
        }

        [Fact]
        public async Task RemoveAsyncRequest_RemovesRecipientFromStorage()
        {
            var model = await _recipientRepository.GetByEmailAddressAsync(_api.Context, "todelete@sink.sendgrid.net");
            await _api.MakeSuccessRequest($"recipients/{model!.Id}/{model.RowVersion}", HttpMethod.Delete);
            model = await _recipientRepository.GetByIdAsync(_api.Context, model.Id);
            Assert.Null(model);
        }
    }
}
