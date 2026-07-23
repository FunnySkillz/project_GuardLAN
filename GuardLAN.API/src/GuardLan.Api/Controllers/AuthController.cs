using GuardLan.Api.Auth;
using GuardLan.Api.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace GuardLan.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    LocalUserAuthenticator authenticator,
    IOptions<GuardLanAuthOptions> options) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("session")]
    public AuthSessionDto GetSession()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return AuthSessionDto.Anonymous;
        }

        var expiresUtc = User.FindFirstValue("guardlan:expires_utc");

        return new AuthSessionDto(
            true,
            User.Identity.Name,
            DateTime.TryParse(expiresUtc, out var parsedExpiresUtc) ? parsedExpiresUtc : null);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthSessionDto>> Login(
        LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!authenticator.ValidateCredentials(request.Username, request.Password))
        {
            return Unauthorized(AuthSessionDto.Anonymous);
        }

        var expiresUtc = DateTime.UtcNow.AddHours(Math.Clamp(options.Value.SessionHours, 1, 24));
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, authenticator.Username),
            new Claim("guardlan:expires_utc", expiresUtc.ToString("O"))
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties
        {
            AllowRefresh = true,
            ExpiresUtc = expiresUtc,
            IsPersistent = true
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            properties);

        cancellationToken.ThrowIfCancellationRequested();

        return new AuthSessionDto(true, authenticator.Username, expiresUtc);
    }

    [HttpPost("logout")]
    public async Task<AuthSessionDto> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return AuthSessionDto.Anonymous;
    }
}
