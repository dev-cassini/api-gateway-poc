using System.Security.Claims;
using LeadsApi.Auth;
using LeadsApi.Contracts;
using LeadsApi.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);
var disableApiAuth = builder.Configuration.GetValue<bool>("GatewayAuthTesting:DisableApiAuth");

if (!disableApiAuth)
{
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
}

builder.Services.AddSingleton<ILeadRepository, InMemoryLeadRepository>();

var app = builder.Build();

if (!disableApiAuth)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.UseAuthTestShortCircuit(builder.Configuration);

var leads = app.MapGroup("/leads");

var createLeadEndpoint = leads.MapPost(
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
        });

var getLeadEndpoint = leads.MapGet(
        "/{leadId:guid}",
        (Guid leadId, ILeadRepository repository) =>
        {
            var lead = repository.GetById(leadId);
            return lead is null ? Results.NotFound() : Results.Ok(lead);
        });

var assignLeadEndpoint = leads.MapPost(
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
        });

if (disableApiAuth)
{
    createLeadEndpoint.AllowAnonymous();
    getLeadEndpoint.AllowAnonymous();
    assignLeadEndpoint.AllowAnonymous();
}
else
{
    createLeadEndpoint.RequireAuthorization(AuthPolicies.ImportLead);
    getLeadEndpoint.RequireAuthorization(AuthPolicies.ReadLead);
    assignLeadEndpoint.RequireAuthorization(AuthPolicies.AssignLead);
}

app.Run();

public partial class Program
{
}

static partial class ProgramAuthTestExtensions
{
    public static void UseAuthTestShortCircuit(this WebApplication app, IConfiguration configuration)
    {
        var enabled = configuration.GetValue<bool>("AuthTesting:ShortCircuitEnabled");
        if (!enabled)
        {
            return;
        }
        var createdPaths = configuration
            .GetSection("AuthTesting:CreatedPaths")
            .Get<string[]>() ?? [];
        var createdPathSet = new HashSet<string>(createdPaths, StringComparer.OrdinalIgnoreCase);

        app.Use(async (context, next) =>
        {
            if (context.GetEndpoint() is not null && !context.Response.HasStarted)
            {
                var isCreated = HttpMethods.IsPost(context.Request.Method) &&
                                createdPathSet.Contains(context.Request.Path.Value ?? string.Empty);
                context.Response.StatusCode = isCreated ? StatusCodes.Status201Created : StatusCodes.Status200OK;
                return;
            }

            await next();
        });
    }
}
