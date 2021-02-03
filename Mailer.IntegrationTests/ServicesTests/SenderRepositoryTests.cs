using Mailer.Domain.Models;
using Mailer.Domain.Repositories;
using Mailer.Exceptions;
using Mailer.Infrastructure.Models;
using Mailer.Infrastructure.Repositories;
using Mailer.IntegrationTests.Fixtures;
using MongoDB.Driver;
using System.Threading.Tasks;
using Xunit;

namespace Mailer.IntegrationTests.ServicesTests
{
    [Collection(CollectionNames.Services)]
    public class SenderRepositoryTests
    {
        private readonly ISenderRepository _senderRepository;
        private readonly IMongoCollection<SenderDbModel> _senderCollection;
        private readonly IMailerContext _context;

        public SenderRepositoryTests(ApiFixture apiFixture)
        {
            _senderRepository = apiFixture.GetService<ISenderRepository>();
            _senderCollection = ((IMongoRepository<SenderDbModel>)_senderRepository).GetCollection();
            _context = apiFixture.Context;
        }

        [Fact]
        public async Task ThrowsDuplicatedDefinitionException_IfDuplicatedEmailAddress()
        {
            const string emailAddress = "new@email.com";
            await _senderRepository.InsertAsync(_context, new Sender { EmailAddress = emailAddress });
            await Assert.ThrowsAsync<DuplicatedDefinitionException>(() => _senderRepository.InsertAsync(_context, new Sender { EmailAddress = emailAddress }));
        }

        [Fact]
        public async Task DoesNotThrowDuplicatedDefinitionException_IfDuplicatedEmailAddressOnDeletedEntity()
        {
            const string emailAddress = "newdeleted@email.com";
            for (var i = 0; i < 3; i++)
            {
                var inserted = new Sender { EmailAddress = emailAddress };
                await _senderRepository.InsertAsync(_context, inserted);
                await _senderRepository.DeleteAsync(_context, inserted);
            }
        }

        [Fact]
        public async Task StoresHashedEmailAddressInDatabase()
        {
            var model = await _senderRepository.GetByEmailAddressAsync(_context, "sender@email.com");
            var dbModel = (await _senderCollection.FindAsync(m => m.Id == model!.Id)).FirstOrDefault();
            Assert.NotEmpty(dbModel.EmailAddress.Cipher);
            Assert.NotEqual(model!.EmailAddress, dbModel.EmailAddress.Cipher);
            Assert.NotEmpty(dbModel.EmailAddress.Hash);
            Assert.NotEqual(model.EmailAddress, dbModel.EmailAddress.Hash);
        }

        [Fact]
        public async Task IncrementsRowVersionOnUpdate()
        {
            var model = await _senderRepository.GetByEmailAddressAsync(_context, "sender2@email.com");
            var prevVersion = model!.RowVersion;
            await _senderRepository.ReplaceAsync(_context, model);
            Assert.Equal(prevVersion + 1, model.RowVersion);
            model = await _senderRepository.GetByIdAsync(_context, model.Id);
            Assert.Equal(prevVersion + 1, model!.RowVersion);
        }

        [Fact]
        public async Task OnlyMarksAsDeletedOnDelete()
        {
            var model = await _senderRepository.GetByEmailAddressAsync(_context, "todelete@email.com");
            await _senderRepository.DeleteAsync(_context, model!);
            Assert.Null(await _senderRepository.GetByIdAsync(_context, model!.Id));
            var dbModel = (await _senderCollection.FindAsync(m => m.Id == model!.Id)).FirstOrDefault();
            Assert.NotNull(dbModel);
            Assert.True(dbModel.Deleted);
        }

        [Fact]
        public async Task ThrowsInvalidVersionException_IfUnmatchedRowVersionOnUpdate()
        {
            var model = await _senderRepository.GetByEmailAddressAsync(_context, "sender@email.com");
            model!.RowVersion += 1;
            await Assert.ThrowsAsync<InvalidVersionException>(() => _senderRepository.ReplaceAsync(_context, model));
        }

        [Fact]
        public async Task ThrowsInvalidVersionException_IfUnmatchedRowVersionOnDelete()
        {
            var model = await _senderRepository.GetByEmailAddressAsync(_context, "sender@email.com");
            model!.RowVersion += 1;
            await Assert.ThrowsAsync<InvalidVersionException>(() => _senderRepository.DeleteAsync(_context, model));
        }
    }
}
