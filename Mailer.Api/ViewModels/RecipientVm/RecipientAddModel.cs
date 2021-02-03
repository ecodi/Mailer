using System.ComponentModel.DataAnnotations;

namespace Mailer.Api.ViewModels.RecipientVm
{
    public class RecipientAddModel : RecipientUpdateModel
    {
        [Required, EmailAddress]
        public string EmailAddress { get; set; } = default!;
    }
}
