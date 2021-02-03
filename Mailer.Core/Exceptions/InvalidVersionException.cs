using System;

namespace Mailer.Exceptions
{
    public class InvalidVersionException : Exception
    {
        public InvalidVersionException(string msg) : base(msg) { }
    }
}
