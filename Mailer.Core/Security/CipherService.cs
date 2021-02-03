using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.DataProtection;
using System;
using System.Text;

namespace Mailer.Security
{
    public class HashOptions
    {
        public string? Salt { get; set; }
        public int IterationsCount { get; set; } = 1000;
        public int DerivedKeyBytes { get; set; } = 32;
    }

    public class CipherService : ICipherService
    {
        private readonly IDataProtector _protector;
        private readonly HashOptions _options;

        public CipherService(IDataProtectionProvider provider, HashOptions options, params string[] purposes)
        {
            _protector = provider.CreateProtector(purposes);
            _options = options;
        }

        public string Decrypt(string cipher) => _protector.Unprotect(cipher);
        public string Encrypt(string input) => _protector.Protect(input);
        public string Hash(string input) => Convert.ToBase64String(KeyDerivation.Pbkdf2(
            input, Encoding.ASCII.GetBytes(_options.Salt), KeyDerivationPrf.HMACSHA512, _options.IterationsCount, _options.DerivedKeyBytes));
    }
}
