using Mailer.API.Protos.Recipients;
using Mailer.Mapping;

namespace Mailer.Api.GrpcServices
{
    public class RecipientsMapping : BaseMapping
    {
        public RecipientsMapping()
        {
            CreateMap<UpdateRecipientRequest.Types.Data, Domain.Models.Recipient>();
            CreateMap<AddRecipientRequest.Types.Data, Domain.Models.Recipient>();
            CreateMap<Domain.Models.Recipient, Recipient>().AddTransform<string>(s => s ?? "");
        }
    }
}
