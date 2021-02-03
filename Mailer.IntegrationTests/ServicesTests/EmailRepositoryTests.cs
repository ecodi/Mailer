using Mailer.Domain.Models;
using Mailer.Domain.Repositories;
using Mailer.Infrastructure.Models;
using Mailer.Infrastructure.Repositories;
using Mailer.IntegrationTests.Fixtures;
using MongoDB.Driver;
using System.Threading.Tasks;
using Xunit;

namespace Mailer.IntegrationTests.ServicesTests
{
    [Collection(CollectionNames.Services)]
    public class EmailRepositoryTests
    {
        private readonly IEmailRepository _emailRepository;
        private readonly IMongoCollection<EmailDbModel> _emailCollection;
        private readonly IMailerContext _context;

        public EmailRepositoryTests(ApiFixture apiFixture)
        {
            _emailRepository = apiFixture.GetService<IEmailRepository>();
            _emailCollection = ((IMongoRepository<EmailDbModel>)_emailRepository).GetCollection();
            _context = apiFixture.Context;
        }

        [Fact]
        public async Task StoresHashedEmailAddressInDatabase()
        {
            var model = new Email { Recipient = new Email.Entity { EmailAddress = "new@email.com" }, Sender = new Email.Entity { EmailAddress = "new2@email.com" } };
            await _emailRepository.InsertAsync(_context, model);
            var dbModel = (await _emailCollection.FindAsync(m => m.Id == model!.Id)).FirstOrDefault();
            foreach (var entity in new[] { dbModel.Recipient, dbModel.Sender })
            {
                Assert.NotEmpty(entity!.EmailAddress.Cipher);
                Assert.NotEqual(model.Recipient.EmailAddress, entity.EmailAddress.Cipher);
                Assert.NotEqual(model.Sender.EmailAddress, entity.EmailAddress.Cipher);
                Assert.NotEmpty(entity.EmailAddress.Hash);
                Assert.NotEqual(model.Recipient.EmailAddress, entity.EmailAddress.Hash);
                Assert.NotEqual(model.Sender.EmailAddress, entity.EmailAddress.Hash);
            }
        }

        [Fact]
        public async Task RemovesEntityFromDatabaseOnDelete()
        {
            var model = new Email { Recipient = new Email.Entity { EmailAddress = "new@email.com" }, Sender = new Email.Entity { EmailAddress = "new2@email.com" } };
            await _emailRepository.InsertAsync(_context, model);
            Assert.NotNull((await _emailCollection.FindAsync(m => m.Id == model!.Id)).FirstOrDefault());
            await _emailRepository.DeleteAsync(_context, model);
            Assert.Null((await _emailCollection.FindAsync(m => m.Id == model.Id)).FirstOrDefault());
        }
    }
}
