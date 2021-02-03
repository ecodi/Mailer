using Mailer.Security;

namespace Mailer.Infrastructure.Types
{
    public class EncryptedString
    {
        public string? Cipher { get; }
        public string? Hash { get; }

        private EncryptedString(string? value, ICipherService cipherService)
        {
            Cipher = value is null ? null : cipherService.Encrypt(value);
            Hash = value is null ? null : cipherService.Hash(value);
        }
        private EncryptedString(string? cipher, string? hash)
        {
            Cipher = cipher;
            Hash = hash;
        }

        public string? GetValue(ICipherService cipherService)
        {
            return Cipher is null ? null : cipherService.Decrypt(Cipher);
        }

        public static EncryptedString Create(string? value, ICipherService cipherService)
            => new EncryptedString(value, cipherService);
        public static EncryptedString Raw(string? cipher, string? hash)
            => new EncryptedString(cipher, hash);

        public override bool Equals(object obj)
            => obj is EncryptedString encrypted && encrypted.Hash == Hash;
        public override int GetHashCode() => Hash?.GetHashCode() ?? 0;
        public static bool operator ==(EncryptedString lhs, EncryptedString rhs) => lhs.Equals(rhs);
        public static bool operator !=(EncryptedString lhs, EncryptedString rhs) => !lhs.Equals(rhs);
    }
}
