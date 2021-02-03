using Mailer.Domain.Application;
using Mailer.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Mailer.Infrastructure.Connectors
{
    public class SendGridOptions
    {
        public string ApiKey { get; set; } = default!;
        public MailSettings? MailSettings { get; set; }
    }

    public class SendGridMailClient : IMailClient
    {
        private SendGridClient? _client;
        private readonly IPolicyProvider _policyProvider;
        private readonly ILogger<SendGridMailClient> _logger;
        private readonly SendGridOptions _options;

        public SendGridMailClient(IPolicyProvider policyProvider, ILogger<SendGridMailClient> logger, IOptions<SendGridOptions> optionsAccessor)
        {
            _policyProvider = policyProvider;
            _options = optionsAccessor.Value;
            _logger = logger;
        }

        public void Initialize()
        {
            _client = new SendGridClient(_options.ApiKey);
        }

        public async Task SendEmailAsync(Email email)
        {
            if (_client is null)
                throw new InvalidOperationException($"Client not initialized. Call {nameof(Initialize)} first.");
            var msg = new SendGridMessage();

            if (email.Sender != null) msg.SetFrom(new EmailAddress(email.Sender.EmailAddress, email.Sender.Name));
            msg.AddTo(new EmailAddress(email.Recipient.EmailAddress, email.Recipient.Name));
            msg.SetSubject(email.Subject);
            msg.AddContent(MimeType.Text, email.PlainBody);
            msg.AddContent(MimeType.Html, email.HtmlBody);
            msg.MailSettings = _options.MailSettings;
            await _policyProvider.Get<Response>("sendgrid.sendemail",
                new PolicyConfig { Wr = new PolicyConfig.WrConfig(5, TimeSpan.FromMilliseconds(300)) },
                typeof(HttpRequestException))
                .ExecuteAsync(async () =>
                {
                    var response = await _client.SendEmailAsync(msg);
                    if ((int)response.StatusCode < 200 || (int)response.StatusCode > 299)
                    {
                        var body = await response.Body.ReadAsStringAsync();
                        _logger?.LogWarning($"Invalid status code on e-mail send: {response.StatusCode}. {body}");
                        throw new HttpRequestException(body);
                    }
                    return response;
                });
        }
    }
}
