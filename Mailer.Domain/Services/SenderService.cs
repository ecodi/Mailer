using Mailer.Domain.Models;
using Mailer.Domain.Repositories;
using Mailer.Exceptions;
using System.Linq;
using System.Threading.Tasks;

namespace Mailer.Domain.Services
{
    public class SenderService : GenericService<Sender>, ISenderService
    {
        private readonly ISenderRepository _senderRepository;
        private readonly IMailingRepository _mailingRepository;

        public SenderService(ISenderRepository repository, IMailingRepository mailingRepository) : base(repository) {
            _senderRepository = repository;
            _mailingRepository = mailingRepository;
        }

        protected override async Task ValidateAsync(IMailerContext context, Sender model)
        {
            var existing = await _senderRepository.GetByEmailAddressAsync(context, model.EmailAddress);
            if (existing != null && model.Id != existing.Id)
                throw new DuplicatedDefinitionException($"Sender with e-mail address {model.EmailAddress} already exists.");
        }

        protected override async Task ValidateDeletionAsync(IMailerContext context, Sender model)
        {
            if (await _mailingRepository.GetListBySenderAsync(context, model, 1).AnyAsync())
                throw new IntegrityException("Sender is used in mailing.");
        }
    }
}
