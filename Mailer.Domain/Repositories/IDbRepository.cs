using Mailer.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mailer.Domain.Repositories
{
    public interface IDbRepository<TModel> where TModel : class, IMailerModel
    {
        Task InsertAsync(IMailerContext context, TModel record);
        Task ReplaceAsync(IMailerContext context, TModel record);
        Task DeleteAsync(IMailerContext context, TModel record);
        Task<TModel?> GetByIdAsync(IMailerContext context, Guid id);
        IAsyncEnumerable<TModel> GetListAsync(IMailerContext context, int? limit = null);
    }
}
