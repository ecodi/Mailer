using Mailer.Domain.Application;
using Mailer.Domain.Models;
using Mailer.Domain.Repositories;
using Mailer.Domain.Services;
using Mailer.Exceptions;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Mailer.Domain.Tests.Services
{
    public class MailingServiceTests
    {
        private readonly Mock<IMailingRepository> _mailingRepositoryMock;
        private readonly Mock<IRecipientService> _recipientServiceMock;
        private readonly Mock<ISenderService> _senderServiceMock;
        private readonly Mock<IEmailRepository> _emailRepositoryMock;
        private readonly Mock<IMailClient> _mailClientMock;
        private readonly Mock<IMessageBus<ExecuteMailingMessage>> _messageBusMock;
        private readonly MailingService _service;
        private readonly IMailerContext _context;

        private readonly Mailing _testModel = new Mailing
        {
            Recipients = new[] { new Recipient { Id = Guid.NewGuid(), EmailAddress = "recipient@email.com", FirstName = "Chuck", LastName = "Norris" } },
            Sender = new Sender { Id = Guid.NewGuid(), EmailAddress = "sender@email.com", Name = "i am sender" },
            SubjectTemplate = "Happy Birthday {{recipient.firstname}} {{recipient.lastname}}",
            PlainBodyTemplate = "Email sent to {{recipient.emailaddress}}",
            HtmlBodyTemplate = "<body>Email sent from {{sender.emailaddress}} ({{sender.name}})</body>",
            Status = new MailingStatus { StatusCode = MailingStatusCode.Draft }
        };

        public MailingServiceTests()
        {
            _mailingRepositoryMock = new Mock<IMailingRepository>();
            _recipientServiceMock = new Mock<IRecipientService>();
            foreach(var recipient in _testModel.Recipients)
                _recipientServiceMock.Setup(m => m.GetAsync(It.IsAny<IMailerContext>(), recipient.Id))
                    .ReturnsAsync(new Recipient { Id = recipient.Id });
            _senderServiceMock = new Mock<ISenderService>();
            _senderServiceMock.Setup(m => m.GetAsync(It.IsAny<IMailerContext>(), _testModel.Sender.Id))
                .ReturnsAsync(new Sender { Id = _testModel.Sender.Id });
            _emailRepositoryMock = new Mock<IEmailRepository>();
            _mailClientMock = new Mock<IMailClient>();
            _messageBusMock = new Mock<IMessageBus<ExecuteMailingMessage>>();
            _service = new MailingService(_mailingRepositoryMock.Object, _recipientServiceMock.Object, _senderServiceMock.Object,
                _emailRepositoryMock.Object, _mailClientMock.Object, _messageBusMock.Object);
            _context = new Mock<IMailerContext>().Object;
        }

        public class SaveMethod : MailingServiceTests
        {
            [Fact]
            public async Task ThrowsMailerNotExistingReference_IfAnyRecipientDoesNotExist()
            {
                _recipientServiceMock.Setup(m => m.GetAsync(It.IsAny<IMailerContext>(), _testModel.Recipients.First().Id))
                    .ReturnsAsync(default(Recipient));
                await Assert.ThrowsAsync<MailerNotExistingReferenceException>(() => _service.SaveAsync(_context, _testModel));
                _mailingRepositoryMock.Verify(m => m.InsertAsync(_context, It.IsAny<Mailing>()), Times.Never);
            }

            [Fact]
            public async Task ThrowsMailerNotExistingReference_IfSenderDoesNotExist()
            {
                _senderServiceMock.Setup(m => m.GetAsync(_context, _testModel.Sender.Id))
                    .ReturnsAsync(default(Sender));
                await Assert.ThrowsAsync<MailerNotExistingReferenceException>(() => _service.SaveAsync(_context, _testModel));
                _mailingRepositoryMock.Verify(m => m.InsertAsync(_context, It.IsAny<Mailing>()), Times.Never);
            }

            [Fact]
            public async Task InsertsModelToDatabase_IfNoErrors()
            {
                await _service.SaveAsync(_context, _testModel);
                _mailingRepositoryMock.Verify(m => m.InsertAsync(_context, _testModel), Times.Once);
            }
        }

        public class DeleteMethod : MailingServiceTests
        {
            public DeleteMethod()
            {
                _testModel.Id = Guid.NewGuid();
            }

            [Theory,
                InlineData(MailingStatusCode.Accepted),
                InlineData(MailingStatusCode.Done),
                InlineData(MailingStatusCode.InProgress)]
            public async Task ThrowsMailerValidationException_IfStatusCodeNotDraft(MailingStatusCode statusCode)
            {
                _testModel.Status.StatusCode = statusCode;
                await Assert.ThrowsAsync<MailerValidationException>(() => _service.DeleteAsync(_context, _testModel));
                _mailingRepositoryMock.Verify(m => m.ReplaceOnStatusCodeAsync(It.IsAny<IMailerContext>(), It.IsAny<Mailing>(), It.IsAny<MailingStatusCode>()), Times.Never);
            }
        }

        public class ExecuteMethod : MailingServiceTests
        {
            public ExecuteMethod()
            {
                _testModel.Id = Guid.NewGuid();
            }

            [Theory,
                InlineData(MailingStatusCode.Accepted),
                InlineData(MailingStatusCode.Done),
                InlineData(MailingStatusCode.InProgress)]
            public async Task ThrowsMailerValidationException_IfStatusCodeNotDraft(MailingStatusCode statusCode)
            {
                _testModel.Status.StatusCode = statusCode;
                await Assert.ThrowsAsync<MailerValidationException>(() => _service.ExecuteAsync(_context, _testModel));
                _mailingRepositoryMock.Verify(m => m.ReplaceOnStatusCodeAsync(It.IsAny<IMailerContext>(), It.IsAny<Mailing>(), It.IsAny<MailingStatusCode>()), Times.Never);
            }

            [Fact]
            public async Task ThrowsMailerRequiredFieldEmptyException_IfSubjectTemplateNotSet()
            {
                _testModel.SubjectTemplate = null;
                await Assert.ThrowsAsync<MailerRequiredFieldEmptyException>(() => _service.ExecuteAsync(_context, _testModel));
                _mailingRepositoryMock.Verify(m => m.ReplaceOnStatusCodeAsync(It.IsAny<IMailerContext>(), It.IsAny<Mailing>(), It.IsAny<MailingStatusCode>()), Times.Never);
            }

            [Fact]
            public async Task ThrowsMailerRequiredFieldEmptyException_IfBodyTemplatesNotSet()
            {
                _testModel.PlainBodyTemplate = null;
                _testModel.HtmlBodyTemplate = null;
                await Assert.ThrowsAsync<MailerRequiredFieldEmptyException>(() => _service.ExecuteAsync(_context, _testModel));
                _mailingRepositoryMock.Verify(m => m.ReplaceOnStatusCodeAsync(It.IsAny<IMailerContext>(), It.IsAny<Mailing>(), It.IsAny<MailingStatusCode>()), Times.Never);
            }

            [Fact]
            public async Task ChangesStatusCodeToAccepted_PublishesMessage()
            {
                await _service.ExecuteAsync(_context, _testModel);
                Assert.Equal(MailingStatusCode.Accepted, _testModel.Status.StatusCode);
                _messageBusMock.Verify(m => m.PublishAsync(It.Is<ExecuteMailingMessage>(msg => msg.MailingId == _testModel.Id)), Times.Once);
            }
        }

        public class ProcessMethod : MailingServiceTests
        {
            public ProcessMethod()
            {
                _testModel.Id = Guid.NewGuid();
            }

            [Theory,
                InlineData(MailingStatusCode.Done),
                InlineData(MailingStatusCode.InProgress)]
            public async Task ThrowsMailerValidationException_IfStatusCodeNotDraftAndNotAccepted(MailingStatusCode statusCode)
            {
                _testModel.Status.StatusCode = statusCode;
                await Assert.ThrowsAsync<MailerValidationException>(() => _service.ProcessAsync(_context, _testModel));
                _mailingRepositoryMock.Verify(m => m.ReplaceAsync(It.IsAny<IMailerContext>(), It.IsAny<Mailing>()), Times.Never);
            }

            [Fact]
            public async Task ChangesStatusCodeToDone_SendsEmails()
            {
                await _service.ProcessAsync(_context, _testModel);
                Assert.Equal(MailingStatusCode.Done, _testModel.Status.StatusCode);
                _emailRepositoryMock.Verify(m => m.InsertAsync(It.IsAny<IMailerContext>(), It.Is<Email>(e => e.MailingId == _testModel.Id)), Times.Exactly(_testModel.Recipients.Count));
                _mailClientMock.Verify(m => m.SendEmailAsync(It.IsAny<Email>()), Times.Exactly(_testModel.Recipients.Count));
            }
        }
    }
}
