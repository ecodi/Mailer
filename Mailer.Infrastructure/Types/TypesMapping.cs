using Mailer.Mapping;
using Mailer.Infrastructure.Security;

namespace Mailer.Infrastructure.Types
{
    public class TypesMapping : BaseMapping
    {
        public TypesMapping(IDbProtector protector)
        {
            CreateMap<string?, EncryptedString>()
                .ConstructUsing(value => EncryptedString.Create(value, protector));
            CreateMap<EncryptedString, string?>()
                .ConstructUsing(encrypted => encrypted.GetValue(protector));
        }
    }
}
