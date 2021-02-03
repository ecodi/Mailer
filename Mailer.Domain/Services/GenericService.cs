using Mailer.Domain.Models;
using Mailer.Domain.Repositories;
using Mailer.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mailer.Domain.Services
{
    public class GenericService<TModel> : IService<TModel> where TModel:class, IMailerModel
    {
        protected readonly IDbRepository<TModel> Repository;

        public GenericService(IDbRepository<TModel> dbRepository)
        {
            Repository = dbRepository;
        }

        public Task<TModel?> GetAsync(IMailerContext context, Guid id) => Repository.GetByIdAsync(context, id);

        public async Task<TModel> GetAndEnsureVersionAsync(IMailerContext context, Guid id, int rowVersion)
        {
            var model = await GetAsync(context, id);
            if (model is null) throw new NotFoundException($"Entity {typeof(TModel).Name} ID:{id} not found.");
            if (model.RowVersion != rowVersion) throw new InvalidVersionException($"Invalid version, expected: {model.RowVersion}, got: {rowVersion}.");
            return model;
        }

        public IAsyncEnumerable<TModel> GetListAsync(IMailerContext context) => Repository.GetListAsync(context);

        protected virtual Task ValidateAsync(IMailerContext context, TModel model) => Task.CompletedTask;

        public async Task SaveAsync(IMailerContext context, TModel model)
        {
            model.Validate();
            await ValidateAsync(context, model);
            if (model.Id.IsEmpty()) await Repository.InsertAsync(context, model);
            else await Repository.ReplaceAsync(context, model);
        }

        protected virtual Task ValidateDeletionAsync(IMailerContext context, TModel model) => Task.CompletedTask;
        
        public async Task DeleteAsync(IMailerContext context, TModel model)
        {
            await ValidateDeletionAsync(context, model);
            await Repository.DeleteAsync(context, model);
        }
    }
}
