using Mailer.Domain.Models;
using Mailer.Infrastructure.Types;
using System;

namespace Mailer.Infrastructure.Models
{
    public class EmailDbModel : BaseDbModel
    {
        public Guid MailingId { get; set; }
        public Entity? Recipient { get; set; }
        public Entity? Sender { get; set; }
        public string? Subject { get; set; }
        public string? PlainBody { get; set; }
        public string? HtmlBody { get; set; }
        public EmailStatusCode StatusCode { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        public class Entity
        {
            public EncryptedString EmailAddress { get; set; } = default!;
            public string? Name { get; set; }
        }
    }
}
