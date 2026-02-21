namespace LeadsApi.Auth.UnitTests;

internal sealed class ApiRoutesPolicy(List<ApiRoutePolicy> routes)
{
    public IReadOnlyList<ApiRoutePolicy> Routes { get; } = routes;

    public ApiRoutePolicy GetRoute(string method, string path)
    {
        return Routes.Single(route =>
            string.Equals(route.Method, method, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(route.Path, path, StringComparison.OrdinalIgnoreCase));
    }
}

internal sealed class ApiRoutePolicy
{
    public string Name { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public ApiRoutePluginsPolicy Plugins { get; set; } = new();
}

internal sealed class ApiRoutePluginsPolicy
{
    public AuthPolicy Auth { get; set; } = new();
    public OpaPolicy Opa { get; set; } = new();
}

internal sealed class AuthPolicy
{
    public bool HasPolicy { get; set; }
    public bool RequiresAuthenticationUser { get; set; }
    public List<string> RequiredScopes { get; } = [];
    public List<string> RequiredRoles { get; } = [];
}

internal sealed class OpaPolicy
{
    public List<string> RequiredStaffTypes { get; } = [];
}
