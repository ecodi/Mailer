using FluentAssertions;
using Mailer.Domain.Models;
using Mailer.Exceptions;
using System;
using System.Linq;
using Xunit;

namespace Mailer.Domain.Tests.Models
{
    public class MailingTests
    {
        private readonly Mailing _testModel = new Mailing
        {
            Id = Guid.NewGuid(),
            Recipients = new[] { new Recipient { EmailAddress = "recipient@email.com", FirstName = "Chuck", LastName = "Norris" } },
            Sender = new Sender { EmailAddress = "sender@email.com", Name = "i am sender" },
            SubjectTemplate = "Happy Birthday {{recipient.firstname}} {{recipient.lastname}}",
            PlainBodyTemplate = "Email sent to {{recipient.emailaddress}}",
            HtmlBodyTemplate = "<body>Email sent from {{sender.emailaddress}} ({{sender.name}})</body>",
            Status = new MailingStatus { StatusCode = MailingStatusCode.Draft }
        };

        public class ValidateMethod : MailingTests
        {
            [Fact]
            public void DoesNotThrowException_IfAllValuesProper()
            {
                _testModel.Validate();
            }

            [Fact]
            public void ThrowsMailerRequiredFieldEmpty_IfNoneRecipientSet()
            {
                _testModel.Recipients = new Recipient[0];
                Assert.Throws<MailerRequiredFieldEmptyException>(() => _testModel.Validate());
            }

            [Fact]
            public void ThrowsMailerRequiredFieldEmpty_IfSenderNotSet()
            {
                _testModel.Sender = default!;
                Assert.Throws<MailerRequiredFieldEmptyException>(() => _testModel.Validate());
            }
        }

        public class PrepareEmailMethod : MailingTests
        {
            [Fact]
            public void GeneratesProperEmail()
            {
                var email = _testModel.PrepareEmail(_testModel.Recipients.First());
                email.Should().BeEquivalentTo(new Email
                {
                    Id = email.Id,
                    MailingId = _testModel.Id,
                    Recipient = new Email.Entity { EmailAddress = "recipient@email.com", Name = "Chuck Norris" },
                    Sender = new Email.Entity { EmailAddress = "sender@email.com", Name = "i am sender" },
                    Subject = "Happy Birthday Chuck Norris",
                    PlainBody = "Email sent to recipient@email.com",
                    HtmlBody = "<body>Email sent from sender@email.com (i am sender)</body>"
                }, opts => opts.ComparingByMembers<Email>().Excluding(e => e.Timestamp));
            }
        }
    }
}
