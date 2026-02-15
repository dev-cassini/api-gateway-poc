using System.Net.Http.Json;

namespace LeadsApi.Services;

public sealed class StaffTypeClient(HttpClient httpClient, ILogger<StaffTypeClient> logger) : IStaffTypeClient
{
    public async Task<string?> GetStaffTypeAsync(string userId, CancellationToken cancellationToken)
    {
        if (httpClient.BaseAddress is null)
        {
            logger.LogWarning("Staff directory BaseUrl is not configured.");
            return null;
        }

        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync($"staff/{Uri.EscapeDataString(userId)}/type", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed calling staff directory API for user {UserId}.", userId);
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogInformation(
                "Staff directory API returned {StatusCode} for user {UserId}.",
                (int)response.StatusCode,
                userId);
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<StaffTypeResponse>(cancellationToken: cancellationToken);
        return payload?.StaffType;
    }

    private sealed class StaffTypeResponse
    {
        public string? StaffType { get; init; }
    }
}
