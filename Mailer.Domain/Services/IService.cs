using Mailer.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mailer.Domain.Services
{
    public interface IService<TModel> where TModel : class, IMailerModel
    {
        Task<TModel?> GetAsync(IMailerContext context, Guid id);
        Task<TModel> GetAndEnsureVersionAsync(IMailerContext context, Guid id, int rowVersion);
        IAsyncEnumerable<TModel> GetListAsync(IMailerContext context);
        Task SaveAsync(IMailerContext context, TModel model);
        Task DeleteAsync(IMailerContext context, TModel model);
    }
}
