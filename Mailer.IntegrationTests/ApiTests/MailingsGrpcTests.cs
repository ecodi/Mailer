using Mailer.Domain.Repositories;
using Mailer.IntegrationTests.Fixtures;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Mailer.API.Protos.Mailings;
using Grpc.Core;

namespace Mailer.IntegrationTests.ApiTests
{
    [Collection(CollectionNames.ApiGrpc)]
    public class MailingsGrpcTests
    {
        private readonly ApiFixture _api;
        private readonly Mailings.MailingsClient _client;
        private readonly IMailingRepository _mailingRepository;

        public MailingsGrpcTests(ApiFixture apiFixture)
        {
            _api = apiFixture;
            _client = new Mailings.MailingsClient(_api.AuthChannel);
            _mailingRepository = _api.GetService<IMailingRepository>();
        }

        [Fact]
        public async Task Request_RequiresAuthentication()
        {
            var clientUnAuth = new Mailings.MailingsClient(_api.Channel);
            using var call = clientUnAuth.GetMailings(new GetMailingsRequest());
            var ex = await Assert.ThrowsAsync<RpcException>(() => call.LoadAsync());
            Assert.Equal(StatusCode.Unauthenticated, ex.StatusCode);
        }

        [Fact]
        public async Task GetAsyncRequest_ReturnsRequestedMailing()
        {
            var model = await _mailingRepository.GetListAsync(_api.Context).FirstAsync(m => m.SubjectTemplate == "read only");
            var result = await _client.GetMailingAsync(new GetMailingRequest { Id = model!.Id.ToString() });
            Assert.Equal(model.Id, result.Id.ToGuid());
            Assert.Equal(model.Recipients.First().EmailAddress, result.Recipients.FirstOrDefault()?.EmailAddress);
            Assert.Equal(model.Sender.EmailAddress, result.Sender?.EmailAddress);
        }

        [Fact]
        public async Task GetListAsyncRequest_ReturnsMailingsList()
        {
            using var call = _client.GetMailings(new GetMailingsRequest());
            var results = await call.LoadAsync();
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

            var data = new AddMailingRequest.Types.Data
            {
                SenderId = sender!.Id.ToString(),
                SubjectTemplate = "Hello {{recipient.Name}}",
                PlainBodyTemplate = "Place your message here",
                HtmlBodyTemplate = "<html><body><h1>Place your message here</h1></body></html>"
            };
            data.RecipientsIds.Add(recipient!.Id.ToString());
            var result = await _client.AddMailingAsync(new AddMailingRequest
            {
                Data = data
            });
            Assert.False(result.Id.ToGuid().IsEmpty());
            Assert.Equal(recipient.Id, result.Recipients?.FirstOrDefault()?.Id.ToGuid());
            Assert.Equal(sender.Id, result.Sender?.Id.ToGuid());

            var model = await _mailingRepository.GetByIdAsync(_api.Context, result.Id.ToGuid());
            Assert.NotNull(model);
            Assert.Equal(recipient.Id, model!.Recipients?.FirstOrDefault()?.Id);
            Assert.Equal(sender.Id, model!.Sender?.Id);
        }

        [Fact]
        public async Task SendAsyncRequest_SendsEmailsAndUpdatesMailingStatus()
        {
            var model = await _mailingRepository.GetListAsync(_api.Context).FirstAsync(m => m.SubjectTemplate == "read only");
            using var call = _client.SendMailing(new SendMailingRequest
            {
                Id = model!.Id.ToString(),
                RowVersion = model.RowVersion
            });
            var results = await call.LoadAsync();
            Assert.True(results.Count <= 3);
            Assert.Equal(MailingStatus.Types.MailingStatusCode.Accepted, results.First().Status.StatusCode);
            Assert.Equal(MailingStatus.Types.MailingStatusCode.Done, results.Last().Status.StatusCode);
            if (results.Count == 3)
                Assert.Equal(MailingStatus.Types.MailingStatusCode.InProgress, results[1].Status.StatusCode);
            Assert.All(results, r => Assert.Equal(model.Id, r.Id.ToGuid()));

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
            var data = new UpdateMailingRequest.Types.Data
            {
                SenderId = model.Sender.Id.ToString(),
                SubjectTemplate = "updated subject",
                PlainBodyTemplate = "updated body",
                HtmlBodyTemplate = "<html><body>updated body</body></html>"
            };
            data.RecipientsIds.AddRange(model.Recipients.Select(r => r.Id.ToString()));
            var result = await _client.UpdateMailingAsync(new UpdateMailingRequest
            {
                Id = model!.Id.ToString(),
                RowVersion = model.RowVersion,
                Data = data
            });
            result.Should().BeEquivalentTo(
                new Mailing
                {
                    Id = model.Id.ToString(),
                    RowVersion = model.RowVersion + 1,
                    SubjectTemplate = "updated subject",
                    PlainBodyTemplate = "updated body",
                    HtmlBodyTemplate = "<html><body>updated body</body></html>",
                    Status = new MailingStatus { StatusCode = MailingStatus.Types.MailingStatusCode.Draft, Message = model.Status.Message ?? "" }
                }, opts => opts.ComparingByMembers<Mailing>().Excluding(m => m.Recipients).Excluding(m => m.Sender)
            );

            model = await _mailingRepository.GetByIdAsync(_api.Context, result.Id.ToGuid());
            Assert.Equal(model!.RowVersion, result.RowVersion);
        }

        [Fact]
        public async Task RemoveAsyncRequest_RemovesMailingFromStorage()
        {
            var model = await _mailingRepository.GetListAsync(_api.Context).FirstAsync(r => r.SubjectTemplate == "to delete");
            await _client.DeleteMailingAsync(new DeleteMailingRequest
            {
                Id = model!.Id.ToString(),
                RowVersion = model.RowVersion
            });
            model = await _mailingRepository.GetByIdAsync(_api.Context, model.Id);
            Assert.Null(model);
        }
    }
}
