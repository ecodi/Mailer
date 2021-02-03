namespace Mailer.Exceptions
{
    public class MailerRequiredFieldEmptyException : MailerValidationException
    {
        public string FieldName { get; }

        public MailerRequiredFieldEmptyException(string fieldName) : base($"Field {fieldName} is required.")
        {
            FieldName = fieldName;
        }
    }
}
