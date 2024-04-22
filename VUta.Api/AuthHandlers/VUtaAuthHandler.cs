using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace VUta.Api.AuthHandlers;

public class VUtaAuthHandler
    : AuthenticationHandler<VUtaAuthSchemeOptions>
{
    public VUtaAuthHandler(
        IOptionsMonitor<VUtaAuthSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey(HeaderNames.Authorization))
            return Task.FromResult(AuthenticateResult.Fail("Header not found"));

        var header = Request.Headers[HeaderNames.Authorization].ToString();
        if (header == $"Bearer {Options.Secret}")
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "vuta_secret")
                }, nameof(VUtaAuthHandler))), Scheme.Name)));

        return Task.FromResult(AuthenticateResult.Fail("Invalid secret"));
    }
}