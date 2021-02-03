using Mailer.Domain.Application;
using Mailer.Domain.Models;
using Mailer.Infrastructure.Connectors;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mailer.IntegrationTests.Fixtures
{
    public class DummyMailClient : IMailClient
    {
        private readonly ConcurrentBag<Email> _sentEmails = new ConcurrentBag<Email>();
        public IReadOnlyCollection<Email> SendEmails => _sentEmails;

        private readonly SendGridMailClient _sendGrid;

        public DummyMailClient(SendGridMailClient sendGrid)
        {
            _sendGrid = sendGrid;
            sendGrid.Initialize();
        }

        public async Task SendEmailAsync(Email email)
        {
            if (!email.Recipient.EmailAddress.EndsWith("@sink.sendgrid.net"))
                throw new ArgumentException($"Trying to send e-mail to disallowed address: {email.Recipient.EmailAddress}");
            await _sendGrid.SendEmailAsync(email);
            _sentEmails.Add(email);
        }
    }
}
