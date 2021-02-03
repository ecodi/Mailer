using Mailer.Domain.Models;
using Mailer.Exceptions;
using Xunit;

namespace Mailer.Domain.Tests.Models
{
    public class SenderTests
    {
        private readonly Sender _testModel = new Sender
        {
            EmailAddress = "sender@email.com",
            Name = "i am sender"
        };

        public class ValidateMethod : SenderTests
        {
            [Fact]
            public void DoesNotThrowException_IfAllValuesProper()
            {
                _testModel.Validate();
            }

            [Fact]
            public void ThrowsMailerRequiredFieldEmpty_IfEmailAddresNotSet()
            {
                _testModel.EmailAddress = default!;
                Assert.Throws<MailerRequiredFieldEmptyException>(() => _testModel.Validate());
            }

            [Fact]
            public void ThrowsMailerInvalidFieldFormat_IfInvalidEmailAddress()
            {
                _testModel.EmailAddress = "invalid";
                Assert.Throws<MailerInvalidFieldFormatException>(() => _testModel.Validate());
            }
        }
    }
}
