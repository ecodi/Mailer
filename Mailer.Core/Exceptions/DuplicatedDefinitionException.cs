namespace Mailer.Exceptions
{
    public class DuplicatedDefinitionException : MailerValidationException
    {
        public DuplicatedDefinitionException(string msg) : base(msg) { }
    }
}
