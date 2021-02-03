using Mailer.Domain.Models;
using Mailer.Api.ViewModels.SenderVm;
using Mailer.Domain.Services;
using Mailer.Mapping;

namespace Mailer.Api.Controllers
{
    public class SendersController : BaseCrudController<Sender, SenderViewModel, SenderAddModel, SenderUpdateModel>
    {
        public SendersController(ISenderService service, IMapper mapper) : base(service, mapper)
        {
        }
    }
}
