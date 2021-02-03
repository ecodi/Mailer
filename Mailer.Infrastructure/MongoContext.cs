using Mailer.Infrastructure.Types;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;

namespace Mailer.Infrastructure
{
    public class MongoOptions
    {
        public string ConnectionString { get; set; } = default!;
        public string? CollectionNameSuffix { get; set; }
    }

    public class MongoContext : IMongoContext
    {
        private readonly MongoOptions _options;

        public MongoContext(IOptions<MongoOptions> optionsAccessor)
        {
            _options = optionsAccessor.Value;
            TryRegisterClassMap<EncryptedString>(cm =>
            {
                cm.MapCreator(p => EncryptedString.Raw(p.Cipher, p.Hash));
                cm.MapMember(p => p.Cipher);
                cm.MapMember(p => p.Hash);
            });

        }
        public void TryRegisterClassMap<TClass>(Action<BsonClassMap<TClass>> classMapInitializer)
        {
            try
            {
                BsonClassMap.RegisterClassMap(classMapInitializer);
            }
            catch (ArgumentException) { }
        }


        public IMongoDatabase Db
        {
            get
            {
                var mongoUrl = MongoUrl.Create(_options.ConnectionString);
                var settings = MongoClientSettings.FromUrl(mongoUrl);
                var client = new MongoClient(settings);
                return client.GetDatabase(mongoUrl.DatabaseName);
            }
        }
        public IMongoCollection<TDbModel> GetCollection<TDbModel>()
        {
            var name = typeof(TDbModel).Name.ToLower();
            var i = name.IndexOf("dbmodel", StringComparison.Ordinal);
            return Db.GetCollection<TDbModel>((i > 0 ? name.Substring(0, i) : name) + (_options.CollectionNameSuffix ?? ""));
        }
    }
}
