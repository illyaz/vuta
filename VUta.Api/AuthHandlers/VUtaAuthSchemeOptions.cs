using Microsoft.AspNetCore.Authentication;

namespace VUta.Api.AuthHandlers;

public class VUtaAuthSchemeOptions
    : AuthenticationSchemeOptions
{
    public string Secret { get; set; } = null!;
}