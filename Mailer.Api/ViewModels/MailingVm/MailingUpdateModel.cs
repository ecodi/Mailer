using System;
using System.Collections.Generic;

namespace Mailer.Api.ViewModels.MailingVm
{
    public class MailingUpdateModel
    {
        public ICollection<Guid>? RecipientsIds { get; set; }
        public Guid SenderId { get; set; }
        public string? SubjectTemplate { get; set; }
        public string? PlainBodyTemplate { get; set; }
        public string? HtmlBodyTemplate { get; set; }
    }
}
