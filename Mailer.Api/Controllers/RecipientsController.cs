using Mailer.Domain.Models;
using Mailer.Api.ViewModels.RecipientVm;
using Mailer.Domain.Services;
using Mailer.Mapping;

namespace Mailer.Api.Controllers
{
    public class RecipientsController : BaseCrudController<Recipient, RecipientViewModel, RecipientAddModel, RecipientUpdateModel>
    {
        public RecipientsController(IRecipientService service, IMapper mapper) : base(service, mapper)
        {
        }
    }
}
