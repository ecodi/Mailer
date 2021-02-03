using Mailer.Exceptions;
using System;

namespace Mailer.Domain.Models
{
    public class Email : BaseModel, IMailerModel
    {
        public Guid MailingId { get; set; }
        public Entity Recipient { get; set; } = default!;
        public Entity? Sender { get; set; }
        public string? Subject { get; set; }
        public string? PlainBody { get; set; }
        public string? HtmlBody { get; set; }
        public EmailStatusCode StatusCode { get; set; }
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        public class Entity
        {
            public string EmailAddress { get; set; } = "";
            public string? Name { get; set; }
        }

        public void Validate()
        {
            if (Recipient is null)
                throw new MailerRequiredFieldEmptyException(nameof(Recipient));
            if (!Recipient.EmailAddress.IsValidEmail())
                throw new MailerInvalidFieldFormatException(nameof(Recipient.EmailAddress), "improper e-mail address");
            if (Sender is null)
                throw new MailerRequiredFieldEmptyException(nameof(Sender));
            if (!Sender.EmailAddress.IsValidEmail())
                throw new MailerInvalidFieldFormatException(nameof(Sender.EmailAddress), "improper e-mail address");
        }
    }

    public enum EmailStatusCode
    {
        Generated = 0,
        Sent = 1,
        Failure = 2
    }
}
