using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Mailer.Api.Auth
{
    public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly ICredentialService _credentialService;

        public BasicAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory loggerFactory, UrlEncoder encoder, ISystemClock clock,
            ICredentialService credentialService) : base(options, loggerFactory, encoder, clock)
        {
            _credentialService = credentialService;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(HeaderNames.Authorization, out var authHeader)
                  || !AuthenticationHeaderValue.TryParse(authHeader, out var authHeaderValue)
                  || authHeaderValue.Scheme.ToLowerInvariant() != "basic")
                return AuthenticateResult.NoResult();

            string login, password;
            try
            {
                var credentialBytes = Convert.FromBase64String(authHeaderValue.Parameter);
                var credential = Encoding.UTF8.GetString(credentialBytes).Split(':');
                (login, password) = (credential[0], credential[1]);
            }
            catch
            {
                return AuthenticateResult.Fail($"Invalid {HeaderNames.Authorization} header");
            }

            var user = await _credentialService.GetUserAsync(login, password);
            if (user is null)
                return AuthenticateResult.Fail("Invalid credentials");

            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name)
            };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
    }
}
