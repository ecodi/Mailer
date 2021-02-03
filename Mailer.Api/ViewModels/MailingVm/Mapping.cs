using Mailer.Domain.Models;
using Mailer.Mapping;
using System.Linq;

namespace Mailer.Api.ViewModels.MailingVm
{
    public class Mapping : BaseMapping
    {
        public Mapping()
        {
            CreateMap<MailingUpdateModel, Mailing>().IncludeAllDerived()
                .ForMember(m => m.Recipients, e => e.MapFrom(vm => vm.RecipientsIds == null ? null : vm.RecipientsIds.Select(id => new Recipient { Id = id })))
                .ForMember(m => m.Sender, e => e.MapFrom(vm => new Sender { Id = vm.SenderId }));
            CreateMap<MailingAddModel, Mailing>();
            CreateMap<MailingStatus, MailingStatusViewModel>();
            CreateMap<Mailing, MailingViewModel>();
        }
    }
}
