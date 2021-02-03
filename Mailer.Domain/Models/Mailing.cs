using Mailer.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mailer.Domain.Models
{
    public class Mailing : BaseModel, IMailerModel
    {
        public ICollection<Recipient> Recipients { get; set; } = default!;
        public Sender Sender { get; set; } = default!;
        public string? SubjectTemplate { get; set; }
        public string? PlainBodyTemplate { get; set; }
        public string? HtmlBodyTemplate { get; set; }
        public MailingStatus Status { get; set; } = new MailingStatus();

        public void Validate()
        {
            if (Recipients is null || !Recipients.Any())
                throw new MailerRequiredFieldEmptyException(nameof(Recipients));
            if (Sender is null)
                throw new MailerRequiredFieldEmptyException(nameof(Sender));
        }

        public Email PrepareEmail(Recipient recipient)
        {
            return new Email
            {
                MailingId = Id,
                Recipient = new Email.Entity { EmailAddress = recipient.EmailAddress, Name = $"{(recipient.FirstName ?? "")} {(recipient.LastName ?? "")}" },
                Sender = Sender is null ? null : new Email.Entity { EmailAddress = Sender.EmailAddress, Name = Sender.Name },
                Subject = FormatTemplate(recipient, SubjectTemplate),
                PlainBody = FormatTemplate(recipient, PlainBodyTemplate),
                HtmlBody = FormatTemplate(recipient, HtmlBodyTemplate)
            };
        }

        private string? FormatTemplate(Recipient recipient, string? value)
        {
            if (value is null) return null;
            value = value.Replace("{{recipient.FirstName}}", recipient.FirstName, StringComparison.OrdinalIgnoreCase);
            value = value.Replace("{{recipient.LastName}}", recipient.LastName, StringComparison.OrdinalIgnoreCase);
            value = value.Replace("{{recipient.EmailAddress}}", recipient.EmailAddress, StringComparison.OrdinalIgnoreCase);
            value = value.Replace("{{sender.Name}}", Sender.Name, StringComparison.OrdinalIgnoreCase);
            value = value.Replace("{{sender.EmailAddress}}", Sender.EmailAddress, StringComparison.OrdinalIgnoreCase);
            return value;
        }
    }

    public enum MailingStatusCode
    {
        Draft = 0,
        InProgress = 1,
        Done = 2,
        Accepted = 3
    }
    public class MailingStatus
    {
        public MailingStatusCode StatusCode { get; set; }
        public string? Message { get; set; }
    }
}
