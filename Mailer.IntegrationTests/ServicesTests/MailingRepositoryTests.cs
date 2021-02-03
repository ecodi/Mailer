using Mailer.Domain.Repositories;
using Mailer.Exceptions;
using Mailer.Infrastructure.Models;
using Mailer.Infrastructure.Repositories;
using Mailer.IntegrationTests.Fixtures;
using MongoDB.Driver;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Mailer.IntegrationTests.ServicesTests
{
    [Collection(CollectionNames.Services)]
    public class MailingRepositoryTests
    {
        private readonly IMailingRepository _mailingRepository;
        private readonly IMongoCollection<MailingDbModel> _mailingCollection;
        private readonly IMailerContext _context;

        public MailingRepositoryTests(ApiFixture apiFixture)
        {
            _mailingRepository = apiFixture.GetService<IMailingRepository>();
            _mailingCollection = ((IMongoRepository<MailingDbModel>)_mailingRepository).GetCollection();
            _context = apiFixture.Context;
        }

        [Fact]
        public async Task IncrementsRowVersionOnUpdate()
        {
            var model = await _mailingRepository.GetListAsync(_context).FirstAsync(r => r.SubjectTemplate == "to update");
            var prevVersion = model!.RowVersion;
            await _mailingRepository.ReplaceAsync(_context, model);
            Assert.Equal(prevVersion + 1, model.RowVersion);
            model = await _mailingRepository.GetByIdAsync(_context, model.Id);
            Assert.Equal(prevVersion + 1, model!.RowVersion);
        }

        [Fact]
        public async Task OnlyMarksAsDeletedOnDelete()
        {
            var model = await _mailingRepository.GetListAsync(_context).FirstAsync(r => r.SubjectTemplate == "to delete");
            await _mailingRepository.DeleteAsync(_context, model!);
            Assert.Null(await _mailingRepository.GetByIdAsync(_context, model!.Id));
            var dbModel = (await _mailingCollection.FindAsync(m => m.Id == model!.Id)).FirstOrDefault();
            Assert.NotNull(dbModel);
            Assert.True(dbModel.Deleted);
        }

        [Fact]
        public async Task ThrowsInvalidVersionException_IfUnmatchedRowVersionOnUpdate()
        {
            var model = await _mailingRepository.GetListAsync(_context).FirstAsync(r => r.SubjectTemplate == "read only");
            model!.RowVersion += 1;
            await Assert.ThrowsAsync<InvalidVersionException>(() => _mailingRepository.ReplaceAsync(_context, model));
        }

        [Fact]
        public async Task ThrowsInvalidVersionException_IfUnmatchedRowVersionOnDelete()
        {
            var model = await _mailingRepository.GetListAsync(_context).FirstAsync(r => r.SubjectTemplate == "read only");
            model!.RowVersion += 1;
            await Assert.ThrowsAsync<InvalidVersionException>(() => _mailingRepository.DeleteAsync(_context, model));
        }
    }
}
