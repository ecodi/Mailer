using System;

namespace Mailer.Api.ViewModels.RecipientVm
{
    public class RecipientViewModel
    {
        public Guid Id { get; set; }
        public int RowVersion { get; set; }
        public string EmailAddress { get; set; } = default!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }
}
