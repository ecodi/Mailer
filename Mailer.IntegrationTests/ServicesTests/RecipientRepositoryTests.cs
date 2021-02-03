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
    public class RecipientRepositoryTests
    {
        private readonly IRecipientRepository _recipientRepository;
        private readonly IMongoCollection<RecipientDbModel> _recipientCollection;
        private readonly IMailerContext _context;

        public RecipientRepositoryTests(ApiFixture apiFixture)
        {
            _recipientRepository = apiFixture.GetService<IRecipientRepository>();
            _recipientCollection = ((IMongoRepository<RecipientDbModel>)_recipientRepository).GetCollection();
            _context = apiFixture.Context;
        }

        [Fact]
        public async Task ThrowsDuplicatedDefinitionException_IfDuplicatedEmailAddress()
        {
            const string emailAddress = "new@email.com";
            await _recipientRepository.InsertAsync(_context, new Recipient { EmailAddress = emailAddress });
            await Assert.ThrowsAsync<DuplicatedDefinitionException>(() => _recipientRepository.InsertAsync(_context, new Recipient { EmailAddress = emailAddress }));
        }

        [Fact]
        public async Task DoesNotThrowDuplicatedDefinitionException_IfDuplicatedEmailAddressOnDeletedEntity()
        {
            const string emailAddress = "newdeleted@email.com";
            for (var i = 0; i < 3; i++)
            {
                var inserted = new Recipient { EmailAddress = emailAddress };
                await _recipientRepository.InsertAsync(_context, inserted);
                await _recipientRepository.DeleteAsync(_context, inserted);
            }
        }

        [Fact]
        public async Task StoresHashedEmailAddressInDatabase()
        {
            var model = await _recipientRepository.GetByEmailAddressAsync(_context, "email@sink.sendgrid.net");
            var dbModel = (await _recipientCollection.FindAsync(m => m.Id == model!.Id)).FirstOrDefault();
            Assert.NotEmpty(dbModel.EmailAddress.Cipher);
            Assert.NotEqual(model!.EmailAddress, dbModel.EmailAddress.Cipher);
            Assert.NotEmpty(dbModel.EmailAddress.Hash);
            Assert.NotEqual(model.EmailAddress, dbModel.EmailAddress.Hash);
        }

        [Fact]
        public async Task IncrementsRowVersionOnUpdate()
        {
            var model = await _recipientRepository.GetByEmailAddressAsync(_context, "email3@sink.sendgrid.net");
            var prevVersion = model!.RowVersion;
            await _recipientRepository.ReplaceAsync(_context, model);
            Assert.Equal(prevVersion + 1, model.RowVersion);
            model = await _recipientRepository.GetByIdAsync(_context, model.Id);
            Assert.Equal(prevVersion + 1, model!.RowVersion);
        }

        [Fact]
        public async Task OnlyMarksAsDeletedOnDelete()
        {
            var model = await _recipientRepository.GetByEmailAddressAsync(_context, "todelete@sink.sendgrid.net");
            await _recipientRepository.DeleteAsync(_context, model!);
            Assert.Null(await _recipientRepository.GetByIdAsync(_context, model!.Id));
            var dbModel = (await _recipientCollection.FindAsync(m => m.Id == model!.Id)).FirstOrDefault();
            Assert.NotNull(dbModel);
            Assert.True(dbModel.Deleted);
        }

        [Fact]
        public async Task ThrowsInvalidVersionException_IfUnmatchedRowVersionOnUpdate()
        {
            var model = await _recipientRepository.GetByEmailAddressAsync(_context, "email@sink.sendgrid.net");
            model!.RowVersion += 1;
            await Assert.ThrowsAsync<InvalidVersionException>(() => _recipientRepository.ReplaceAsync(_context, model));
        }

        [Fact]
        public async Task ThrowsInvalidVersionException_IfUnmatchedRowVersionOnDelete()
        {
            var model = await _recipientRepository.GetByEmailAddressAsync(_context, "email@sink.sendgrid.net");
            model!.RowVersion += 1;
            await Assert.ThrowsAsync<InvalidVersionException>(() => _recipientRepository.DeleteAsync(_context, model));
        }
    }
}
