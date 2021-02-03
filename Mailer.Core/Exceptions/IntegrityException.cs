namespace Mailer.Exceptions
{
    public class IntegrityException : MailerValidationException
    {
        public IntegrityException(string msg) : base(msg) { }
    }
}
