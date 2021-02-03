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
    public class SenderServiceTests
    {
        private readonly Mock<ISenderRepository> _senderRepositoryMock;
        private readonly Mock<IMailingRepository> _mailingRepositoryMock;
        private readonly SenderService _service;
        private readonly IMailerContext _context;

        private readonly Sender _testModel = new Sender
        {
            EmailAddress = "sender@email.com",
            Name = "i am sender"
        };

        public SenderServiceTests()
        {
            _senderRepositoryMock = new Mock<ISenderRepository>();
            _mailingRepositoryMock = new Mock<IMailingRepository>();
            _service = new SenderService(_senderRepositoryMock.Object, _mailingRepositoryMock.Object);
            _context = new Mock<IMailerContext>().Object;
        }

        public class SaveMethod : SenderServiceTests
        {
            [Fact]
            public async Task ThrowsDuplicatedDefinitionException_IfSenderWithEmailAlreadyExists()
            {
                _senderRepositoryMock.Setup(m => m.GetByEmailAddressAsync(_context, _testModel.EmailAddress))
                    .ReturnsAsync(new Sender { Id = Guid.NewGuid() });
                await Assert.ThrowsAsync<DuplicatedDefinitionException>(() => _service.SaveAsync(_context, _testModel));
                _senderRepositoryMock.Verify(m => m.InsertAsync(It.IsAny<IMailerContext>(), It.IsAny<Sender>()), Times.Never);
            }

            [Fact]
            public async Task InsertsModelToDatabase_IfNoErrors()
            {
                await _service.SaveAsync(_context, _testModel);
                _senderRepositoryMock.Verify(m => m.InsertAsync(It.IsAny<IMailerContext>(), _testModel), Times.Once);
            }
        }

        public class DeleteMethod : SenderServiceTests
        {
            [Fact]
            public async Task ThrowsIntegrityException_IfMailingWithSenderExists()
            {
                _testModel.Id = Guid.NewGuid();
                _mailingRepositoryMock.Setup(m => m.GetListBySenderAsync(It.IsAny<IMailerContext>(), _testModel, It.IsAny<int?>()))
                    .Returns(new[] { new Mailing { Id = Guid.NewGuid(), Sender = _testModel } }.ToAsyncEnumerable());
                await Assert.ThrowsAsync<IntegrityException>(() => _service.DeleteAsync(_context, _testModel));
                _senderRepositoryMock.Verify(m => m.DeleteAsync(It.IsAny<IMailerContext>(), It.IsAny<Sender>()), Times.Never);
            }
        }
    }
}
