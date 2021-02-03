using Mailer.Mapping;
using Mailer.Domain.Models;
using Mailer.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mailer.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public abstract class BaseCrudController<TModel, TViewModel, TAddModel, TUpdateModel> : ControllerBase
        where TModel : class, IMailerModel
        where TViewModel : class
    {
        protected readonly IService<TModel> Service;
        protected readonly IMapper Mapper;

        protected BaseCrudController(IService<TModel> service, IMapper mapper)
        {
            Service = service;
            Mapper = mapper;
        }

        [HttpGet("{id}")]
        public async Task<TViewModel?> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            return Mapper.Map<TModel?, TViewModel?>(await Service.GetAsync(HttpContext.GetMailerContext(cancellationToken), id));
        }

        [HttpGet("")]
        public IAsyncEnumerable<TViewModel> GetListAsync(CancellationToken cancellationToken)
        {
            return Mapper.MapAsyncEnumerable<TModel, TViewModel>(Service.GetListAsync(HttpContext.GetMailerContext(cancellationToken)));
        }

        [HttpPost]
        public async Task<TViewModel> InsertAsync(TAddModel vm, CancellationToken cancellationToken)
        {
            var model = Mapper.Map<TAddModel, TModel>(vm);
            await Service.SaveAsync(HttpContext.GetMailerContext(cancellationToken), model);
            return Mapper.Map<TModel, TViewModel>(model!);
        }

        [HttpPut("{id}/{rowVersion:int}")]
        public async Task<ActionResult<TViewModel>> UpdateAsync(Guid id, int rowVersion, TUpdateModel vm, CancellationToken cancellationToken)
        {
            var model = await Service.GetAndEnsureVersionAsync(HttpContext.GetMailerContext(cancellationToken), id, rowVersion);
            Mapper.Map(vm, model);
            await Service.SaveAsync(HttpContext.GetMailerContext(cancellationToken), model);
            return Mapper.Map<TModel, TViewModel>(model!);
        }

        [HttpDelete("{id}/{rowVersion:int}")]
        public async Task<ActionResult> RemoveAsync(Guid id, int rowVersion, CancellationToken cancellationToken)
        {
            var model = await Service.GetAndEnsureVersionAsync(HttpContext.GetMailerContext(cancellationToken), id, rowVersion);
            await Service.DeleteAsync(HttpContext.GetMailerContext(cancellationToken), model);
            return new OkResult();
        }
    }
}
