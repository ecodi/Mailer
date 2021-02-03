namespace Mailer.Security
{
    public interface ICipherService
    {
        string Encrypt(string input);
        string Decrypt(string cipher);
        string Hash(string input);
    }
}
