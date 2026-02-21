namespace LeadsApi.Auth.IntegrationTests;

internal sealed class RemoteIntegrationSettings
{
    public required Uri BaseUrl { get; init; }
    public required string EnvironmentName { get; init; }
    public required Uri TokenEndpoint { get; init; }
    public string? ImpersonationEndpoint { get; init; }
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public string? Audience { get; init; }
    public string MachineClientScope { get; init; } = "openid profile";
    public string LeadImportScope { get; init; } = "leads:import";
    public string ReadScope { get; init; } = "profile:read";
    public string ImpersonationMode { get; init; } = "token_exchange";
    public string ImpersonationGrantType { get; init; } = "urn:ietf:params:oauth:grant-type:token-exchange";
    public string RequestedSubjectField { get; init; } = "requested_subject";
    public required string AdviserUser { get; init; }
    public required string CustomerUser { get; init; }
    public required string ManagerUser { get; init; }
    public required string NonPrivilegedUser { get; init; }

    public static bool TryLoadFromEnvironment(out RemoteIntegrationSettings? settings, out string? error)
    {
        settings = null;
        error = null;

        var baseUrlRaw = Environment.GetEnvironmentVariable("AUTH_TEST_BASE_URL");
        var environmentName = Environment.GetEnvironmentVariable("AUTH_TEST_ENV");
        var tokenEndpointRaw = Environment.GetEnvironmentVariable("AUTH_TEST_IDP_TOKEN_ENDPOINT");
        var clientId = Environment.GetEnvironmentVariable("AUTH_TEST_CLIENT_ID");
        var clientSecret = Environment.GetEnvironmentVariable("AUTH_TEST_CLIENT_SECRET");
        var adviserUser = Environment.GetEnvironmentVariable("AUTH_TEST_USER_ADVISER");
        var customerUser = Environment.GetEnvironmentVariable("AUTH_TEST_USER_CUSTOMER");
        var managerUser = Environment.GetEnvironmentVariable("AUTH_TEST_USER_MANAGER");
        var nonPrivilegedUser = Environment.GetEnvironmentVariable("AUTH_TEST_USER_NON_PRIVILEGED");

        if (!Uri.TryCreate(baseUrlRaw, UriKind.Absolute, out var baseUrl))
        {
            error = "AUTH_TEST_BASE_URL is missing or invalid.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(environmentName))
        {
            error = "AUTH_TEST_ENV is required.";
            return false;
        }

        if (!Uri.TryCreate(tokenEndpointRaw, UriKind.Absolute, out var tokenEndpoint))
        {
            error = "AUTH_TEST_IDP_TOKEN_ENDPOINT is missing or invalid.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            error = "AUTH_TEST_CLIENT_ID and AUTH_TEST_CLIENT_SECRET are required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(adviserUser) ||
            string.IsNullOrWhiteSpace(customerUser) ||
            string.IsNullOrWhiteSpace(managerUser) ||
            string.IsNullOrWhiteSpace(nonPrivilegedUser))
        {
            error = "AUTH_TEST_USER_ADVISER, AUTH_TEST_USER_CUSTOMER, AUTH_TEST_USER_MANAGER, and AUTH_TEST_USER_NON_PRIVILEGED are required.";
            return false;
        }

        settings = new RemoteIntegrationSettings
        {
            BaseUrl = baseUrl,
            EnvironmentName = environmentName,
            TokenEndpoint = tokenEndpoint,
            ImpersonationEndpoint = Environment.GetEnvironmentVariable("AUTH_TEST_IDP_IMPERSONATION_ENDPOINT"),
            ClientId = clientId,
            ClientSecret = clientSecret,
            Audience = Environment.GetEnvironmentVariable("AUTH_TEST_AUDIENCE"),
            MachineClientScope = Environment.GetEnvironmentVariable("AUTH_TEST_MACHINE_SCOPE") ?? "openid profile",
            LeadImportScope = Environment.GetEnvironmentVariable("AUTH_TEST_SCOPE_LEADS_IMPORT") ?? "leads:import",
            ReadScope = Environment.GetEnvironmentVariable("AUTH_TEST_SCOPE_READ") ?? "profile:read",
            ImpersonationMode = Environment.GetEnvironmentVariable("AUTH_TEST_IMPERSONATION_MODE") ?? "token_exchange",
            ImpersonationGrantType = Environment.GetEnvironmentVariable("AUTH_TEST_IMPERSONATION_GRANT_TYPE")
                ?? "urn:ietf:params:oauth:grant-type:token-exchange",
            RequestedSubjectField = Environment.GetEnvironmentVariable("AUTH_TEST_REQUESTED_SUBJECT_FIELD")
                ?? "requested_subject",
            AdviserUser = adviserUser,
            CustomerUser = customerUser,
            ManagerUser = managerUser,
            NonPrivilegedUser = nonPrivilegedUser
        };

        return true;
    }
}
