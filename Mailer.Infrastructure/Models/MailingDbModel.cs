using Mailer.Domain.Models;
using System;
using System.Collections.Generic;

namespace Mailer.Infrastructure.Models
{
    public class MailingDbModel : BaseMarkDeletedDbModel
    {
        public ICollection<Guid>? RecipientsIds { get; set; }
        public Guid SenderId { get; set; }
        public string? SubjectTemplate { get; set; }
        public string? PlainBodyTemplate { get; set; }
        public string? HtmlBodyTemplate { get; set; }
        public MailingStatusDbModel? Status { get; set; }
    }

    public class MailingStatusDbModel
    {
        public MailingStatusCode StatusCode { get; set; }
        public string? Message { get; set; }
    }
}
