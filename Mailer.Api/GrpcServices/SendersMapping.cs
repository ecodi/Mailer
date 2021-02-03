using Mailer.API.Protos.Senders;
using Mailer.Mapping;

namespace Mailer.Api.GrpcServices
{
    public class SendersMapping : BaseMapping
    {
        public SendersMapping()
        {
            CreateMap<UpdateSenderRequest.Types.Data, Domain.Models.Sender>();
            CreateMap<AddSenderRequest.Types.Data, Domain.Models.Sender>();
            CreateMap<Domain.Models.Sender, Sender>().AddTransform<string>(s => s ?? "");
        }
    }
}
