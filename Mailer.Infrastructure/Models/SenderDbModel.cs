using Mailer.Infrastructure.Types;

namespace Mailer.Infrastructure.Models
{
    public class SenderDbModel : BaseMarkDeletedDbModel
    {
        public EncryptedString EmailAddress { get; set; } = default!;
        public string? Name { get; set; }
    }
}
