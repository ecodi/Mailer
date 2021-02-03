using Mailer.Domain.Models;
using Mailer.Exceptions;
using Xunit;

namespace Mailer.Domain.Tests.Models
{
    public class RecipientTests
    {
        private readonly Recipient _testModel = new Recipient
        {
            EmailAddress = "recipient@email.com",
            FirstName = "Chuck",
            LastName = "Norris"
        };

        public class ValidateMethod : RecipientTests
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
