using Microsoft.AspNetCore.Authorization;

namespace LeadsApi.Auth;

public sealed class MustBeManagerRequirement : IAuthorizationRequirement
{
}
