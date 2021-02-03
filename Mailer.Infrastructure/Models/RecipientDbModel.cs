using Mailer.Infrastructure.Types;

namespace Mailer.Infrastructure.Models
{
    public class RecipientDbModel : BaseMarkDeletedDbModel
    {
        public EncryptedString EmailAddress { get; set; } = default!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }
}
