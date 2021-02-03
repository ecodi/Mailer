using Mailer.Domain.Models;
using Mailer.Exceptions;
using System;
using Xunit;

namespace Mailer.Domain.Tests.Models
{
    public class EmailTests
    {
        private readonly Email _testModel = new Email
        {
            Id = Guid.NewGuid(),
            MailingId = Guid.NewGuid(),
            Recipient = new Email.Entity { EmailAddress = "recipient@email.com", Name = "some name" },
            Sender = new Email.Entity { EmailAddress = "sender@email.com", Name = "i am sender" },
            Subject = "Happy birthday",
            PlainBody = "Good luck!",
            HtmlBody = "<h1>Good luck!</h1>",
            StatusCode = EmailStatusCode.Sent
        };

        public class ValidateMethod : EmailTests
        {

            [Fact]
            public void DoesNotThrowException_IfAllValuesProper()
            {
                _testModel.Validate();
            }

            [Fact]
            public void ThrowsMailerRequiredFieldEmpty_IfRecipientNotSet()
            {
                _testModel.Recipient = default!;
                Assert.Throws<MailerRequiredFieldEmptyException>(() => _testModel.Validate());
            }

            [Fact]
            public void ThrowsMailerInvalidFieldFormat_IfRecipientsInvalidEmail()
            {
                _testModel.Recipient.EmailAddress = "invalid";
                Assert.Throws<MailerInvalidFieldFormatException>(() => _testModel.Validate());
            }

            [Fact]
            public void ThrowsMailerRequiredFieldEmpty_IfSenderNotSet()
            {
                _testModel.Sender = null;
                Assert.Throws<MailerRequiredFieldEmptyException>(() => _testModel.Validate());
            }

            [Fact]
            public void ThrowsMailerInvalidFieldFormat_IfSendersInvalidEmail()
            {
                _testModel.Sender!.EmailAddress = "invalid";
                Assert.Throws<MailerInvalidFieldFormatException>(() => _testModel.Validate());
            }
        }
    }
}
