using Mailer.Exceptions;
using System;
using System.Net.Mail;

namespace Mailer
{
    public static class StringExtensions
    {
        public static bool IsValidEmail(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;
            try
            {
                var _ = new MailAddress(value);
            }
            catch (FormatException)
            {
                return false;
            }
            return true;
        }

        public static Guid ToGuid(this string value, string fieldName = "Id")
        {
            try
            {
                return new Guid(value);
            }
            catch
            {
                throw new MailerInvalidFieldFormatException(fieldName, "not a uuid");
            }
        }
    }
}
