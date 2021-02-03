using System;
using System.Threading.Tasks;
using Mailer.Domain.Models;
using Mailer.Domain.Services;
using Mailer.Api.ViewModels.MailingVm;
using Microsoft.AspNetCore.Mvc;
using Mailer.Mapping;
using System.Threading;

namespace Mailer.Api.Controllers
{
    public class MailingsController : BaseCrudController<Mailing, MailingViewModel, MailingAddModel, MailingUpdateModel>
    {
        private readonly IMailingService _mailingService;

        public MailingsController(IMailingService service, IMapper mapper) : base(service, mapper)
        {
            _mailingService = service;
        }

        [HttpPost("{id}/{rowVersion:int}/emails")]
        public async Task<ActionResult> SendAsync(Guid id, int rowVersion, CancellationToken cancellationToken)
        {
            var model = await _mailingService.GetAndEnsureVersionAsync(HttpContext.GetMailerContext(cancellationToken), id, rowVersion);
            await _mailingService.ExecuteAsync(HttpContext.GetMailerContext(cancellationToken), model);
            return new AcceptedResult(Url.Action(nameof(GetAsync), model.Id), Mapper.Map<Mailing, MailingViewModel>(model));
        }
    }
}
