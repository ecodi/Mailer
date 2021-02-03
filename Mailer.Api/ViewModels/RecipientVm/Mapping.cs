using Mailer.Domain.Models;
using Mailer.Mapping;

namespace Mailer.Api.ViewModels.RecipientVm
{
    public class Mapping : BaseMapping
    {
        public Mapping()
        {
            CreateMap<RecipientUpdateModel, Recipient>().IncludeAllDerived();
            CreateMap<RecipientAddModel, Recipient>();
            CreateMap<Recipient, RecipientViewModel>();
        }
    }
}
