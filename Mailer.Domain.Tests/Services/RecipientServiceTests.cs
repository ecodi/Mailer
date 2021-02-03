using Mailer.Domain.Models;
using Mailer.Domain.Repositories;
using Mailer.Domain.Services;
using Mailer.Exceptions;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Mailer.Domain.Tests.Services
{
    public class RecipientServiceTests
    {
        private readonly Mock<IRecipientRepository> _recipientRepositoryMock;
        private readonly Mock<IMailingRepository> _mailingRepositoryMock;
        private readonly RecipientService _service;
        private readonly IMailerContext _context;

        private readonly Recipient _testModel = new Recipient
        {
            EmailAddress = "recipient@email.com",
            FirstName = "Chuck",
            LastName = "Norris"
        };

        public RecipientServiceTests()
        {
            _recipientRepositoryMock = new Mock<IRecipientRepository>();
            _mailingRepositoryMock = new Mock<IMailingRepository>();
            _service = new RecipientService(_recipientRepositoryMock.Object, _mailingRepositoryMock.Object);
            _context = new Mock<IMailerContext>().Object;
        }

        public class SaveMethod : RecipientServiceTests
        {
            [Fact]
            public async Task ThrowsDuplicatedDefinitionException_IfRecipientWithEmailAlreadyExists()
            {
                _recipientRepositoryMock.Setup(m => m.GetByEmailAddressAsync(_context, _testModel.EmailAddress))
                    .ReturnsAsync(new Recipient { Id = Guid.NewGuid() });
                await Assert.ThrowsAsync<DuplicatedDefinitionException>(() => _service.SaveAsync(_context, _testModel));
                _recipientRepositoryMock.Verify(m => m.InsertAsync(It.IsAny<IMailerContext>(), It.IsAny<Recipient>()), Times.Never);
            }

            [Fact]
            public async Task InsertsModelToDatabase_IfNoErrors()
            {
                await _service.SaveAsync(_context, _testModel);
                _recipientRepositoryMock.Verify(m => m.InsertAsync(It.IsAny<IMailerContext>(), _testModel), Times.Once);
            }
        }

        public class DeleteMethod : RecipientServiceTests
        {
            [Fact]
            public async Task ThrowsIntegrityException_IfMailingWithRecipientExists()
            {
                _testModel.Id = Guid.NewGuid();
                _mailingRepositoryMock.Setup(m => m.GetListByRecipientAsync(It.IsAny<IMailerContext>(), _testModel, It.IsAny<int?>()))
                    .Returns(new[] { new Mailing { Id = Guid.NewGuid(), Recipients = new[] { _testModel } } }.ToAsyncEnumerable());
                await Assert.ThrowsAsync<IntegrityException>(() => _service.DeleteAsync(_context, _testModel));
                _recipientRepositoryMock.Verify(m => m.DeleteAsync(It.IsAny<IMailerContext>(), It.IsAny<Recipient>()), Times.Never);
            }
        }
    }
}
