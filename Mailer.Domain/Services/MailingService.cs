using Mailer.Domain.Application;
using Mailer.Domain.Models;
using Mailer.Domain.Repositories;
using Mailer.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mailer.Domain.Services
{
    public class ExecuteMailingMessage : IMessage
    {
        public Guid MailingId { get; set; }
        public Guid UserId { get; set; }

        public ExecuteMailingMessage() { }
        public ExecuteMailingMessage(Guid mailingId, Guid userId)
        {
            MailingId = mailingId;
            UserId = userId;
        }
    }

    public class MailingService : GenericService<Mailing>, IMailingService
    {
        private readonly IMailingRepository _mailingRepository;
        private readonly ISenderService _senderService;
        private readonly IRecipientService _recipientService;
        private readonly IEmailRepository _emailRepository;
        private readonly IMailClient _mailClient;
        private readonly IMessageBus<ExecuteMailingMessage> _messageBus;

        public MailingService(IMailingRepository repository, IRecipientService recipientService, ISenderService senderService,
            IEmailRepository emailRepository, IMailClient mailClient, IMessageBus<ExecuteMailingMessage> messageBus) : base(repository)
        {
            _mailingRepository = repository;
            _senderService = senderService;
            _recipientService = recipientService;
            _emailRepository = emailRepository;
            _mailClient = mailClient;
            _messageBus = messageBus;
        }

        protected override async Task ValidateAsync(IMailerContext context, Mailing model)
        {
            foreach (var recipient in model.Recipients)
                if (await _recipientService.GetAsync(context, recipient.Id) is null)
                    throw new MailerNotExistingReferenceException("Recipient", recipient.Id);
            if (model.Sender != null && await _senderService.GetAsync(context, model.Sender.Id) is null)
                throw new MailerNotExistingReferenceException("Sender", model.Sender.Id);
        }

        protected override Task ValidateDeletionAsync(IMailerContext context, Mailing model)
        {
            if (model.Status.StatusCode != MailingStatusCode.Draft)
                throw new MailerValidationException("Mailing already executed.");
            return Task.CompletedTask;
        }

        public async Task ExecuteAsync(IMailerContext context, Mailing model)
        {
            if (model.Status.StatusCode != MailingStatusCode.Draft)
                throw new MailerValidationException("Mailing already executed.");
            if (string.IsNullOrEmpty(model.SubjectTemplate))
                throw new MailerRequiredFieldEmptyException("subject");
            if (string.IsNullOrEmpty(model.PlainBodyTemplate) && string.IsNullOrEmpty(model.HtmlBodyTemplate))
                throw new MailerRequiredFieldEmptyException("body");

            context.CancellationToken.ThrowIfCancellationRequested();
            model.Status.StatusCode = MailingStatusCode.Accepted;
            await _messageBus.PublishAsync(new ExecuteMailingMessage(model.Id, context.UserId));
            await _mailingRepository.ReplaceOnStatusCodeAsync(context, model, MailingStatusCode.Draft);
        }

        public async Task ProcessAsync(IMailerContext context, Mailing model)
        {
            if (model.Status.StatusCode != MailingStatusCode.Draft && model.Status.StatusCode != MailingStatusCode.Accepted)
                throw new MailerValidationException("Mailing already processed.");

            model.Status.StatusCode = MailingStatusCode.InProgress;
            await Repository.ReplaceAsync(context, model);

            var tasks = new HashSet<Task>();
            var no_concurrent = 10;
            foreach (var recipient in model.Recipients)
            {
                if (tasks.Count >= no_concurrent)
                {
                    var done = await Task.WhenAny(tasks);
                    tasks.Remove(done);
                }
                tasks.Add(PrepareAndSendEmail(recipient));
            }
            await Task.WhenAll(tasks);

            model.Status.StatusCode = MailingStatusCode.Done;
            await Repository.ReplaceAsync(context, model);

            async Task PrepareAndSendEmail(Recipient recipient)
            {
                var email = model.PrepareEmail(recipient);
                try
                {
                    await _mailClient.SendEmailAsync(email);
                    email.StatusCode = EmailStatusCode.Sent;
                }
                catch {
                    email.StatusCode = EmailStatusCode.Failure;
                }
                await _emailRepository.InsertAsync(context, email);
            }
        }
    }
}
