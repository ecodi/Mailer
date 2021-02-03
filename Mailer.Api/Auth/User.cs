using System;
using System.Collections.Generic;

namespace Mailer.Api.Auth
{
    public class User
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string Login { get; set; } = default!;
        public string HashedPassword { get; set; } = default!;

        public bool Verify(string login, string hashedPassword) => string.Equals(Login, login, StringComparison.OrdinalIgnoreCase) && string.Equals(hashedPassword, HashedPassword);
    }

    public class UsersList : List<User> { }
}
