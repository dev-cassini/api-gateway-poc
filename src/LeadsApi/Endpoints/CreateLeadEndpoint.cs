using System.Security.Claims;
using LeadsApi.Contracts;
using LeadsApi.Services;

namespace LeadsApi.Endpoints;

public static class CreateLeadEndpoint
{
    public static void MapCreateLeadEndpoint(this RouteGroupBuilder leads)
    {
        leads.MapPost("", Handle)
            .AllowAnonymous();
    }

    public static IResult Handle(CreateLeadRequest request, HttpContext httpContext, ILeadRepository repository)
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
    }
}
