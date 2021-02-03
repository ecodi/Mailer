namespace Mailer.Exceptions
{
    public class MailerInvalidFieldFormatException : MailerValidationException
    {
        public MailerInvalidFieldFormatException(string fieldName, string details) : base($"Field {fieldName} has invalid format: {details}.") { }
    }
}
