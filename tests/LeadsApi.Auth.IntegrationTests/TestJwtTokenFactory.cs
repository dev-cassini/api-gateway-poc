using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace LeadsApi.Auth.IntegrationTests;

internal static class TestJwtTokenFactory
{
    public const string SigningKey = "local-dev-jwt-signing-key-for-poc-2026";

    public static string CreateToken(string userId, string[] roles, string[]? scopes = null, string? email = null)
    {
        var claims = new List<Claim>
        {
            new("sub", userId)
        };

        claims.AddRange(roles.Select(role => new Claim("role", role)));

        if (scopes is { Length: > 0 })
        {
            claims.Add(new Claim("scope", string.Join(' ', scopes)));
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            claims.Add(new Claim("email", email));
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
