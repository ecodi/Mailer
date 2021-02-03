using Mailer.Exceptions;

namespace Mailer.Domain.Models
{
    public class Sender : BaseModel, IMailerModel
    {
        public string EmailAddress { get; set; } = default!;
        public string? Name { get; set; }

        public void Validate()
        {
            if (string.IsNullOrEmpty(EmailAddress))
                throw new MailerRequiredFieldEmptyException(nameof(EmailAddress));
            if (!EmailAddress.IsValidEmail())
                throw new MailerInvalidFieldFormatException(nameof(EmailAddress), "improper e-mail address");
        }
    }
}
