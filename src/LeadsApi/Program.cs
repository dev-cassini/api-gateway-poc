using System.Security.Claims;
using System.Text;
using LeadsApi.Contracts;
using LeadsApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var jwtSigningKey = builder.Configuration["Jwt:SigningKey"] ?? "local-dev-jwt-signing-key-for-poc-2026";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            NameClaimType = "sub",
            RoleClaimType = "role",
            ClockSkew = TimeSpan.Zero
        };
    });

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

app.UseAuthentication();

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
