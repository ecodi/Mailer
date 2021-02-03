using System;

namespace Mailer.Infrastructure
{
    public interface IMongoModel
    {
        Guid Id { get; set; }
        public int RowVersion { get; set; }
        StampDbModel LastChangeStamp { get; set; }
    }

    public class StampDbModel
    {
        public Guid UserId { get; set; }
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    }
}
