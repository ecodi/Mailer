using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Mailer.Mapping;
using Mailer.Domain.Models;
using Mailer.Domain.Repositories;
using Mailer.Exceptions;
using MongoDB.Driver;

namespace Mailer.Infrastructure.Repositories
{
    public class GenericRepository<TModel, TDbModel> : IDbRepository<TModel>
        where TModel : class, IMailerModel
        where TDbModel : IMongoModel, new()
    {
        protected readonly IMapper Mapper;
        private readonly IMongoCollection<TDbModel> _collection;

        public GenericRepository(IMongoContext context, IMapper mapper)
        {
            _collection = context.GetCollection<TDbModel>();
            Mapper = mapper;
        }

        public IMongoCollection<TDbModel> GetCollection() => _collection;

        public async Task InsertAsync(IMailerContext context, TModel record)
        {
            var dbModel = GetDbModel(context, record);
            if (dbModel.Id.IsEmpty())
                dbModel.Id = Guid.NewGuid();
            try
            {
                await GetCollection().InsertOneAsync(dbModel, null, context.CancellationToken);
            }
            catch(MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                throw new DuplicatedDefinitionException("Similar data already exists.");
            }
            dbModel.ReflectVersionTo(record);
        }

        protected virtual Expression<Func<TDbModel, bool>> CreateKeyFilter(Guid id) => AdjustFilter(m => m.Id == id);

        protected virtual Expression<Func<TDbModel, bool>> CreateVersionedKeyFilter(Guid id, int rowVersion)
            => AdjustFilter(m => m.Id == id && m.RowVersion == rowVersion);

        protected virtual Expression<Func<TDbModel, bool>> AdjustFilter(Expression<Func<TDbModel, bool>>? filter)
        {
            if (typeof(IMongoMarkDeleted).IsAssignableFrom(typeof(TDbModel)))
            {
                if (filter is null)
                    return m => !((IMongoMarkDeleted)m).Deleted;

                var notDeletedPredicate = Expression.Not(Expression.Property(filter.Parameters[0], nameof(IMongoMarkDeleted.Deleted)));
                var filterBody = Expression.AndAlso(filter.Body, notDeletedPredicate);
                var resultFilter = Expression.Lambda<Func<TDbModel, bool>>(filterBody, filter.Parameters[0]);
                return resultFilter;
            }
            return filter ?? (m => true);
        }

        public virtual async Task<TModel?> GetByIdAsync(IMailerContext context, Guid id)
        {
            return Mapper.Map<TDbModel, TModel>((await GetCollection().FindAsync(CreateKeyFilter(id), new FindOptions<TDbModel> { Limit = 1 }, cancellationToken: context.CancellationToken)).FirstOrDefault());
        }

        protected async Task<TModel?> GetDbOneAsync(IMailerContext context, Expression<Func<TDbModel, bool>>? filter)
            => await GetDbListAsync(context, filter, 1).FirstOrDefaultAsync();


        public IAsyncEnumerable<TModel> GetListAsync(IMailerContext context, int? limit = null)
            => GetDbListAsync(context, null, limit);
        protected virtual async IAsyncEnumerable<TModel> GetDbListAsync(IMailerContext context, Expression<Func<TDbModel, bool>>? filter, int? limit = null)
        {
            using var asyncCursor = await GetCollection().FindAsync(AdjustFilter(filter ?? (m => true)), new FindOptions<TDbModel> { Limit = limit }, cancellationToken: context.CancellationToken);
            while (await asyncCursor.MoveNextAsync(context.CancellationToken))
            {
                foreach (var result in asyncCursor.Current)
                    yield return Mapper.Map<TDbModel, TModel>(result);
            }
        }

        public async Task ReplaceAsync(IMailerContext context, TModel record)
        {
            var dbModel = GetDbModel(context, record);
            ReplaceOneResult result;
            try
            {
                result = await GetCollection().ReplaceOneAsync(CreateVersionedKeyFilter(record.Id, record.RowVersion), dbModel, cancellationToken: context.CancellationToken);
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                throw new DuplicatedDefinitionException("Similar data already exists.");
            }
            if (result.ModifiedCount > 0)
                dbModel.ReflectVersionTo(record);
            else throw new InvalidVersionException($"{typeof(TModel).Name} with ID:{record.Id} and version:{record.RowVersion} not found.");
        }

        public async Task DeleteAsync(IMailerContext context, TModel record)
        {
            if (typeof(IMongoMarkDeleted).IsAssignableFrom(typeof(TDbModel)))
            {
                await MarkDeletedAsync(context, record);
                return;
            }
            var result = await GetCollection().DeleteOneAsync(CreateVersionedKeyFilter(record.Id, record.RowVersion), cancellationToken: context.CancellationToken);
            if (result.DeletedCount == 0)
                throw new InvalidVersionException($"{typeof(TModel).Name} with ID:{record.Id} and version:{record.RowVersion} not found.");
        }

        protected async Task MarkDeletedAsync(IMailerContext context, TModel record)
        {
            var updateDef = Builders<TDbModel>.Update.Set(o => ((IMongoMarkDeleted)o).Deleted, true);
            var result = await GetCollection().UpdateOneAsync(CreateVersionedKeyFilter(record.Id, record.RowVersion), updateDef, cancellationToken: context.CancellationToken);
            if (result.ModifiedCount == 0)
                throw new InvalidVersionException($"{typeof(TModel).Name} with ID:{record.Id} and version:{record.RowVersion} not found.");
        }

        protected TDbModel GetDbModel(IMailerContext context, TModel record)
        {
            var dbModel = Mapper.Map<TModel, TDbModel>(record);
            dbModel.LastChangeStamp = new StampDbModel { UserId = context.UserId };
            dbModel.RowVersion += 1;
            return dbModel;
        }

        public Task CreateIndexesAsync(CancellationToken cancellationToken)
        {
            var indexes = new[]
            {
                new CreateIndexModel<TDbModel>(
                    new IndexKeysDefinitionBuilder<TDbModel>().Ascending(s => s.Id).Ascending(s => s.RowVersion),
                    typeof(IMongoMarkDeleted).IsAssignableFrom(typeof(TDbModel)) ? new CreateIndexOptions<TDbModel> {
                        PartialFilterExpression = Builders<TDbModel>.Filter.Where(s => ((IMongoMarkDeleted)s).Deleted == false) } : null)
            }.Union(IndexesDefinitions());
            return GetCollection().Indexes.CreateManyAsync(indexes, cancellationToken);
        }

        protected virtual IEnumerable<CreateIndexModel<TDbModel>> IndexesDefinitions()
            => Enumerable.Empty<CreateIndexModel<TDbModel>>();
    }

    public static class MongoModelExtensions
    {
        public static void ReflectVersionTo(this IMongoModel dbModel, IMailerModel record)
        {
            record.Id = dbModel.Id;
            record.RowVersion = dbModel.RowVersion;
        }
    }
}
