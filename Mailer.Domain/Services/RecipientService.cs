using Mailer.Domain.Models;
using Mailer.Domain.Repositories;
using Mailer.Exceptions;
using System.Linq;
using System.Threading.Tasks;

namespace Mailer.Domain.Services
{
    public class RecipientService : GenericService<Recipient>, IRecipientService
    {
        private readonly IRecipientRepository _recipientRepository;
        private readonly IMailingRepository _mailingRepository;

        public RecipientService(IRecipientRepository repository, IMailingRepository mailingRepository) : base(repository) {
            _recipientRepository = repository;
            _mailingRepository = mailingRepository;
        }

        protected override async Task ValidateAsync(IMailerContext context, Recipient model)
        {
            var existing = await _recipientRepository.GetByEmailAddressAsync(context, model.EmailAddress);
            if (existing != null && model.Id != existing.Id)
                throw new DuplicatedDefinitionException($"Recipient with e-mail address {model.EmailAddress} already exists.");
        }

        protected override async Task ValidateDeletionAsync(IMailerContext context, Recipient model)
        {
            if (await _mailingRepository.GetListByRecipientAsync(context, model, 1).AnyAsync())
                throw new IntegrityException("Recipient is used in mailing.");
        }
    }
}
