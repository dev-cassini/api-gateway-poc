using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace LeadsApi.Auth.IntegrationTests;

internal interface IImpersonationTokenProvider
{
    Task<string> GetUserTokenAsync(string userName, string scope, CancellationToken cancellationToken);
}

internal sealed class ImpersonationTokenProvider(HttpClient idpClient, RemoteIntegrationSettings settings) : IImpersonationTokenProvider
{
    private readonly SemaphoreSlim _machineTokenLock = new(1, 1);
    private string? _machineToken;
    private DateTimeOffset _machineTokenExpiresUtc = DateTimeOffset.MinValue;

    public async Task<string> GetUserTokenAsync(string userName, string scope, CancellationToken cancellationToken)
    {
        var machineToken = await GetMachineTokenAsync(cancellationToken);

        return settings.ImpersonationMode.ToLowerInvariant() switch
        {
            "token_exchange" => await ExchangeForUserTokenAsync(machineToken, userName, scope, cancellationToken),
            "endpoint" => await RequestUserTokenFromEndpointAsync(machineToken, userName, scope, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported impersonation mode '{settings.ImpersonationMode}'.")
        };
    }

    private async Task<string> GetMachineTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_machineToken) && DateTimeOffset.UtcNow < _machineTokenExpiresUtc)
        {
            return _machineToken;
        }

        await _machineTokenLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(_machineToken) && DateTimeOffset.UtcNow < _machineTokenExpiresUtc)
            {
                return _machineToken;
            }

            var response = await idpClient.PostAsync(
                settings.TokenEndpoint,
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = settings.ClientId,
                    ["client_secret"] = settings.ClientSecret,
                    ["scope"] = settings.MachineClientScope
                }.WithOptional("audience", settings.Audience)),
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken)
                          ?? throw new InvalidOperationException("IDP did not return a valid machine token response.");

            _machineToken = payload.AccessToken;
            _machineTokenExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(30, payload.ExpiresIn - 30));
            return _machineToken;
        }
        finally
        {
            _machineTokenLock.Release();
        }
    }

    private async Task<string> ExchangeForUserTokenAsync(
        string machineToken,
        string userName,
        string scope,
        CancellationToken cancellationToken)
    {
        var response = await idpClient.PostAsync(
            settings.TokenEndpoint,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = settings.ImpersonationGrantType,
                ["client_id"] = settings.ClientId,
                ["client_secret"] = settings.ClientSecret,
                ["subject_token"] = machineToken,
                ["subject_token_type"] = "urn:ietf:params:oauth:token-type:access_token",
                [settings.RequestedSubjectField] = userName,
                ["scope"] = scope
            }.WithOptional("audience", settings.Audience)),
            cancellationToken);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken)
                      ?? throw new InvalidOperationException("IDP token exchange did not return a valid user token response.");
        return payload.AccessToken;
    }

    private async Task<string> RequestUserTokenFromEndpointAsync(
        string machineToken,
        string userName,
        string scope,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.ImpersonationEndpoint))
        {
            throw new InvalidOperationException("AUTH_TEST_IDP_IMPERSONATION_ENDPOINT is required when AUTH_TEST_IMPERSONATION_MODE=endpoint.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, settings.ImpersonationEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", machineToken);
        request.Content = JsonContent.Create(new Dictionary<string, string>
        {
            ["userName"] = userName,
            ["scope"] = scope
        });

        var response = await idpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var payload = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        if (payload.RootElement.TryGetProperty("access_token", out var tokenProperty) &&
            tokenProperty.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(tokenProperty.GetString()))
        {
            return tokenProperty.GetString()!;
        }

        throw new InvalidOperationException("Impersonation endpoint did not return an access_token.");
    }

    private sealed class TokenResponse
    {
        public required string AccessToken { get; init; }
        public int ExpiresIn { get; init; } = 300;
    }
}

internal static class DictionaryExtensions
{
    public static Dictionary<string, string> WithOptional(this Dictionary<string, string> values, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            values[key] = value;
        }

        return values;
    }
}
