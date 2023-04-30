namespace VUta.Api.AuthHandlers
{
    using Microsoft.AspNetCore.Authentication;

    public class VUtaAuthSchemeOptions
        : AuthenticationSchemeOptions
    {
        public string Secret { get; set; } = null!;
    }
}
