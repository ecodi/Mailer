using System;

namespace Mailer.Exceptions
{
    public class MailerValidationException : Exception
    {
        public MailerValidationException(string msg) : base(msg) { }
    }
}
