using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace LeadsApi.Auth;

public sealed class DemoBearerAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "DemoBearer";

    public DemoBearerAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var header = authorizationHeader.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.Fail("Authorization header must use Bearer."));
        }

        var token = header["Bearer ".Length..].Trim();
        var tokenParts = token.Split('|', 3, StringSplitOptions.TrimEntries);

        if (tokenParts.Length < 2 || string.IsNullOrWhiteSpace(tokenParts[0]))
        {
            return Task.FromResult(
                AuthenticateResult.Fail("Invalid token format. Expected <userId>|<role1,role2>[|<scope1,scope2>]."));
        }

        var userId = tokenParts[0];
        var roles = tokenParts[1]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (roles.Length == 0)
        {
            return Task.FromResult(AuthenticateResult.Fail("At least one role is required in the token."));
        }

        var scopes = tokenParts.Length == 3
            ? tokenParts[2]
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : [];

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userId),
            new("sub", userId)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(scopes.Select(scope => new Claim("scope", scope)));
        claims.AddRange(scopes.Select(scope => new Claim("scp", scope)));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
