using System.Security.Claims;
using LeadsApi.Services;
using Microsoft.AspNetCore.Authorization;

namespace LeadsApi.Auth;

public sealed class MustBeManagerHandler(IStaffTypeClient staffTypeClient)
    : AuthorizationHandler<MustBeManagerRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MustBeManagerRequirement requirement)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? context.User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var cancellationToken = context.Resource is HttpContext httpContext
            ? httpContext.RequestAborted
            : CancellationToken.None;

        var staffType = await staffTypeClient.GetStaffTypeAsync(userId, cancellationToken);
        if (string.Equals(staffType, "manager", StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }
    }
}
