using System;

namespace Mailer
{
    public static class GuidExtensions
    {
        public static bool IsEmpty(this Guid id) => id == Guid.Empty;
    }
}
