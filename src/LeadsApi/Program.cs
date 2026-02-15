using System.Security.Claims;
using LeadsApi.Auth;
using LeadsApi.Contracts;
using LeadsApi.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = DemoBearerAuthenticationHandler.SchemeName;
        options.DefaultChallengeScheme = DemoBearerAuthenticationHandler.SchemeName;
    })
    .AddScheme<AuthenticationSchemeOptions, DemoBearerAuthenticationHandler>(
        DemoBearerAuthenticationHandler.SchemeName,
        _ =>
        {
        });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthPolicies.ImportLead, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "leads:import");
    });

    options.AddPolicy(AuthPolicies.ReadLead, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("adviser", "customer");
    });

    options.AddPolicy(AuthPolicies.AssignLead, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("adviser");
        policy.AddRequirements(new MustBeManagerRequirement());
    });
});

builder.Services.AddSingleton<IAuthorizationHandler, MustBeManagerHandler>();
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
app.UseAuthorization();

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
    .RequireAuthorization(AuthPolicies.ImportLead);

leads.MapGet(
        "/{leadId:guid}",
        (Guid leadId, ILeadRepository repository) =>
        {
            var lead = repository.GetById(leadId);
            return lead is null ? Results.NotFound() : Results.Ok(lead);
        })
    .RequireAuthorization(AuthPolicies.ReadLead);

leads.MapPost(
        "/{leadId:guid}/assign",
        (Guid leadId, AssignLeadRequest request, ILeadRepository repository) =>
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

            var updatedLead = repository.Assign(leadId, request.AdviserId);
            return updatedLead is null ? Results.NotFound() : Results.Ok(updatedLead);
        })
    .RequireAuthorization(AuthPolicies.AssignLead);

app.Run();

public partial class Program
{
}
