using Mailer.Domain.Models;
using Mailer.Api.ViewModels.RecipientVm;
using Mailer.Api.ViewModels.SenderVm;
using System;
using System.Collections.Generic;

namespace Mailer.Api.ViewModels.MailingVm
{
    public class MailingViewModel
    {
        public Guid Id { get; set; }
        public int RowVersion { get; set; }
        public ICollection<RecipientViewModel>? Recipients { get; set; }
        public SenderViewModel? Sender { get; set; }
        public string? SubjectTemplate { get; set; }
        public string? PlainBodyTemplate { get; set; }
        public string? HtmlBodyTemplate { get; set; }
        public MailingStatusViewModel Status { get; set; } = default!;
    }

    public class MailingStatusViewModel
    {
        public MailingStatusCode StatusCode { get; set; }
        public string? Message { get; set; }
    }
}
