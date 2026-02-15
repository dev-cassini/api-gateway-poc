using System.Security.Claims;
using LeadsApi.Contracts;
using LeadsApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ILeadRepository, InMemoryLeadRepository>();
builder.Services.AddHttpClient<IStaffTypeClient, StaffTypeClient>((sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["StaffDirectory:BaseUrl"];

    if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
    {
        client.BaseAddress = uri;
    }

    client.Timeout = TimeSpan.FromSeconds(5);
});

var app = builder.Build();

app.UseUserClaimsFromBearerToken();

var leads = app.MapGroup("/leads");

leads.MapPost(
        "",
        (CreateLeadRequest request, HttpContext httpContext, ILeadRepository repository) =>
        {
            var errors = new Dictionary<string, string[]>();
            if (string.IsNullOrWhiteSpace(request.ContactName))
            {
                errors[nameof(request.ContactName)] = ["ContactName is required."];
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                errors[nameof(request.Email)] = ["Email is required."];
            }

            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var createdBy = httpContext.User.FindFirstValue(ClaimTypes.Email)
                            ?? httpContext.User.FindFirstValue("email");
            var lead = repository.Create(request, createdBy);
            return Results.Created($"/leads/{lead.Id}", lead);
        })
    .AllowAnonymous();

leads.MapGet(
        "/{leadId:guid}",
        (Guid leadId, ILeadRepository repository) =>
        {
            var lead = repository.GetById(leadId);
            return lead is null ? Results.NotFound() : Results.Ok(lead);
        })
    .AllowAnonymous();

leads.MapPost(
        "/{leadId:guid}/assign",
        async Task<IResult> (Guid leadId, AssignLeadRequest request, HttpContext httpContext, IStaffTypeClient staffTypeClient, ILeadRepository repository) =>
        {
            var errors = new Dictionary<string, string[]>();
            if (string.IsNullOrWhiteSpace(request.AdviserId))
            {
                errors[nameof(request.AdviserId)] = ["AdviserId is required."];
            }

            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? httpContext.User.FindFirstValue("sub");
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            var staffType = await staffTypeClient.GetStaffTypeAsync(userId, httpContext.RequestAborted);
            if (!string.Equals(staffType, "manager", StringComparison.OrdinalIgnoreCase))
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            var updatedLead = repository.Assign(leadId, request.AdviserId);
            return updatedLead is null ? Results.NotFound() : Results.Ok(updatedLead);
        })
    .AllowAnonymous();

app.Run();

public partial class Program
{
}

static partial class ProgramClaimsPrincipalExtensions
{
    public static void UseUserClaimsFromBearerToken(this WebApplication app)
    {
        app.Use((context, next) =>
        {
            if (TryCreatePrincipalFromBearerToken(context.Request.Headers.Authorization, out var principal))
            {
                context.User = principal;
            }

            return next();
        });
    }

    private static bool TryCreatePrincipalFromBearerToken(string? authorizationHeader, out ClaimsPrincipal principal)
    {
        principal = new ClaimsPrincipal(new ClaimsIdentity());

        if (string.IsNullOrWhiteSpace(authorizationHeader) ||
            !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var token = authorizationHeader["Bearer ".Length..].Trim();
        var tokenParts = token.Split('|', 4, StringSplitOptions.TrimEntries);
        if (tokenParts.Length < 2 || string.IsNullOrWhiteSpace(tokenParts[0]))
        {
            return false;
        }

        var userId = tokenParts[0];
        var roles = tokenParts[1]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (roles.Length == 0)
        {
            return false;
        }

        var scopes = tokenParts.Length >= 3
            ? tokenParts[2]
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : [];

        var email = tokenParts.Length == 4 && !string.IsNullOrWhiteSpace(tokenParts[3])
            ? tokenParts[3].Trim()
            : null;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userId),
            new("sub", userId)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(scopes.Select(scope => new Claim("scope", scope)));
        claims.AddRange(scopes.Select(scope => new Claim("scp", scope)));

        if (email is not null)
        {
            claims.Add(new Claim(ClaimTypes.Email, email));
            claims.Add(new Claim("email", email));
        }

        var identity = new ClaimsIdentity(claims, "KongForwardedBearer");
        principal = new ClaimsPrincipal(identity);
        return true;
    }
}
