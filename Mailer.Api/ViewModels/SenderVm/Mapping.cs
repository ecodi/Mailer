using Mailer.Domain.Models;
using Mailer.Mapping;

namespace Mailer.Api.ViewModels.SenderVm
{
    public class Mapping : BaseMapping
    {
        public Mapping()
        {
            CreateMap<SenderUpdateModel, Sender>().IncludeAllDerived();
            CreateMap<SenderAddModel, Sender>();
            CreateMap<Sender, SenderViewModel>();
        }
    }
}
