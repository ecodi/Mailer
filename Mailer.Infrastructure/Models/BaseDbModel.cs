using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Mailer.Infrastructure.Models
{
    public abstract class BaseDbModel : IMongoModel
    {
        [BsonId]
        public Guid Id { get; set; }
        public int RowVersion { get; set; }
        public StampDbModel LastChangeStamp { get; set; } = default!;
    }

    public abstract class BaseMarkDeletedDbModel : BaseDbModel, IMongoMarkDeleted
    {
        public bool Deleted { get; set; }
    }
}
