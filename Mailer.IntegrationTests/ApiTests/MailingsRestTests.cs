using Mailer.Domain.Repositories;
using Mailer.IntegrationTests.Fixtures;
using Mailer.Api.ViewModels.MailingVm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Mailer.Domain.Models;
using System.Net;
using Polly;
using FluentAssertions;

namespace Mailer.IntegrationTests.ApiTests
{
    [Collection(CollectionNames.ApiRest)]
    public class MailingsRestTests
    {
        private readonly ApiFixture _api;
        private readonly IMailingRepository _mailingRepository;

        public MailingsRestTests(ApiFixture apiFixture)
        {
            _api = apiFixture;
            _mailingRepository = _api.GetService<IMailingRepository>();
        }

        [Fact]
        public async Task GetAsyncRequest_WithoutAuthHeader_Returns401Error()
        {
            var model = await _mailingRepository.GetListAsync(_api.Context).FirstAsync(m => m.SubjectTemplate == "read only");
            var response = await _api.MakeRequest($"mailings/{model.Id}", HttpMethod.Get, addAuth: false);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetAsyncRequest_ReturnsRequestedMailing()
        {
            var model = await _mailingRepository.GetListAsync(_api.Context).FirstAsync(m => m.SubjectTemplate == "read only");
            var result = await _api.MakeJsonSuccessRequest<MailingViewModel>($"mailings/{model.Id}", HttpMethod.Get);
            Assert.Equal(model.Id, result.Id);
            Assert.Equal(model.Recipients.First().EmailAddress, result.Recipients.FirstOrDefault()?.EmailAddress);
            Assert.Equal(model.Sender.EmailAddress, result.Sender?.EmailAddress);
        }

        [Fact]
        public async Task GetListAsyncRequest_ReturnsMailingsList()
        {
            var results = await _api.MakeJsonSuccessRequest<ICollection<MailingViewModel>>("mailings", HttpMethod.Get);
            Assert.NotEmpty(results);
            var result = results.First();
            Assert.NotNull(result.Recipients.FirstOrDefault()?.EmailAddress);
            Assert.NotNull(result.Sender?.EmailAddress);
        }

        [Fact]
        public async Task InsertAsyncRequest_AddsMailingToStorage()
        {
            var recipientRepository = _api.GetService<IRecipientRepository>();
            var recipient = await recipientRepository.GetByEmailAddressAsync(_api.Context, "email@sink.sendgrid.net");
            var senderRepository = _api.GetService<ISenderRepository>();
            var sender = await senderRepository.GetByEmailAddressAsync(_api.Context, "sender@email.com");

            var result = await _api.MakeJsonSuccessRequest<MailingViewModel>("mailings", HttpMethod.Post, new MailingAddModel
            {
                RecipientsIds = new[] { recipient!.Id },
                SenderId = sender!.Id,
                SubjectTemplate = "Hello {{recipient.Name}}",
                PlainBodyTemplate = "Place your message here",
                HtmlBodyTemplate = "<html><body><h1>Place your message here</h1></body></html>"
            });
            Assert.False(result.Id.IsEmpty());
            Assert.Equal(recipient.Id, result.Recipients?.FirstOrDefault()?.Id);
            Assert.Equal(sender.Id, result.Sender?.Id);

            var model = await _mailingRepository.GetByIdAsync(_api.Context, result.Id);
            Assert.NotNull(model);
            Assert.Equal(recipient.Id, model!.Recipients?.FirstOrDefault()?.Id);
            Assert.Equal(sender.Id, model!.Sender?.Id);
        }

        [Fact]
        public async Task SendAsyncRequest_SendsEmailsAndUpdatesMailingStatus()
        {
            var model = await _mailingRepository.GetListAsync(_api.Context).FirstAsync(m => m.SubjectTemplate == "read only");
            var result = await _api.MakeJsonSuccessRequest<MailingViewModel>($"mailings/{model.Id}/{model.RowVersion}/emails", HttpMethod.Post);
            Assert.Equal(model.Id, result.Id);
            Assert.Equal(MailingStatusCode.Accepted, result.Status.StatusCode);

            model = await Policy
                .HandleResult<Mailing>(m => m is null || m.Status.StatusCode != MailingStatusCode.Done)
                .WaitAndRetryAsync(new[] {
                    TimeSpan.FromMilliseconds(500),
                    TimeSpan.FromMilliseconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(60)
                })
                .ExecuteAsync(() => _mailingRepository.GetByIdAsync(_api.Context, model.Id)!);
            Assert.Equal(MailingStatusCode.Done, model.Status.StatusCode);

            var mailClient = _api.GetService<DummyMailClient>();
            Assert.All(model.Recipients, r => Assert.Contains(mailClient.SendEmails, e => e.MailingId == model.Id && e.Recipient.EmailAddress == r.EmailAddress));

            var emailRepository = _api.GetService<IEmailRepository>();
            var emails = await emailRepository.GetListByMailingAsync(_api.Context, model.Id).ToListAsync();
            Assert.All(model.Recipients, r => Assert.Contains(emails, e => e.MailingId == model.Id && e.Recipient.EmailAddress == r.EmailAddress));
        }

        [Fact]
        public async Task UpdateAsyncRequest_UpdatesMailingInStorage()
        {
            var model = await _mailingRepository.GetListAsync(_api.Context).FirstAsync(r => r.SubjectTemplate == "to update");
            var result = await _api.MakeJsonSuccessRequest<MailingViewModel>($"mailings/{model.Id}/{model.RowVersion}", HttpMethod.Put, new MailingUpdateModel
            {
                RecipientsIds = model.Recipients.Select(r => r.Id).ToList(),
                SenderId = model.Sender.Id,
                SubjectTemplate = "updated subject",
                PlainBodyTemplate = "updated body",
                HtmlBodyTemplate = "<html><body>updated body</body></html>"
            });
            result.Should().BeEquivalentTo(
                new MailingViewModel
                {
                    Id = model.Id,
                    RowVersion = model.RowVersion + 1,
                    SubjectTemplate = "updated subject",
                    PlainBodyTemplate = "updated body",
                    HtmlBodyTemplate = "<html><body>updated body</body></html>",
                    Status = new MailingStatusViewModel { StatusCode = model.Status.StatusCode, Message = model.Status.Message }
                }, opts => opts.Excluding(m => m.Recipients).Excluding(m => m.Sender)
            );

            model = await _mailingRepository.GetByIdAsync(_api.Context, result.Id);
            Assert.Equal(model!.RowVersion, result.RowVersion);
        }

        [Fact]
        public async Task RemoveAsyncRequest_RemovesMailingFromStorage()
        {
            var model = await _mailingRepository.GetListAsync(_api.Context).FirstAsync(r => r.SubjectTemplate == "to delete");
            await _api.MakeSuccessRequest($"mailings/{model.Id}/{model.RowVersion}", HttpMethod.Delete);
            model = await _mailingRepository.GetByIdAsync(_api.Context, model.Id);
            Assert.Null(model);
        }
    }
}
