using System.ComponentModel.DataAnnotations;

namespace Mailer.Api.ViewModels.SenderVm
{
    public class SenderAddModel : SenderUpdateModel
    {
        [Required, EmailAddress]
        public string EmailAddress { get; set; } = default!;
    }
}
