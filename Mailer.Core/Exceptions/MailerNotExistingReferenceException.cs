using System;

namespace Mailer.Exceptions
{
    public class MailerNotExistingReferenceException : MailerValidationException
    {
        public MailerNotExistingReferenceException(string modelName, Guid id) : base($"{modelName} with ID:{id} does not exist.") { }
    }
}
