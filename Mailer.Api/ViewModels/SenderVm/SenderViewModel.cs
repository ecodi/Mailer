using System;

namespace Mailer.Api.ViewModels.SenderVm
{
    public class SenderViewModel
    {
        public Guid Id { get; set; }
        public int RowVersion { get; set; }
        public string EmailAddress { get; set; } = default!;
        public string? Name { get; set; }
    }
}
