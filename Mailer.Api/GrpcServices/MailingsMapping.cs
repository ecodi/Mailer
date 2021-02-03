using Mailer.API.Protos.Mailings;
using Mailer.Mapping;
using System.Linq;

namespace Mailer.Api.GrpcServices
{
    public class MailingsMapping : BaseMapping
    {
        public MailingsMapping()
        {
            CreateMap<Domain.Models.MailingStatusCode, MailingStatus.Types.MailingStatusCode>()
               .ConvertUsingEnumMapping(opt => opt
                   .MapValue(Domain.Models.MailingStatusCode.Draft, MailingStatus.Types.MailingStatusCode.Draft)
                   .MapValue(Domain.Models.MailingStatusCode.InProgress, MailingStatus.Types.MailingStatusCode.InProgress)
                   .MapValue(Domain.Models.MailingStatusCode.Done, MailingStatus.Types.MailingStatusCode.Done)
                   .MapValue(Domain.Models.MailingStatusCode.Accepted, MailingStatus.Types.MailingStatusCode.Accepted)
               ).ReverseMap();

            CreateMap<Domain.Models.MailingStatus, MailingStatus>().AddTransform<string>(s => s ?? "");
            CreateMap<UpdateMailingRequest.Types.Data, Domain.Models.Mailing>()
                .ForMember(m => m.Recipients, e => e.MapFrom(vm => vm.RecipientsIds == null ? null : vm.RecipientsIds.Select(id => new Domain.Models.Recipient { Id = id.ToGuid("Id") })))
                .ForMember(m => m.Sender, e => e.MapFrom(vm => new Domain.Models.Sender { Id = vm.SenderId.ToGuid("Id") }));
            CreateMap<AddMailingRequest.Types.Data, Domain.Models.Mailing>()
                .ForMember(m => m.Recipients, e => e.MapFrom(vm => vm.RecipientsIds == null ? null : vm.RecipientsIds.Select(id => new Domain.Models.Recipient { Id = id.ToGuid("Id") })))
                .ForMember(m => m.Sender, e => e.MapFrom(vm => new Domain.Models.Sender { Id = vm.SenderId.ToGuid("Id") }));
            CreateMap<Domain.Models.Mailing, Mailing>().AddTransform<string>(s => s ?? "");
        }
    }
}
