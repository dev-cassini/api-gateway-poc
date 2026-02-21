using System.Security.Claims;
using LeadsApi.Contracts;
using LeadsApi.Services;

namespace LeadsApi.Endpoints;

public static class AssignLeadEndpoint
{
    public static void MapAssignLeadEndpoint(this RouteGroupBuilder leads)
    {
        leads.MapPost("/{leadId:guid}/assign", Handle);
    }

    public static async Task<IResult> Handle(
        Guid leadId,
        AssignLeadRequest request,
        HttpContext httpContext,
        IStaffTypeClient staffTypeClient,
        ILeadRepository repository)
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
    }
}
